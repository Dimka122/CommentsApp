-- Схема БД в диалекте MySQL 8 — для MySQL Workbench:
--   File -> Import -> Reverse Engineer MySQL Create Script... (затем Model -> Create Diagram from Catalog Objects).
-- Рабочая БД проекта — PostgreSQL, эквивалентная схема: docs/db-schema.sql. Структура и индексы совпадают.
CREATE SCHEMA IF NOT EXISTS comments DEFAULT CHARACTER SET utf8mb4;
USE comments;

CREATE TABLE users (
  id          CHAR(36)      NOT NULL,
  user_name   VARCHAR(64)   NOT NULL,
  email       VARCHAR(254)  NOT NULL,
  homepage    VARCHAR(2048) NULL,
  ip_address  VARCHAR(45)   NOT NULL,
  user_agent  VARCHAR(512)  NOT NULL,
  created_at  DATETIME(6)   NOT NULL,
  PRIMARY KEY (id),
  INDEX ix_users_user_name (user_name),
  INDEX ix_users_email (email)
) ENGINE = InnoDB;

CREATE TABLE comments (
  id          CHAR(36)   NOT NULL,
  user_id     CHAR(36)   NOT NULL,
  parent_id   CHAR(36)   NULL,
  text        TEXT       NOT NULL,   -- исходный текст (до 5000 символов)
  text_html   MEDIUMTEXT NOT NULL,   -- санитизированный HTML
  created_at  DATETIME(6) NOT NULL,
  PRIMARY KEY (id),
  INDEX ix_comments_parent_created (parent_id, created_at),
  INDEX ix_comments_user_id (user_id),
  CONSTRAINT fk_comments_user   FOREIGN KEY (user_id)   REFERENCES users (id)    ON DELETE RESTRICT,
  CONSTRAINT fk_comments_parent FOREIGN KEY (parent_id) REFERENCES comments (id) ON DELETE CASCADE
) ENGINE = InnoDB;

CREATE TABLE attachments (
  id             CHAR(36)     NOT NULL,
  comment_id     CHAR(36)     NOT NULL,
  kind           SMALLINT     NOT NULL COMMENT '1 = image, 2 = text',
  storage_path   VARCHAR(512) NOT NULL,
  original_name  VARCHAR(255) NOT NULL,
  size_bytes     BIGINT       NOT NULL,
  PRIMARY KEY (id),
  INDEX ix_attachments_comment_id (comment_id),
  CONSTRAINT fk_attachments_comment FOREIGN KEY (comment_id) REFERENCES comments (id) ON DELETE CASCADE
) ENGINE = InnoDB;
