-- Tabela imutável (LGPD) — INSERT only, nunca UPDATE/DELETE
CREATE TABLE IF NOT EXISTS consentimentos (
    id         UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id   UUID         NOT NULL REFERENCES contas (id) ON DELETE CASCADE,
    tipo       VARCHAR(50)  NOT NULL,
    versao     VARCHAR(20)  NOT NULL,
    aceito     BOOLEAN      NOT NULL,
    ip_origem  INET,
    user_agent VARCHAR(500),
    criado_em  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_consentimentos_tipo CHECK (
        tipo IN ('termos_uso', 'politica_privacidade', 'consentimento_parental')
    )
);

CREATE INDEX IF NOT EXISTS ix_consentimentos_conta_id ON consentimentos (conta_id);
CREATE INDEX IF NOT EXISTS ix_consentimentos_tipo     ON consentimentos (tipo);
