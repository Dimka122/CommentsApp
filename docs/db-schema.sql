-- Схема БД (PostgreSQL). Соответствует EF Core конфигурациям.
CREATE TABLE users (
    id          uuid PRIMARY KEY,
    user_name   varchar(64)   NOT NULL,
    email       varchar(254)  NOT NULL,
    homepage    varchar(2048) NULL,
    ip_address  varchar(45)   NOT NULL,
    user_agent  varchar(512)  NOT NULL,
    created_at  timestamptz   NOT NULL
);
CREATE INDEX ix_users_user_name ON users (user_name);
CREATE INDEX ix_users_email ON users (email);

CREATE TABLE comments (
    id          uuid PRIMARY KEY,
    user_id     uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    parent_id   uuid NULL REFERENCES comments(id) ON DELETE CASCADE,
    text        varchar(5000)  NOT NULL,
    text_html   varchar(20000) NOT NULL,
    created_at  timestamptz    NOT NULL
);
CREATE INDEX ix_comments_parent_created ON comments (parent_id, created_at DESC);
CREATE INDEX ix_comments_user_id ON comments (user_id);

CREATE TABLE attachments (
    id             uuid PRIMARY KEY,
    comment_id     uuid NOT NULL REFERENCES comments(id) ON DELETE CASCADE,
    kind           smallint NOT NULL,          -- 1 = image, 2 = text
    storage_path   varchar(512) NOT NULL,
    original_name  varchar(255) NOT NULL,
    size_bytes     bigint NOT NULL
);
CREATE INDEX ix_attachments_comment_id ON attachments (comment_id);
