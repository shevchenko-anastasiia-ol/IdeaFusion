# Image copy detection model `v1`

Finds copies of artworks already published on IdeaFusion (re-uploads, modified copies,
fragments, collages, screenshots) and tells them apart from other works by the same artist.
Output is advice for the author and the moderator; there are no automatic sanctions.

## Pipeline

1. SSCD (`sscd_disc_mixup`, Pizzi et al. 2022) 512-d descriptor, score normalised by a
   background set of 2000 works; top-5 candidates from the index.
2. For each candidate: pHash distance (with mirror), SIFT + RANSAC similarity transform
   (inliers, where they lie: a shared frame matches only on the border).
3. Fusion `gbm: sscd+phash+kp` fitted on train pairs, isotonic calibration on val:
   P(copy) for each candidate.
4. Risk zone: red if P ≥ 0.500 (≤1% of same-artist works reach it on val),
   yellow if P ≥ 0.100 (≤10%), else green. Match type from pHash and the
   transform scale: exact, modified, fragment, partial (collage/screenshot), similar composition.

## Data

`artbench_v1`: public-domain ArtBench-10 works, split by artist into train/val/test;
12 AugLy modification types (collage and screenshot only in test); hard negatives = other
works of the same artist. See ml/research/README.md.

## Test results (thresholds set on val)

| | SSCD+norm alone | deployed model |
|---|---|---|
| muAP | 0.965 | 0.986 |
| recall at ≤1% false accusations (val threshold) | 0.872 | 0.969 |
| actual same-artist false-match rate there | 0.035 | 0.031 |
| recall at ≤1% false accusations (oracle) | 0.812 | 0.964 |
| AUC copy vs same-artist work | 0.980 | 0.998 |

Recall by modification type (val threshold):

| type | recall |
|---|---|
| collage | 0.813 |
| color | 1.000 |
| crop | 1.000 |
| filter | 1.000 |
| fragment | 0.983 |
| hflip | 1.000 |
| jpeg | 1.000 |
| resize | 1.000 |
| rotate | 1.000 |
| screenshot | 0.827 |
| text_overlay | 1.000 |
| watermark | 1.000 |

## Limits

* Evaluated on 256 px public-domain paintings and prints; modern digital illustration,
  photography and 3D renders were not in the data.
* Thresholds were set on val; the false-match rate on new data can drift (it did between
  val and test). Re-check the zones on platform data before relying on them.
* "Similar composition" is informational: a redrawn work is not a copy, a person decides.
* CPU latency: ~0.1 s for the descriptor plus ~0.1 s per keypoint-verified candidate.
