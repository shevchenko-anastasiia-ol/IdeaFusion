"""Stage 3, strategy A: a projection head over frozen SSCD vectors (chapter 1, section 1.2.5).

The head is residual, y = normalize(x + MLP(x)), with the last layer initialised to zero, so
at step 0 it is exactly the SSCD baseline and training can only move away from it as far as
the loss asks. This limits "forgetting" of SSCD's robustness to modifications.

Training data: for each image, V precomputed views (clean + modified). A batch takes B images
and two random views of each; the two views of an image are positives, everything else in the
batch is a negative. The artist-aware sampler fills the batch with several works per artist,
so the negatives include exactly the hard cases the platform cares about (same hand, same
series, same frame). The random sampler is the ablation without hard negatives.
"""

from __future__ import annotations

import copy
import math
from dataclasses import dataclass
from typing import Callable

import numpy as np
import torch
import torch.nn.functional as F
from torch import nn


class ResidualHead(nn.Module):
    def __init__(self, dim: int = 512, hidden: int = 1024, dropout: float = 0.1):
        super().__init__()
        self.mlp = nn.Sequential(
            nn.Linear(dim, hidden),
            nn.GELU(),
            nn.Dropout(dropout),
            nn.Linear(hidden, dim),
        )
        nn.init.zeros_(self.mlp[-1].weight)
        nn.init.zeros_(self.mlp[-1].bias)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        return F.normalize(x + self.mlp(x), dim=-1)


def info_nce(z1: torch.Tensor, z2: torch.Tensor, tau: float) -> torch.Tensor:
    """SimCLR NT-Xent over 2B views: each view must pick its pair among the other 2B-1."""
    z = torch.cat([z1, z2])
    sim = z @ z.T / tau
    sim.fill_diagonal_(-math.inf)
    b = z1.shape[0]
    target = torch.cat([torch.arange(b, 2 * b), torch.arange(0, b)]).to(z.device)
    return F.cross_entropy(sim, target)


def batch_hard_triplet(z1: torch.Tensor, z2: torch.Tensor, margin: float) -> torch.Tensor:
    """For each anchor view: its other view vs the most similar view of another image."""
    sim = z1 @ z2.T
    pos = sim.diag()
    neg = sim.masked_fill(torch.eye(len(sim), dtype=torch.bool, device=sim.device), -math.inf)
    hardest = torch.maximum(neg.max(dim=1).values, neg.max(dim=0).values)
    return F.relu(hardest - pos + margin).mean()


class ArtistBatchSampler:
    """Batches of `artists_per_batch` artists x up to `per_artist` works each."""

    def __init__(self, artists: np.ndarray, artists_per_batch: int, per_artist: int, rng: np.random.Generator):
        self.rng = rng
        self.artists_per_batch = artists_per_batch
        self.per_artist = per_artist
        self.groups = [np.flatnonzero(artists == a) for a in np.unique(artists)]
        self.n = len(artists)

    def __iter__(self):
        pools = [self.rng.permutation(g).tolist() for g in self.groups]
        # A lone remaining work has no same-artist company; it waits for the next epoch.
        active = [a for a, p in enumerate(pools) if len(p) >= 2]
        while len(active) >= 2:
            chosen = self.rng.choice(active, size=min(self.artists_per_batch, len(active)), replace=False)
            batch: list[int] = []
            for a in chosen:
                batch += pools[a][: self.per_artist]
                pools[a] = pools[a][self.per_artist :]
            active = [a for a in active if len(pools[a]) >= 2]
            yield np.array(batch)


class RandomBatchSampler:
    def __init__(self, n: int, batch_size: int, rng: np.random.Generator):
        self.n, self.batch_size, self.rng = n, batch_size, rng

    def __iter__(self):
        perm = self.rng.permutation(self.n)
        for i in range(0, self.n - 1, self.batch_size):
            if len(perm[i : i + self.batch_size]) > 1:
                yield perm[i : i + self.batch_size]


@dataclass
class HeadConfig:
    loss: str = "infonce"  # infonce | triplet
    sampler: str = "artist"  # artist | random
    tau: float = 0.05
    margin: float = 0.2
    hidden: int = 1024
    dropout: float = 0.1
    lr: float = 1e-3
    weight_decay: float = 1e-4
    epochs: int = 30
    artists_per_batch: int = 32
    per_artist: int = 8
    batch_size: int = 256  # random sampler only
    patience: int = 8


def apply_head(head: nn.Module | None, x: np.ndarray, batch: int = 8192) -> np.ndarray:
    if head is None:
        return x
    head.eval()
    with torch.inference_mode():
        return np.concatenate([head(torch.from_numpy(x[i : i + batch])).numpy() for i in range(0, len(x), batch)])


def train_head(
    views: np.ndarray,
    artists: np.ndarray,
    cfg: HeadConfig,
    seed: int,
    validate: Callable[[nn.Module], float],
) -> tuple[nn.Module, list[dict]]:
    """Train on `views` (N, V, D); after every epoch `validate(head)` returns a score to
    maximise. Returns the best head (by that score) and the per-epoch history."""
    torch.manual_seed(seed)
    rng = np.random.default_rng(seed)
    head = ResidualHead(views.shape[2], cfg.hidden, cfg.dropout)
    opt = torch.optim.AdamW(head.parameters(), lr=cfg.lr, weight_decay=cfg.weight_decay)
    sampler = (
        ArtistBatchSampler(artists, cfg.artists_per_batch, cfg.per_artist, rng)
        if cfg.sampler == "artist"
        else RandomBatchSampler(len(views), cfg.batch_size, rng)
    )
    sched = torch.optim.lr_scheduler.CosineAnnealingLR(opt, T_max=cfg.epochs)
    data = torch.from_numpy(views)

    best_score, best_state, history, stale = validate(head), copy.deepcopy(head.state_dict()), [], 0
    history.append({"epoch": 0, "loss": float("nan"), "val": best_score})
    for epoch in range(1, cfg.epochs + 1):
        head.train()
        losses = []
        for idx in sampler:
            pick = torch.from_numpy(np.stack([rng.choice(views.shape[1], 2, replace=False) for _ in idx]))
            x = data[torch.from_numpy(idx)]
            z1 = head(x[torch.arange(len(idx)), pick[:, 0]])
            z2 = head(x[torch.arange(len(idx)), pick[:, 1]])
            loss = info_nce(z1, z2, cfg.tau) if cfg.loss == "infonce" else batch_hard_triplet(z1, z2, cfg.margin)
            opt.zero_grad()
            loss.backward()
            opt.step()
            losses.append(loss.item())
        sched.step()
        score = validate(head)
        history.append({"epoch": epoch, "loss": float(np.mean(losses)), "val": score})
        if score > best_score:
            best_score, best_state, stale = score, copy.deepcopy(head.state_dict()), 0
        else:
            stale += 1
            if stale >= cfg.patience:
                break
    head.load_state_dict(best_state)
    head.eval()
    return head, history
