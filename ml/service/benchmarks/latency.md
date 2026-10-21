# Latency of one copy check (CPU)

65 test queries (all modification types and hard negatives), index of 1300 works,
top-5 candidates verified with SIFT + RANSAC, torch threads = 4. Milliseconds.

| | descriptor | search | verify_and_fuse | total |
|---|---|---|---|---|
| 50% | 124 | 2 | 255 | 388 |
| 95% | 185 | 3 | 279 | 437 |
| max | 224 | 3 | 297 | 502 |

Exact search over 100 000 vectors (numpy, upper bound for pgvector HNSW): median 3.7 ms, max 4.9 ms.

Reference images are read from local disk here; from MinIO add the transfer of five images.
