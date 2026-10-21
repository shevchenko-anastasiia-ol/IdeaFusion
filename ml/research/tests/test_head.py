import numpy as np
import torch

from copydet.head import (
    ArtistBatchSampler,
    HeadConfig,
    ResidualHead,
    apply_head,
    batch_hard_triplet,
    info_nce,
    train_head,
)


def test_head_starts_as_identity():
    x = torch.nn.functional.normalize(torch.randn(5, 512), dim=-1)
    assert torch.allclose(ResidualHead()(x), x, atol=1e-6)


def test_losses_prefer_aligned_pairs():
    z = torch.nn.functional.normalize(torch.randn(8, 16), dim=-1)
    shuffled = z[torch.randperm(8)]
    assert info_nce(z, z, 0.1) < info_nce(z, shuffled, 0.1)
    assert batch_hard_triplet(z, z, 0.2) < batch_hard_triplet(z, shuffled, 0.2)


def test_artist_sampler_groups_artists_and_never_repeats():
    artists = np.array(["a"] * 5 + ["b"] * 3 + ["c"] * 1 + ["d"] * 4)
    sampler = ArtistBatchSampler(artists, artists_per_batch=2, per_artist=2, rng=np.random.default_rng(0))
    batches = list(sampler)
    seen = np.concatenate(batches)
    assert len(seen) == len(set(seen))
    assert 8 not in seen  # the only work of artist "c" has no same-artist partner
    for b in batches:
        _, counts = np.unique(artists[b], return_counts=True)
        assert counts.max() >= 1 and len(b) >= 2


def test_training_separates_hard_negatives():
    """Two 'artists' whose works share a strong common direction (a frame/series): the head
    must learn to tell works apart while keeping each work's views together."""
    rng = np.random.default_rng(0)
    n_art, per, views, dim = 6, 6, 4, 32
    style = rng.normal(size=(n_art, dim)) * 3
    work = rng.normal(size=(n_art * per, dim))
    artists = np.repeat(np.arange(n_art), per).astype(str)
    base = style[np.repeat(np.arange(n_art), per)] + work
    x = base[:, None, :] + rng.normal(size=(n_art * per, views, dim)) * 0.3
    x = (x / np.linalg.norm(x, axis=-1, keepdims=True)).astype(np.float32)

    def margin(head) -> float:
        z = apply_head(head, x.reshape(-1, dim)).reshape(x.shape)
        pos = np.einsum("nd,nd->n", z[:, 0], z[:, 1]).mean()
        sim = z[:, 0] @ z[:, 0].T
        same = (artists[:, None] == artists[None, :]) & ~np.eye(len(z), dtype=bool)
        return float(pos - sim[same].mean())

    cfg = HeadConfig(epochs=40, artists_per_batch=4, per_artist=6, hidden=64, dropout=0.0, lr=3e-3, tau=0.1, patience=100)
    head, history = train_head(x, artists, cfg, seed=0, validate=margin)
    assert history[0]["val"] < max(h["val"] for h in history)
    assert margin(head) > margin(None) + 0.05
