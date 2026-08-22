# Design — Módulo 1: Autenticação + Perfis

## Visão Geral da Arquitetura

```
Calegrafia.App (MAUI)
    ↓  HTTPS / JWT
Calegrafia.Api (ASP.NET Core)
    ↓
Calegrafia.Application (Use Cases)
    ↓
Calegrafia.Domain (Entidades)
    ↓
Calegrafia.Infrastructure (Dapper + PostgreSQL + DbUp)
```

---

## Modelo de Dados

### Tabela `contas`
```sql
CREATE TABLE contas (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email           VARCHAR(255) NOT NULL UNIQUE,
    senha_hash      VARCHAR(255),             -- NULL para social login puro
    status          VARCHAR(20) NOT NULL DEFAULT 'pendente', -- pendente | ativo | bloqueado
    mfa_ativo       BOOLEAN NOT NULL DEFAULT FALSE,
    mfa_secret      VARCHAR(100),             -- TOTP secret (criptografado em repouso)
    tentativas_login INT NOT NULL DEFAULT 0,
    bloqueado_ate   TIMESTAMPTZ,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Tabela `provedores_sociais`
```sql
CREATE TABLE provedores_sociais (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id    UUID NOT NULL REFERENCES contas(id) ON DELETE CASCADE,
    provedor    VARCHAR(20) NOT NULL,   -- google | apple
    subject_id  VARCHAR(255) NOT NULL,  -- ID do usuário no provedor
    UNIQUE (provedor, subject_id)
);
```

### Tabela `tokens_confirmacao`
```sql
CREATE TABLE tokens_confirmacao (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id    UUID NOT NULL REFERENCES contas(id) ON DELETE CASCADE,
    tipo        VARCHAR(30) NOT NULL,   -- confirmacao_email | redefinicao_senha | reset_mfa
    token       VARCHAR(255) NOT NULL UNIQUE,
    expira_em   TIMESTAMPTZ NOT NULL,   -- 10 minutos para reset_mfa e redefinicao_senha
    usado       BOOLEAN NOT NULL DEFAULT FALSE,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Tabela `refresh_tokens`
```sql
CREATE TABLE refresh_tokens (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id    UUID NOT NULL REFERENCES contas(id) ON DELETE CASCADE,
    token       VARCHAR(500) NOT NULL UNIQUE,
    expira_em   TIMESTAMPTZ NOT NULL,
    revogado    BOOLEAN NOT NULL DEFAULT FALSE,
    dispositivo VARCHAR(255),
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Tabela `perfis`
```sql
CREATE TABLE perfis (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id      UUID NOT NULL REFERENCES contas(id) ON DELETE CASCADE,
    nome          VARCHAR(100) NOT NULL,
    avatar_url    VARCHAR(500),
    is_infantil   BOOLEAN NOT NULL DEFAULT FALSE,
    usa_libras    BOOLEAN NOT NULL DEFAULT FALSE,
    criado_em     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    atualizado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Tabela `consentimentos` (LGPD — imutável)
```sql
CREATE TABLE consentimentos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id        UUID NOT NULL REFERENCES contas(id) ON DELETE CASCADE,
    tipo            VARCHAR(50) NOT NULL,  -- termos_uso | politica_privacidade | consentimento_parental
    versao          VARCHAR(20) NOT NULL,  -- ex: "1.0", "1.1"
    aceito          BOOLEAN NOT NULL,
    ip_origem       INET,
    user_agent      VARCHAR(500),
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT NOW()
    -- sem UPDATE/DELETE — registro imutável para conformidade LGPD
);
```

### Tabela `logs_autenticacao` (auditoria — anonimizável)
```sql
CREATE TABLE logs_autenticacao (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conta_id    UUID,                    -- NULL após anonimização
    email_hash  VARCHAR(64),             -- hash do email para auditoria anonimizada
    evento      VARCHAR(30) NOT NULL,    -- login_ok | login_falha | logout | refresh | bloqueio
    ip_origem   INET,
    user_agent  VARCHAR(500),
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## Endpoints da API

### Autenticação

| Método | Rota | RF | Auth |
|---|---|---|---|
| POST | `/api/auth/cadastro` | RF-01 | Pública |
| POST | `/api/auth/confirmar-email` | RF-01 | Pública |
| POST | `/api/auth/login` | RF-02 | Pública |
| POST | `/api/auth/refresh` | RF-03 | Pública |
| POST | `/api/auth/logout` | RF-06 | JWT |
| POST | `/api/auth/logout-todos` | RF-06 | JWT |
| POST | `/api/auth/recuperar-senha` | RF-10 | Pública |
| POST | `/api/auth/redefinir-senha` | RF-10 | Pública |
| POST | `/api/auth/social/{provedor}` | RF-04 | Pública |
| GET  | `/api/auth/mfa/configurar` | RF-05 | JWT |
| POST | `/api/auth/mfa/ativar` | RF-05 | JWT |
| POST | `/api/auth/mfa/desativar` | RF-05 | JWT |
| POST | `/api/auth/mfa/reset-solicitar` | RF-11 | Pública |
| POST | `/api/auth/mfa/reset-confirmar` | RF-11 | Pública |

### Perfis

| Método | Rota | RF | Auth |
|---|---|---|---|
| GET    | `/api/perfis` | RF-08 | JWT |
| POST   | `/api/perfis` | RF-07 | JWT |
| PUT    | `/api/perfis/{id}` | RF-09 | JWT |
| DELETE | `/api/perfis/{id}` | RF-07 | JWT |

### Conta (LGPD)

| Método | Rota | RF | Auth |
|---|---|---|---|
| POST   | `/api/conta/exportar-dados` | RF-13 | JWT |
| DELETE | `/api/conta` | RF-13 | JWT |

---

## Estrutura de Código

```
src/
├── Calegrafia.Domain/
│   ├── Entities/
│   │   ├── Conta.cs
│   │   └── Perfil.cs
│   ├── ValueObjects/
│   │   └── Email.cs
│   └── Interfaces/
│       ├── IContaRepository.cs
│       └── IPerfilRepository.cs
│
├── Calegrafia.Application/
│   ├── Auth/
│   │   ├── Commands/
│   │   │   ├── CadastrarContaCommand.cs
│   │   │   ├── ConfirmarEmailCommand.cs
│   │   │   ├── LoginCommand.cs
│   │   │   ├── RefreshTokenCommand.cs
│   │   │   ├── LoginSocialCommand.cs
│   │   │   ├── AtivarMfaCommand.cs
│   │   │   ├── DesativarMfaCommand.cs
│   │   │   ├── ResetMfaSolicitarCommand.cs
│   │   │   ├── ResetMfaConfirmarCommand.cs
│   │   │   ├── RecuperarSenhaCommand.cs
│   │   │   └── RedefinirSenhaCommand.cs
│   │   └── Handlers/  (handlers correspondentes)
│   ├── Perfis/
│   │   ├── Commands/
│   │   │   ├── CriarPerfilCommand.cs
│   │   │   ├── EditarPerfilCommand.cs
│   │   │   └── ExcluirPerfilCommand.cs
│   │   └── Queries/
│   │       └── ListarPerfisQuery.cs
│   └── Conta/
│       ├── Commands/
│       │   ├── ExportarDadosCommand.cs
│       │   └── ExcluirContaCommand.cs
│
├── Calegrafia.Infrastructure/
│   ├── Repositories/
│   │   ├── ContaRepository.cs
│   │   └── PerfilRepository.cs
│   ├── Services/
│   │   ├── JwtService.cs
│   │   ├── EmailService.cs
│   │   └── TotpService.cs
│   └── Migrations/
│       ├── V001__criar_tabela_contas.sql
│       ├── V002__criar_tabela_provedores_sociais.sql
│       ├── V003__criar_tabela_tokens_confirmacao.sql
│       ├── V004__criar_tabela_refresh_tokens.sql
│       ├── V005__criar_tabela_perfis.sql
│       ├── V006__criar_tabela_consentimentos.sql
│       └── V007__criar_tabela_logs_autenticacao.sql
│
├── Calegrafia.Api/
│   └── Controllers/
│       ├── AuthController.cs
│       ├── PerfisController.cs
│       └── ContaController.cs
│
└── Calegrafia.App/ (MAUI)
    ├── Views/
    │   ├── Auth/
    │   │   ├── LoginPage.xaml
    │   │   ├── CadastroPage.xaml
    │   │   ├── RecuperarSenhaPage.xaml
    │   │   └── MfaPage.xaml
    │   └── Perfis/
    │       ├── SelecionarPerfilPage.xaml
    │       ├── CriarPerfilPage.xaml
    │       └── EditarPerfilPage.xaml
    └── ViewModels/
        ├── Auth/
        │   ├── LoginViewModel.cs
        │   ├── CadastroViewModel.cs
        │   └── MfaViewModel.cs
        └── Perfis/
            ├── SelecionarPerfilViewModel.cs
            └── GerenciarPerfilViewModel.cs
```

---

## Fluxos Principais

### Cadastro → Onboarding
```
App: tela de cadastro (aceite de termos — checkbox desmarcado)
  → POST /api/auth/cadastro (com flag de aceite de termos)
  → consentimento gravado em consentimentos
  → email de confirmação enviado (token expira em 10 min)
  → usuário clica no link → POST /api/auth/confirmar-email
  → POST /api/auth/login → JWT retornado
  → App: tela de criar primeiro perfil (onboarding)
  → POST /api/perfis (se IsInfantil: confirmação parental gravada em consentimentos)
  → App: tela principal (perfil selecionado)
```

### Login Normal
```
App: tela de login
  → POST /api/auth/login
  → se MFA ativo: exibir tela de TOTP → reenviar com código
  → JWT retornado
  → App: tela de seleção de perfil
```

### Reset de TOTP (usuário perdeu o autenticador)
```
App: tela de login → link "Perdi o acesso ao autenticador"
  → POST /api/auth/mfa/reset-solicitar (informa email)
  → email enviado com link (expira em 10 min)
  → usuário clica no link → POST /api/auth/mfa/reset-confirmar
  → MFA desativado, todos os refresh tokens revogados
  → usuário faz login normalmente sem TOTP
```

### Renovação de Token (background)
```
App: detecta access token expirando (< 2 min para expirar)
  → POST /api/auth/refresh (automático, sem interação do usuário)
  → novo access token armazenado em SecureStorage
```

---

## Decisões de Design

| Decisão | Escolha | Motivo |
|---|---|---|
| Algoritmo de hash | BCrypt fator 12 | Padrão seguro; fator 12 equilibra segurança e performance |
| Assinatura JWT | RS256 (assimétrica) | Permite validar tokens sem expor a chave privada |
| Armazenamento de tokens no app | `SecureStorage` MAUI | Criptografado pelo OS (Keychain/Keystore) |
| Limite de perfis | 5 por conta | Cobre famílias típicas sem overhead de gestão |
| TOTP | RFC 6238 (30s, 6 dígitos) | Compatível com todos os autenticadores padrão |
| Bloqueio por tentativas | 5 falhas → 15 min | Proteção contra brute force sem ser punitivo demais |
| Social login e email existente | Vincular conta | Evita duplicidade de conta para o mesmo usuário |
| Documentação da API | Scalar + OpenAPI | Interface moderna substituindo Swagger UI; gerado via `Scalar.AspNetCore` |
| Tabela `consentimentos` | Imutável (INSERT only) | Conformidade LGPD — histórico de aceites não pode ser alterado |
| Logs de autenticação | Anonimizáveis (não deletáveis) | Obrigação legal de auditoria por 2 anos; dados sensíveis removidos por anonimização |
| Exportação de dados | Assíncrona por email | Evita timeout em requests síncronos; conformidade LGPD Art. 18 |
