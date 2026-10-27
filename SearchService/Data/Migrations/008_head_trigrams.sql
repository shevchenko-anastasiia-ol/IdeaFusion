-- Stage 8 (speed), measured on 50 000 documents.
--
-- The fuzzy trigram match moves from the whole text to the document's head (title, subtitle, tags with their
-- synonyms and categories): word_similarity over full bodies took seconds. Misspelled body words are still
-- found, because the vocabulary (search_terms) corrects them for full-text search.
ALTER TABLE search_documents ADD COLUMN head_norm text NOT NULL DEFAULT '';
ALTER TABLE search_documents ADD COLUMN head_translit text NOT NULL DEFAULT '';
CREATE INDEX ix_search_documents_head_trgm ON search_documents USING gin (head_norm gin_trgm_ops);
CREATE INDEX ix_search_documents_head_translit_trgm ON search_documents USING gin (head_translit gin_trgm_ops);

DROP INDEX ix_search_documents_norm_trgm;
DROP INDEX ix_search_documents_translit_trgm;
-- Nothing reads the transliterated body any more; without it rows stay small enough not to be TOASTed.
ALTER TABLE search_documents DROP COLUMN translit_text;

-- "Popular" order: (1 + reactions) halved every 14 days of age. In logarithms that is
-- ln(1 + reactions) + created_at·ln2/14 days, which does not depend on the current moment, so it is stored
-- with each document and indexed instead of being computed for every row of every listing.
ALTER TABLE search_documents ADD COLUMN popularity_key float8 NOT NULL DEFAULT 0;
CREATE INDEX ix_search_documents_popularity ON search_documents (popularity_key DESC, created_at DESC, id DESC);
