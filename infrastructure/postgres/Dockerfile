# Postgres for IdeaFusion: pgvector (image-copy search) + Ukrainian hunspell dictionary for full-text search.
# trixie matches the glibc (collation) of the existing data volume.

# The dictionary comes from Debian's hunspell-uk (brown-uk/dict_uk, UTF-8). It is installed in a
# separate stage: in the Postgres image the package's triggers rewrite tsearch_data.
FROM debian:trixie-slim AS dict
RUN apt-get update && apt-get install -y --no-install-recommends hunspell-uk

FROM pgvector/pgvector:pg17-trixie
COPY --from=dict /usr/share/hunspell/uk_UA.aff /usr/share/postgresql/17/tsearch_data/uk_ua.affix
COPY --from=dict /usr/share/hunspell/uk_UA.dic /usr/share/postgresql/17/tsearch_data/uk_ua.dict
COPY ukrainian.stop /usr/share/postgresql/17/tsearch_data/ukrainian.stop
