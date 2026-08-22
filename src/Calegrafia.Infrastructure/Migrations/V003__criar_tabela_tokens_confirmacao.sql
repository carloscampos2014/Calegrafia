CREATE TABLE IF NOT EXISTS tokens_confirmacao (
    id        UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id  UUID        NOT NULL REFERENCES contas (id) ON DELETE CASCADE,
    tipo      VARCHAR(30) NOT NULL,
    token     VARCHAR(255) NOT NULL UNIQUE,
    expira_em TIMESTAMPTZ NOT NULL,
    usado     BOOLEAN     NOT NULL DEFAULT FALSE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_tokens_confirmacao_tipo CHECK (
        tipo IN ('confirmacao_email', 'redefinicao_senha', 'reset_mfa')
    )
);

CREATE INDEX IF NOT EXISTS ix_tokens_confirmacao_token    ON tokens_confirmacao (token);
CREATE INDEX IF NOT EXISTS ix_tokens_confirmacao_conta_id ON tokens_confirmacao (conta_id);
