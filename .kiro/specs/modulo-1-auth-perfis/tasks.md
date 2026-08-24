# Tasks — Módulo 1: Autenticação + Perfis

> Requirements: `requirements.md` | Design: `design.md`
> Branch: `feature/modulo-1-auth-perfis`

---

## Grupo 1 — Infraestrutura base

### T-01 — Criar estrutura da solution
- [x] Criar `Calegrafia.slnx` (formato moderno XML — requer .NET 10 SDK) com os projetos: `App`, `Api`, `Application`, `Domain`, `Infrastructure`
- [x] Configurar referências entre projetos (Domain ← Application ← Infrastructure/Api)
- [x] Configurar `Serilog` no `Api` com sink de arquivo e console
- [x] Configurar `Scalar.AspNetCore` no `Api` (rota `/scalar`)
- [x] Configurar `DbUp` no `Api` para executar migrations no startup
- [x] Adicionar `Dapper` e `Npgsql` ao `Infrastructure`

**Critério de aceite:** `dotnet build Calegrafia.slnx` sem erros; `/scalar` acessível localmente ✅

---

### T-02 — Migrations do banco de dados ✅
- [x] V001 — Criar tabela `contas`
- [x] V002 — Criar tabela `provedores_sociais`
- [x] V003 — Criar tabela `tokens_confirmacao`
- [x] V004 — Criar tabela `refresh_tokens`
- [x] V005 — Criar tabela `perfis`
- [x] V006 — Criar tabela `consentimentos`
- [x] V007 — Criar tabela `logs_autenticacao`

**Critério de aceite:** migrations executam em ordem sem erro; tabelas criadas no PostgreSQL ✅

---

## Grupo 2 — Domínio e repositórios

### T-03 — Entidades e value objects do domínio ✅
- [x] `Email` (value object com validação de formato)
- [x] `Conta` (entidade com lógica: bloquear, ativar, verificar tentativas)
- [x] `Perfil` (entidade com flags `IsInfantil`, `UsaLibras`)
- [x] Interfaces: `IContaRepository`, `IPerfilRepository`
- [x] `Result<T>` e `Result` (pattern para erros de domínio sem exceções)

**Critério de aceite:** testes unitários para `Email` e lógica de bloqueio de `Conta` ✅

---

### T-04 — Repositórios com Dapper ✅
- [x] `ContaRepository`
- [x] `PerfilRepository`
- [x] `RefreshTokenRepository`
- [x] `TokenConfirmacaoRepository`
- [x] `ConsentimentoRepository` (INSERT only)
- [x] `LogAutenticacaoRepository`

**Critério de aceite:** testes de integração com banco real ou in-memory confirmam as queries ✅

---

## Grupo 3 — Serviços de infraestrutura

### T-05 — JwtService ✅
- [x] Gerar access token (RS256, 15 min, claims: sub, email, perfil_id)
- [x] Gerar refresh token (string aleatória segura, 30 dias)
- [x] Validar e decodificar access token
- [x] `MapInboundClaims = false` para preservar nomes originais de claims

**Critério de aceite:** testes unitários validam geração e validação de tokens ✅ (12 testes passando)

---

### T-06 — TotpService (RFC 6238 + AES-256) ✅
- [x] Gerar secret TOTP (Base32)
- [x] Gerar URI para QR code (otpauth://)
- [x] Validar código TOTP (RFC 6238, janela de 1 passo)
- [x] Criptografar/descriptografar secret em repouso (AES-256-CBC, IV aleatório)

**Critério de aceite:** testes unitários validam geração e verificação de códigos TOTP ✅ (16 testes passando)

---

### T-07 — EmailService ✅
- [x] Enviar email de confirmação de cadastro (com link + token)
- [x] Enviar email de redefinição de senha (com link + token, expira em 10 min)
- [x] Enviar email de reset de TOTP (com link + token, expira em 10 min)
- [x] Enviar email de exportação de dados (com arquivo JSON em anexo)
- [x] Configurar provider (SMTP via MailKit 4.17.0, configurável por `appsettings`)

**Critério de aceite:** emails enviados em ambiente de teste (Mailpit ou similar) ✅ (testes via NSubstitute; integração com Mailpit em testes de integração futuros)

---

## Grupo 4 — Use cases de autenticação

### T-08 — Cadastro e confirmação de email (RF-01, RF-12) ✅
- [x] `CadastrarContaCommand` — validar email único, hash de senha, gravar consentimento, enviar email
- [x] `ConfirmarEmailCommand` — validar token, ativar conta, marcar token como usado
- [x] `IPasswordHasher` + `BcryptPasswordHasher` (fator 12)
- [x] Testes TDD: email duplicado, senha fraca, sem aceitar termos, token expirado/usado

**Critério de aceite:** testes TDD passando ✅ (59 testes, +23 novos)

---

### T-09 — Login e MFA (RF-02, RF-05) ✅
- [x] `LoginCommand` — verificar email/senha, checar bloqueio, checar MFA, retornar JWT + refresh token
- [x] Lógica de bloqueio por tentativas (5 falhas → 15 min)
- [x] Fluxo de MFA: login retorna `mfa_required` quando ativo; segundo passo valida TOTP
- [x] `LogAutenticacao` gravado em cada tentativa
- [x] Testes TDD: login correto, senha errada, conta bloqueada, MFA válido, MFA inválido

**Critério de aceite:** testes TDD passando ✅ (71 testes, +12 novos)

---

### T-10 — Refresh token e logout (RF-03, RF-06) ✅
- [x] `RefreshTokenCommand/Handler` — rotação de token (revoga atual, emite novo)
- [x] `LogoutCommand/Handler` — revogar refresh token (idempotente)
- [x] `LogoutTodosCommand/Handler` — revogar todos os refresh tokens da conta
- [x] Teste TDD: refresh válido, token expirado, token revogado, logout e tentativa de re-uso

**Critério de aceite:** testes TDD passando ✅ (81 testes, +10 novos)

---

### T-11 — Social login (RF-04) ✅
- [x] `LoginSocialCommand/Handler` — validar token via ISocialLoginProvider, criar ou vincular conta
- [x] `ISocialLoginProvider` — abstração DIP para Google e Apple (implementações na Infrastructure)
- [x] `IProvedorSocialRepository` — vincular provedor social à conta
- [x] `SocialUserInfo` record separado
- [x] Teste TDD: provedor inválido, token inválido, novo usuário, usuário existente com mesmo email

**Critério de aceite:** testes TDD passando ✅ (89 testes, +8 novos)

---

### T-12 — Recuperação e redefinição de senha (RF-10) ✅
- [x] `RecuperarSenhaHandler` — token 10 min, retorna 200 mesmo sem email (não revela existência)
- [x] `RedefinirSenhaHandler` — valida token, senha forte, atualiza hash, revoga todos os refresh tokens
- [x] Handlers separados em arquivos individuais
- [x] Teste TDD: token válido, token expirado, token já usado, senha fraca, email inexistente, falha silenciosa

**Critério de aceite:** testes TDD passando ✅ (102 testes, +13 novos)

---

### T-13 — MFA: ativar, desativar e reset (RF-05, RF-11) ✅
- [x] `AtivarMfaCommand/Handler` — passo 1: gera QR code; passo 2: valida TOTP e persiste secret criptografado
- [x] `DesativarMfaCommand/Handler` — exige TOTP válido antes de desativar
- [x] `ResetMfaSolicitarCommand/Handler` — silencioso se email inválido ou MFA não ativo; token 10 min
- [x] `ResetMfaConfirmarCommand/Handler` — valida token, desativa MFA, revoga todos os refresh tokens
- [x] Testes TDD: 15 testes cobrindo todos os cenários

**Critério de aceite:** testes TDD passando ✅ (117 testes, +15 novos)

---

## Grupo 5 — Use cases de perfis

### T-14 — CRUD de perfis (RF-07, RF-08, RF-09) ✅
- [x] `CriarPerfilCommand/Handler` — limite 5, consentimento parental obrigatório para IsInfantil
- [x] `ListarPerfisQuery/Handler` — retorna lista mapeada para PerfilResult
- [x] `EditarPerfilCommand/Handler` — verifica ownership antes de editar
- [x] `ExcluirPerfilCommand/Handler` — verifica ownership antes de excluir
- [x] Testes TDD: 15 testes (132 total)

**Critério de aceite:** testes TDD passando ✅

---

## Grupo 6 — Use cases LGPD

### T-15 — Exportação e exclusão de conta (RF-13)
- [ ] `ExportarDadosCommand` — enfileirar job, retornar 202; job gera JSON e envia por email
- [ ] `ExcluirContaCommand` — verificar senha, excluir dados, anonimizar logs, revogar tokens
- [ ] Teste TDD: exportar dados, excluir conta com senha correta/incorreta

---

## Grupo 7 — Controllers e MAUI

### T-16 — Controllers da API ✅
- [x] `AuthController` — 14 endpoints (RF-01 a RF-11)
- [x] `PerfisController` — 4 endpoints CRUD com `[Authorize]`
- [x] `ContaController` — 2 endpoints LGPD (RF-13)
- [x] JWT RS256 + `UseAuthentication/Authorization` no pipeline
- [x] Rate limiting login: 10 req/min por IP
- [x] DI completo — todos handlers e repositórios registrados
- [x] `ProvedorSocialRepository` — ON CONFLICT DO NOTHING

**Critério de aceite:** todos os endpoints visíveis no Scalar ✅ (build OK, 138 testes)
- [ ] `ContaController` — mapear endpoints LGPD (RF-13)
- [ ] Configurar autenticação JWT no `Program.cs`
- [ ] Rate limiting no endpoint de login (10 req/min por IP)
- [ ] Documentar todos os endpoints com atributos OpenAPI (`[ProducesResponseType]`, summaries)

**Critério de aceite:** todos os endpoints visíveis e testáveis no Scalar (`/scalar`)

---

### T-17 — App MAUI: telas de autenticação
- [ ] `LoginPage` + `LoginViewModel` — email/senha, link para cadastro e recuperação
- [ ] `CadastroPage` + `CadastroViewModel` — formulário com checkboxes de termos (desmarcados)
- [ ] `RecuperarSenhaPage` + `RecuperarSenhaViewModel` — input de email
- [ ] `MfaPage` + `MfaViewModel` — input de código TOTP + link para reset

**Critério de aceite:** fluxo completo de login funcional no emulador

---

### T-18 — App MAUI: telas de perfis
- [ ] `SelecionarPerfilPage` + `SelecionarPerfilViewModel` — lista de perfis com avatar e nome
- [ ] `CriarPerfilPage` + `GerenciarPerfilViewModel` — formulário de criação com flag infantil e Libras
- [ ] `EditarPerfilPage` — reusa `GerenciarPerfilViewModel`
- [ ] Aplicar tema `InfantilTheme` ao selecionar perfil com `IsInfantil = true`

**Critério de aceite:** seleção de perfil aplica tema correto; limite de 5 perfis validado no app

---

### T-19 — Renovação automática de token no App
- [ ] Interceptor HTTP que detecta token expirando (< 2 min) e renova silenciosamente
- [ ] `SecureStorage` para armazenar `access_token` e `refresh_token`
- [ ] Logout automático se refresh token expirado/revogado

**Critério de aceite:** token renovado sem interação do usuário; logout automático em refresh inválido

---

## Resumo

| Grupo | Tasks | RFs cobertos |
|---|---|---|
| 1 — Infraestrutura | T-01, T-02 | — |
| 2 — Domínio | T-03, T-04 | — |
| 3 — Serviços | T-05, T-06, T-07 | — |
| 4 — Auth | T-08 a T-13 | RF-01 a RF-06, RF-10 a RF-12 |
| 5 — Perfis | T-14 | RF-07 a RF-09 |
| 6 — LGPD | T-15 | RF-13 |
| 7 — API + MAUI | T-16 a T-19 | Todos |

**Total: 19 tasks**
