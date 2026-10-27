-- Search index of IdeaFusion (posts, teams, vacancies, people).
-- The index is a copy: SearchService rebuilds it from the source databases at any time.

CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS fuzzystrmatch;

-- Text search configuration "ideafusion": Ukrainian words go through the hunspell uk_UA
-- dictionary (infrastructure/postgres/Dockerfile), unknown words are kept as they are,
-- Latin words are stemmed as English. On a server without the dictionary files the
-- configuration still works, only without Ukrainian morphology.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_ts_dict WHERE dictname = 'uk_hunspell') THEN
        BEGIN
            CREATE TEXT SEARCH DICTIONARY uk_hunspell (
                TEMPLATE = ispell, DictFile = uk_ua, AffFile = uk_ua, StopWords = ukrainian);
        EXCEPTION WHEN OTHERS THEN
            RAISE WARNING 'uk_hunspell is unavailable (%); Ukrainian words are indexed without morphology', SQLERRM;
        END;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_ts_config WHERE cfgname = 'ideafusion') THEN
        CREATE TEXT SEARCH CONFIGURATION ideafusion (COPY = simple);
        IF EXISTS (SELECT 1 FROM pg_ts_dict WHERE dictname = 'uk_hunspell') THEN
            ALTER TEXT SEARCH CONFIGURATION ideafusion
                ALTER MAPPING FOR word, hword, hword_part WITH uk_hunspell, simple;
        END IF;
        ALTER TEXT SEARCH CONFIGURATION ideafusion
            ALTER MAPPING FOR asciiword, asciihword, hword_asciipart WITH english_stem;
    END IF;
END
$$;

CREATE TABLE search_documents (
    id             text PRIMARY KEY,              -- 'post:42', 'team:<ObjectId>', 'vacancy:<ObjectId>', 'user:<Guid>'
    kind           text NOT NULL CHECK (kind IN ('post', 'team', 'vacancy', 'user')),
    source_id      text NOT NULL,                 -- id in the source service
    title          text NOT NULL,
    subtitle       text,                          -- author / team / specialization, shown under the title
    body           text,
    tags           text[] NOT NULL DEFAULT '{}',
    categories     text[] NOT NULL DEFAULT '{}',  -- derived from tags through tag_categories
    tag_text       text NOT NULL DEFAULT '',      -- tags and category names as one string for the tsvector
    image_url      text,
    owner_id       text,
    team_id        text,
    created_at     timestamptz NOT NULL,
    likes          int NOT NULL DEFAULT 0,
    saves          int NOT NULL DEFAULT 0,
    views          int NOT NULL DEFAULT 0,
    open_vacancies int NOT NULL DEFAULT 0,
    norm_text      text NOT NULL,                 -- TextNormalizer.Normalize of all searchable text
    translit_text  text NOT NULL,                 -- the same text in Latin (KMU-2010)
    indexed_at     timestamptz NOT NULL DEFAULT now(),
    fts tsvector GENERATED ALWAYS AS (
        setweight(to_tsvector('ideafusion'::regconfig, coalesce(title, '')), 'A') ||
        setweight(to_tsvector('ideafusion'::regconfig, tag_text), 'B') ||
        setweight(to_tsvector('ideafusion'::regconfig, coalesce(body, '')), 'C') ||
        setweight(to_tsvector('ideafusion'::regconfig, coalesce(subtitle, '')), 'D')
    ) STORED
);

CREATE INDEX ix_search_documents_fts ON search_documents USING gin (fts);
CREATE INDEX ix_search_documents_norm_trgm ON search_documents USING gin (norm_text gin_trgm_ops);
CREATE INDEX ix_search_documents_translit_trgm ON search_documents USING gin (translit_text gin_trgm_ops);
CREATE INDEX ix_search_documents_tags ON search_documents USING gin (tags);
CREATE INDEX ix_search_documents_categories ON search_documents USING gin (categories);
CREATE INDEX ix_search_documents_kind_created ON search_documents (kind, created_at DESC);

-- Vocabulary of the index for spelling correction ("did you mean").
CREATE TABLE search_terms (
    term     text PRIMARY KEY,
    translit text NOT NULL,
    doc_freq int  NOT NULL
);

CREATE INDEX ix_search_terms_term_trgm ON search_terms USING gin (term gin_trgm_ops);
CREATE INDEX ix_search_terms_translit_trgm ON search_terms USING gin (translit gin_trgm_ops);
CREATE INDEX ix_search_terms_term_prefix ON search_terms (term text_pattern_ops);
CREATE INDEX ix_search_terms_translit_prefix ON search_terms (translit text_pattern_ops);

-- Categories are not stored by the source services: a post or team belongs to every category of its tags.
CREATE TABLE tag_categories (
    tag      text NOT NULL,
    category text NOT NULL,
    PRIMARY KEY (tag, category)
);

INSERT INTO tag_categories (tag, category) VALUES
    ('design', 'Дизайн'), ('branding', 'Дизайн'),
    ('music', 'Музика'),
    ('photography', 'Фото'),
    ('animation', 'Анімація'),
    ('art', 'Арт'), ('illustration', 'Арт'), ('street-art', 'Арт'),
    ('digital', 'Цифровий'),
    ('gamedev', 'Gamedev'),
    ('3d', '3D'),
    ('ui-ux', 'UI/UX'),
    ('architecture', 'Архітектура'),
    ('collab', 'Колаб'),
    ('fashion', 'Мода'),
    ('film', 'Фільм'),
    ('poetry', 'Поезія'),
    ('traditional', 'Традиційне'),
    ('wip', 'В процесі'),
    ('writing', 'Письменство');

-- Result of the last synchronisation of every source, for /api/search/status.
CREATE TABLE index_sources (
    kind       text PRIMARY KEY,
    synced_at  timestamptz,
    documents  int NOT NULL DEFAULT 0,
    last_error text,
    failed_at  timestamptz
);
