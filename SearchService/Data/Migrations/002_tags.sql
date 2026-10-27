-- Stage 2: tags as one vocabulary. Written forms of a tag ("#3D", "3d", "3-д") share a key
-- (TextNormalizer.TagKey), and synonyms ("ілюстрація", "illustration") share a canonical tag.

-- Synonym groups: every term of a group points to its canonical term.
-- Keys are computed in the service, so the table holds the terms as people write them.
CREATE TABLE tag_synonyms (
    term      text PRIMARY KEY,
    canonical text NOT NULL
);

INSERT INTO tag_synonyms (term, canonical) VALUES
    ('illustration', 'illustration'), ('illustrations', 'illustration'), ('ілюстрація', 'illustration'),
    ('ілюстрації', 'illustration'),
    ('art', 'art'), ('арт', 'art'), ('мистецтво', 'art'),
    ('street-art', 'street-art'), ('стріт-арт', 'street-art'), ('graffiti', 'street-art'), ('графіті', 'street-art'),
    ('design', 'design'), ('дизайн', 'design'),
    ('branding', 'branding'), ('брендинг', 'branding'),
    ('music', 'music'), ('музика', 'music'),
    ('photography', 'photography'), ('photo', 'photography'), ('фото', 'photography'), ('фотографія', 'photography'),
    ('animation', 'animation'), ('анімація', 'animation'),
    ('3d', '3d'), ('3d-modeling', '3d'), ('3д-моделювання', '3d'),
    ('gamedev', 'gamedev'), ('геймдев', 'gamedev'), ('game-development', 'gamedev'),
    ('ui-ux', 'ui-ux'), ('ux-ui', 'ui-ux'), ('ux', 'ui-ux'),
    ('architecture', 'architecture'), ('архітектура', 'architecture'),
    ('digital', 'digital'), ('цифровий', 'digital'), ('диджитал', 'digital'),
    ('fashion', 'fashion'), ('мода', 'fashion'),
    ('film', 'film'), ('фільм', 'film'), ('кіно', 'film'),
    ('poetry', 'poetry'), ('поезія', 'poetry'), ('вірші', 'poetry'),
    ('writing', 'writing'), ('письменство', 'writing'),
    ('traditional', 'traditional'), ('традиційне', 'traditional'),
    ('wip', 'wip'), ('в процесі', 'wip'),
    ('collab', 'collab'), ('колаб', 'collab'), ('колаборація', 'collab'), ('collaboration', 'collab');

-- Canonical tag keys of a document, for tag filters (tags @> / tags &&) independent of spelling.
ALTER TABLE search_documents ADD COLUMN tag_keys text[] NOT NULL DEFAULT '{}';
CREATE INDEX ix_search_documents_tag_keys ON search_documents USING gin (tag_keys);
DROP INDEX ix_search_documents_tags;

-- Tags in use, for autocomplete: one row per canonical key with its display name and popularity.
CREATE TABLE search_tags (
    key       text PRIMARY KEY,
    label     text NOT NULL,
    doc_count int  NOT NULL
);

-- Every key that leads to a tag in use: its own key and the keys of its synonyms.
CREATE TABLE search_tag_names (
    name_key  text NOT NULL,
    tag_key   text NOT NULL REFERENCES search_tags (key) ON DELETE CASCADE,
    PRIMARY KEY (name_key, tag_key)
);

CREATE INDEX ix_search_tag_names_trgm ON search_tag_names USING gin (name_key gin_trgm_ops);
CREATE INDEX ix_search_tag_names_prefix ON search_tag_names (name_key text_pattern_ops);
