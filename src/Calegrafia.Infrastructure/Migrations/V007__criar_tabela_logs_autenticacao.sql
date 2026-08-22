-- conta_id pode ser NULL após anonimização (obrigação LGPD — retenção 2 anos)
CREATE TABLE IF NOT EXISTS logs_autenticacao (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id    UUID,
    email_hash  VARCHAR(64),
    evento      VARCHAR(30)  NOT NULL,
    ip_origem   INET,
    user_agent  VARCHAR(500),
    criado_em   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_logs_autenticacao_evento CHECK (
        evento IN ('login_ok', 'login_falha', 'logout', 'refresh', 'bloqueio', 'mfa_ok', 'mfa_falha')
    )
);

CREATE INDEX IF NOT EXISTS ix_logs_autenticacao_conta_id ON logs_autenticacao (conta_id);
CREATE INDEX IF NOT EXISTS ix_logs_autenticacao_criado_em ON logs_autenticacao (criado_em);
