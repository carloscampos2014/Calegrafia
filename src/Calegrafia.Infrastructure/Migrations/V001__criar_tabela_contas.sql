CREATE TABLE IF NOT EXISTS contas (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    email            VARCHAR(255) NOT NULL UNIQUE,
    senha_hash       VARCHAR(255),
    status           VARCHAR(20)  NOT NULL DEFAULT 'pendente',
    mfa_ativo        BOOLEAN      NOT NULL DEFAULT FALSE,
    mfa_secret       VARCHAR(100),
    tentativas_login INT          NOT NULL DEFAULT 0,
    bloqueado_ate    TIMESTAMPTZ,
    criado_em        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    atualizado_em    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_contas_status CHECK (status IN ('pendente', 'ativo', 'bloqueado'))
);

CREATE INDEX IF NOT EXISTS ix_contas_email ON contas (email);
