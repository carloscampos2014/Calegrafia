CREATE TABLE IF NOT EXISTS refresh_tokens (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id    UUID         NOT NULL REFERENCES contas (id) ON DELETE CASCADE,
    token       VARCHAR(500) NOT NULL UNIQUE,
    expira_em   TIMESTAMPTZ  NOT NULL,
    revogado    BOOLEAN      NOT NULL DEFAULT FALSE,
    dispositivo VARCHAR(255),
    criado_em   TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_token    ON refresh_tokens (token);
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_conta_id ON refresh_tokens (conta_id);
