CREATE TABLE IF NOT EXISTS perfis (
    id            UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id      UUID         NOT NULL REFERENCES contas (id) ON DELETE CASCADE,
    nome          VARCHAR(100) NOT NULL,
    avatar_url    VARCHAR(500),
    is_infantil   BOOLEAN      NOT NULL DEFAULT FALSE,
    usa_libras    BOOLEAN      NOT NULL DEFAULT FALSE,
    criado_em     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    atualizado_em TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_perfis_conta_id ON perfis (conta_id);
