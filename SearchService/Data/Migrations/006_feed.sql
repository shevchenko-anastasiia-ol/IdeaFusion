-- Stage 6: the "For you" feed without ML. Interactions are copied from contentdb every cycle, and an
-- interest profile per user (weights of tags, categories and authors) is rebuilt from them.

-- One row per (user, post, kind) with the latest moment: repeated views count once.
CREATE TABLE user_interactions (
    user_id     text        NOT NULL,   -- Keycloak sub
    document_id text        NOT NULL,   -- search_documents.id, e.g. post:42
    kind        text        NOT NULL CHECK (kind IN ('view', 'like', 'save')),
    at          timestamptz NOT NULL,
    PRIMARY KEY (user_id, document_id, kind)
);

-- feature: tag:<canonical key>, category:<name> or author:<owner id>.
CREATE TABLE user_interest (
    user_id text   NOT NULL,
    feature text   NOT NULL,
    weight  float8 NOT NULL,
    PRIMARY KEY (user_id, feature)
);

-- What the feed showed and what was opened, to compare variants (content-based, cold start, later ML).
CREATE TABLE feed_events (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id     text        NOT NULL,
    document_id text        NOT NULL,
    event       text        NOT NULL CHECK (event IN ('impression', 'click')),
    position    int         NOT NULL,
    variant     text        NOT NULL,
    at          timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_feed_events_at ON feed_events (at);
CREATE INDEX ix_feed_events_user ON feed_events (user_id, document_id);
