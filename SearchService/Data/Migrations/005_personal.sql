-- Stage 5: search history, saved searches and their notifications. Everything here belongs to one user
-- (Keycloak sub) and is only ever read back by that user.

-- When a document first entered the index; saved searches with notify are matched against newer ones.
ALTER TABLE search_documents ADD COLUMN first_indexed_at timestamptz NOT NULL DEFAULT now();
CREATE INDEX ix_search_documents_first_indexed ON search_documents (first_indexed_at);

CREATE TABLE search_history (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id       text        NOT NULL,
    query         text        NOT NULL,
    filters       jsonb       NOT NULL,
    fingerprint   text        NOT NULL,   -- normalized query + filters: a repeated search updates its row
    results_count int         NOT NULL,
    searched_at   timestamptz NOT NULL DEFAULT now(),
    UNIQUE (user_id, fingerprint)
);
CREATE INDEX ix_search_history_user ON search_history (user_id, searched_at DESC);

CREATE TABLE saved_searches (
    id         uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    text        NOT NULL,
    name       text        NOT NULL,
    query      text        NOT NULL,
    filters    jsonb       NOT NULL,
    notify     boolean     NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    checked_at timestamptz NOT NULL DEFAULT now(),   -- documents indexed after this are new for the subscription
    UNIQUE (user_id, name)
);
CREATE INDEX ix_saved_searches_notify ON saved_searches (checked_at) WHERE notify;

CREATE TABLE search_notifications (
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id         text        NOT NULL,
    saved_search_id uuid        NOT NULL REFERENCES saved_searches (id) ON DELETE CASCADE,
    document_id     text        NOT NULL,
    kind            text        NOT NULL,
    title           text        NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT now(),
    read_at         timestamptz,
    UNIQUE (saved_search_id, document_id)
);
CREATE INDEX ix_search_notifications_user ON search_notifications (user_id, created_at DESC);
