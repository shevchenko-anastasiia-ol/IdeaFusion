-- Stage 4: role directory. Roles are compared word by word, and every word of a role also brings its
-- synonyms into role_text, so "дизайнер" finds "Motion Designer" and "illustrator" finds "Ілюстраторка".
CREATE TABLE role_synonyms (
    term      text PRIMARY KEY,   -- one word, lower case
    canonical text NOT NULL
);

INSERT INTO role_synonyms (term, canonical) VALUES
    ('designer', 'designer'), ('дизайнер', 'designer'), ('дизайнерка', 'designer'),
    ('illustrator', 'illustrator'), ('ілюстратор', 'illustrator'), ('ілюстраторка', 'illustrator'),
    ('artist', 'artist'), ('художник', 'artist'), ('художниця', 'artist'), ('артист', 'artist'), ('митець', 'artist'),
    ('animator', 'animator'), ('аніматор', 'animator'), ('аніматорка', 'animator'), ('мультиплікатор', 'animator'),
    ('developer', 'developer'), ('розробник', 'developer'), ('розробниця', 'developer'), ('програміст', 'developer'),
    ('programmer', 'developer'), ('dev', 'developer'),
    ('musician', 'musician'), ('музикант', 'musician'), ('музикантка', 'musician'),
    ('composer', 'composer'), ('композитор', 'composer'), ('композиторка', 'composer'),
    ('photographer', 'photographer'), ('фотограф', 'photographer'), ('фотографиня', 'photographer'),
    ('videographer', 'videographer'), ('відеограф', 'videographer'), ('оператор', 'videographer'),
    ('editor', 'editor'), ('редактор', 'editor'), ('редакторка', 'editor'), ('монтажер', 'editor'), ('монтажерка', 'editor'),
    ('writer', 'writer'), ('письменник', 'writer'), ('письменниця', 'writer'), ('копірайтер', 'writer'), ('copywriter', 'writer'),
    ('screenwriter', 'screenwriter'), ('сценарист', 'screenwriter'), ('сценаристка', 'screenwriter'),
    ('modeler', 'modeler'), ('modeller', 'modeler'), ('моделер', 'modeler'), ('моделерка', 'modeler'),
    ('drummer', 'drummer'), ('барабанщик', 'drummer'), ('ударник', 'drummer'),
    ('vocalist', 'vocalist'), ('singer', 'vocalist'), ('вокаліст', 'vocalist'), ('вокалістка', 'vocalist'),
    ('співак', 'vocalist'), ('співачка', 'vocalist'),
    ('guitarist', 'guitarist'), ('гітарист', 'guitarist'), ('гітаристка', 'guitarist'),
    ('keyboardist', 'keyboardist'), ('клавішник', 'keyboardist'), ('клавішниця', 'keyboardist'),
    ('engineer', 'engineer'), ('інженер', 'engineer'), ('інженерка', 'engineer'),
    ('producer', 'producer'), ('продюсер', 'producer'), ('продюсерка', 'producer'),
    ('manager', 'manager'), ('менеджер', 'manager'), ('менеджерка', 'manager'),
    ('architect', 'architect'), ('архітектор', 'architect'), ('архітекторка', 'architect'),
    ('rigger', 'rigger'), ('рігер', 'rigger'),
    ('motion', 'motion'), ('моушн', 'motion'),
    ('concept', 'concept'), ('концепт', 'concept'),
    ('graphic', 'graphic'), ('графічний', 'graphic'),
    ('sound', 'sound'), ('звуковий', 'sound'), ('звукорежисер', 'sound'),
    ('web', 'web'), ('веб', 'web'),
    ('mobile', 'mobile'), ('мобільний', 'mobile');

-- Roles in use, for the role filter's autocomplete: one row per role as people write it (normalized),
-- with role_text widened by the directory like the documents' one.
CREATE TABLE search_roles (
    key       text PRIMARY KEY,   -- normalized words of the role
    label     text NOT NULL,      -- most common spelling
    role_text text NOT NULL,
    vacancies int  NOT NULL,      -- open vacancies with this role
    teams     int  NOT NULL,      -- teams that need it
    people    int  NOT NULL       -- people with this specialization
);
CREATE INDEX ix_search_roles_trgm ON search_roles USING gin (role_text gin_trgm_ops);
