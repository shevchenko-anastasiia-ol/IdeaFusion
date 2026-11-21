-- Stage 3: combined filters, sorting and facets.

-- Roles a document is about: a vacancy's role, a person's specialization, a team's required roles.
-- role_text is their normalized and transliterated form for the fuzzy role filter
-- ("аніматор" finds "Аніматор 2D", "khudozhnyk" finds "3D художник").
ALTER TABLE search_documents ADD COLUMN roles text[] NOT NULL DEFAULT '{}';
ALTER TABLE search_documents ADD COLUMN role_text text NOT NULL DEFAULT '';
CREATE INDEX ix_search_documents_role_trgm ON search_documents USING gin (role_text gin_trgm_ops);

CREATE INDEX ix_search_documents_owner ON search_documents (owner_id);
CREATE INDEX ix_search_documents_team ON search_documents (team_id);
CREATE INDEX ix_search_documents_created ON search_documents (created_at DESC, id DESC);
