-- Stage 8: quality metrics. Nothing here identifies a user or keeps the query text:
-- only how a search went (results, latency, whether it was corrected) and where results were clicked.
CREATE TABLE search_queries (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    at          timestamptz NOT NULL DEFAULT now(),
    kind        text,
    has_query   boolean     NOT NULL,
    corrected   boolean     NOT NULL,
    results     int         NOT NULL,
    duration_ms int         NOT NULL
);
CREATE INDEX ix_search_queries_at ON search_queries (at);

CREATE TABLE search_clicks (
    id       bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    at       timestamptz NOT NULL DEFAULT now(),
    kind     text        NOT NULL,
    position int         NOT NULL
);
CREATE INDEX ix_search_clicks_at ON search_clicks (at);
