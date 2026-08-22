CREATE TABLE IF NOT EXISTS provedores_sociais (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id    UUID        NOT NULL REFERENCES contas (id) ON DELETE CASCADE,
    provedor    VARCHAR(20) NOT NULL,
    subject_id  VARCHAR(255) NOT NULL,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_provedores_sociais_provedor CHECK (provedor IN ('google', 'apple')),
    CONSTRAINT uq_provedores_sociais          UNIQUE (provedor, subject_id)
);

CREATE INDEX IF NOT EXISTS ix_provedores_sociais_conta_id ON provedores_sociais (conta_id);
