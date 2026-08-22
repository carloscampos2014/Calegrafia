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

### T-07 — EmailService
- [ ] Enviar email de confirmação de cadastro (com link + token)
- [ ] Enviar email de redefinição de senha (com link + token, expira em 10 min)
- [ ] Enviar email de reset de TOTP (com link + token, expira em 10 min)
- [ ] Enviar email de exportação de dados (com arquivo JSON em anexo)
- [ ] Configurar provider (SMTP ou serviço de email — configurável por `appsettings`)

**Critério de aceite:** emails enviados em ambiente de teste (Mailpit ou similar)

---

## Grupo 4 — Use cases de autenticação

### T-08 — Cadastro e confirmação de email (RF-01, RF-12)
- [ ] `CadastrarContaCommand` — validar email único, hash de senha, gravar consentimento, enviar email
- [ ] `ConfirmarEmailCommand` — validar token, ativar conta, marcar token como usado
- [ ] Teste TDD: cadastro com email duplicado, senha fraca, cadastro sem aceitar termos

---

### T-09 — Login e MFA (RF-02, RF-05)
- [ ] `LoginCommand` — verificar email/senha, checar bloqueio, checar MFA, retornar JWT + refresh token
- [ ] Lógica de bloqueio por tentativas (5 falhas → 15 min)
- [ ] Fluxo de MFA: login retorna `mfa_required` quando ativo; segundo passo valida TOTP
- [ ] `LogAutenticacao` gravado em cada tentativa
- [ ] Teste TDD: login correto, senha errada, conta bloqueada, MFA válido, MFA inválido

---

### T-10 — Refresh token e logout (RF-03, RF-06)
- [ ] `RefreshTokenCommand` — validar token, verificar expiração e revogação, emitir novo access token
- [ ] `LogoutCommand` — revogar refresh token fornecido
- [ ] `LogoutTodosCommand` — revogar todos os refresh tokens da conta
- [ ] Teste TDD: refresh válido, token expirado, token revogado, logout e tentativa de re-uso

---

### T-11 — Social login (RF-04)
- [ ] `LoginSocialCommand` — validar token do provedor (Google/Apple), vincular ou criar conta
- [ ] Integração com Google Identity: verificar `id_token` via JWKS
- [ ] Integração com Apple Sign In: verificar token Apple
- [ ] Teste TDD: novo usuário, usuário existente com mesmo email

---

### T-12 — Recuperação e redefinição de senha (RF-10)
- [ ] `RecuperarSenhaCommand` — gerar token (10 min), enviar email; retornar 200 mesmo se email não existe
- [ ] `RedefinirSenhaCommand` — validar token, atualizar hash de senha, revogar todos os refresh tokens
- [ ] Teste TDD: token válido, token expirado, token já usado

---

### T-13 — MFA: ativar, desativar e reset (RF-05, RF-11)
- [ ] `AtivarMfaCommand` — gerar secret, retornar QR code; confirmar com código TOTP
- [ ] `DesativarMfaCommand` — exigir código TOTP válido, limpar secret
- [ ] `ResetMfaSolicitarCommand` — enviar email com link de reset (10 min)
- [ ] `ResetMfaConfirmarCommand` — validar token, desativar MFA, revogar todos os refresh tokens
- [ ] Teste TDD: ativar, desativar, reset com token válido/expirado

---

## Grupo 5 — Use cases de perfis

### T-14 — CRUD de perfis (RF-07, RF-08, RF-09)
- [ ] `CriarPerfilCommand` — validar limite de 5 perfis, gravar consentimento parental se IsInfantil
- [ ] `ListarPerfisQuery` — retornar todos os perfis da conta autenticada
- [ ] `EditarPerfilCommand` — atualizar nome, avatar, flags
- [ ] `ExcluirPerfilCommand` — exigir confirmação, remover perfil e dados associados
- [ ] Teste TDD: criar 5 perfis (limite), criar 6º (erro), excluir com dados

---

## Grupo 6 — Use cases LGPD

### T-15 — Exportação e exclusão de conta (RF-13)
- [ ] `ExportarDadosCommand` — enfileirar job, retornar 202; job gera JSON e envia por email
- [ ] `ExcluirContaCommand` — verificar senha, excluir dados, anonimizar logs, revogar tokens
- [ ] Teste TDD: exportar dados, excluir conta com senha correta/incorreta

---

## Grupo 7 — Controllers e MAUI

### T-16 — Controllers da API
- [ ] `AuthController` — mapear todos os endpoints de autenticação (RF-01 a RF-11)
- [ ] `PerfisController` — mapear endpoints de perfis (RF-07 a RF-09)
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
