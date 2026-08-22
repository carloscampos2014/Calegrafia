# Requirements — Módulo 1: Autenticação + Perfis

## Contexto

Módulo base do Calegrafia. Toda interação com o app depende de uma conta autenticada
com pelo menos um perfil ativo. Este módulo cobre o ciclo completo: criar conta,
autenticar, gerenciar perfis familiares e proteger o acesso.

---

## Requisitos Funcionais

### RF-01 — Cadastro de conta

- O usuário pode criar uma conta com email e senha
- Email deve ser único no sistema
- Senha deve ter no mínimo 8 caracteres, com ao menos 1 letra maiúscula, 1 minúscula e 1 número
- Após o cadastro, o sistema envia email de confirmação
- A conta só fica ativa após confirmação do email

**Critérios de aceite:**
- [ ] Cadastro com email válido e senha forte cria conta com status `pendente`
- [ ] Email de confirmação é enviado após o cadastro
- [ ] Tentativa de cadastro com email já existente retorna erro 400 com mensagem descritiva
- [ ] Senha fraca retorna erro 400 com descrição dos critérios não atendidos

---

### RF-02 — Login com email e senha

- O usuário pode autenticar com email e senha
- Autenticação bem-sucedida retorna JWT (access token) e refresh token
- Access token expira em 15 minutos
- Refresh token expira em 30 dias
- Conta não confirmada não pode fazer login

**Critérios de aceite:**
- [ ] Login com credenciais válidas retorna `access_token` e `refresh_token`
- [ ] Login com senha incorreta retorna 401 sem revelar se o email existe
- [ ] Login com conta não confirmada retorna 403 com mensagem orientando o usuário
- [ ] Após 5 tentativas falhas consecutivas, a conta é bloqueada por 15 minutos

---

### RF-03 — Refresh de token

- O app pode renovar o access token usando o refresh token antes do vencimento
- Refresh token revogado ou expirado retorna 401 e força novo login

**Critérios de aceite:**
- [ ] Refresh com token válido retorna novo `access_token`
- [ ] Refresh com token expirado retorna 401
- [ ] Refresh com token revogado retorna 401

---

### RF-04 — Social login (Google e Apple)

- O usuário pode autenticar via Google ou Apple
- Se o email do social login já existe no sistema, as contas são vinculadas
- Se não existe, uma nova conta é criada e ativada automaticamente (sem confirmação de email)

**Critérios de aceite:**
- [ ] Login via Google com token válido autentica ou cria conta
- [ ] Login via Apple com token válido autentica ou cria conta
- [ ] Conta existente com mesmo email é vinculada ao provedor social

---

### RF-05 — MFA (opcional)

- O usuário pode habilitar MFA TOTP nas configurações da conta
- Com MFA ativo, o login exige o código TOTP após a senha
- O usuário pode gerar o QR code para configurar no autenticador
- O usuário pode desabilitar o MFA fornecendo o código TOTP atual

**Critérios de aceite:**
- [ ] Habilitação do MFA gera QR code TOTP compatível com Google Authenticator / Authy
- [ ] Login com MFA ativo exige código TOTP válido após a senha
- [ ] Código TOTP inválido retorna 401
- [ ] Desabilitação do MFA requer código TOTP válido

---

### RF-06 — Logout

- O usuário pode fazer logout, revogando o refresh token ativo
- Logout de todos os dispositivos revoga todos os refresh tokens da conta

**Critérios de aceite:**
- [ ] Logout revoga o refresh token fornecido
- [ ] Após logout, tentativa de refresh com o token revogado retorna 401
- [ ] Logout de todos os dispositivos revoga todos os refresh tokens da conta

---

### RF-07 — Criação de perfis

- Uma conta pode ter até 5 perfis
- Cada perfil tem: nome, avatar (padrão ou customizado), flag `IsInfantil` (bool), flag `UsaLibras` (bool)
- O primeiro perfil é criado durante o onboarding, após o login
- O responsável pela conta pode criar, editar e excluir perfis adicionais

**Critérios de aceite:**
- [ ] Criação de perfil com nome e configurações válidas salva e retorna o perfil criado
- [ ] Flag `IsInfantil` ativa o tema infantil ao selecionar o perfil
- [ ] Flag `UsaLibras` ativa o modo Libras no ditado ao usar o perfil
- [ ] Tentativa de criar 6º perfil retorna 400 com mensagem de limite atingido
- [ ] Exclusão de perfil remove progresso, moedas e XP associados (operação irreversível com confirmação)

---

### RF-08 — Seleção de perfil

- Ao abrir o app (autenticado), o usuário vê a tela de seleção de perfil
- O perfil selecionado é o contexto ativo para toda a sessão de uso

**Critérios de aceite:**
- [ ] Tela de seleção exibe todos os perfis da conta com nome e avatar
- [ ] Seleção de perfil define o perfil ativo na sessão
- [ ] Perfil infantil exibe tema infantil imediatamente após seleção
- [ ] Perfil com `UsaLibras` ativo exibe indicador visual na tela de seleção

---

### RF-09 — Edição de perfil

- O responsável pode editar nome, avatar, `IsInfantil` e `UsaLibras` de qualquer perfil

**Critérios de aceite:**
- [ ] Edição de nome e flags é salva e refletida imediatamente
- [ ] Troca de `IsInfantil` altera o tema na próxima seleção do perfil

---

### RF-10 — Recuperação de senha

- O usuário pode solicitar redefinição de senha via email
- O link de redefinição expira em 10 minutos
- Após redefinição, todos os refresh tokens são revogados

**Critérios de aceite:**
- [ ] Solicitação com email válido envia link de redefinição
- [ ] Solicitação com email inexistente retorna 200 sem revelar a existência do email (segurança)
- [ ] Link expirado retorna erro descritivo
- [ ] Redefinição bem-sucedida revoga todos os refresh tokens ativos

---

### RF-11 — Reset de TOTP por email

- Se o usuário perder acesso ao autenticador, pode solicitar reset do MFA via email
- O fluxo: informar email → receber link de reset → clicar no link → MFA desativado e todos os refresh tokens revogados
- O link expira em 10 minutos
- Após o reset, o usuário pode reconfigurar o MFA normalmente

**Critérios de aceite:**
- [ ] Solicitação de reset com email válido e MFA ativo envia link de reset
- [ ] Solicitação com email sem MFA ativo retorna 200 sem ação (não revela estado da conta)
- [ ] Link válido desativa o MFA e revoga todos os refresh tokens
- [ ] Link expirado retorna erro descritivo
- [ ] Após reset, usuário consegue fazer login com email e senha sem TOTP

---

### RF-12 — Consentimento e LGPD (coleta de dados)

- No cadastro, o usuário deve aceitar explicitamente os Termos de Uso e a Política de Privacidade
- O aceite é registrado com timestamp, versão do documento e IP
- Os checkboxes de aceite não podem vir pré-marcados
- A finalidade da coleta de dados deve ser informada na tela de cadastro de forma clara e acessível
- Ao criar um perfil com `IsInfantil = true`, o responsável deve confirmar que é o titular da conta e responsável legal pelo menor (consentimento parental)

**Critérios de aceite:**
- [ ] Cadastro sem aceitar os termos retorna 400 com mensagem descritiva
- [ ] O aceite é gravado na tabela `consentimentos` com timestamp, versão e IP
- [ ] Checkboxes de aceite chegam desmarcados por padrão no app
- [ ] Criação de perfil infantil exibe confirmação de consentimento parental e registra o aceite

---

### RF-13 — Direitos do titular (LGPD Art. 18)

- O usuário pode solicitar **exportação de todos os seus dados** (portabilidade)
  - Retorna JSON com: dados da conta, perfis e histórico de progresso
  - O arquivo é gerado assincronamente e enviado por email em até 72h
- O usuário pode solicitar **exclusão da conta** (direito ao esquecimento)
  - Remove: conta, perfis, progresso, moedas, tokens, consentimentos
  - Dados de log de autenticação são anonimizados (não removidos — obrigação legal de auditoria)
  - A exclusão é irreversível e exige confirmação com senha atual
  - Após exclusão, todos os refresh tokens são revogados imediatamente
- O usuário pode solicitar **correção de dados** — já coberto por RF-09 (edição de perfil) e configurações da conta

**Critérios de aceite:**
- [ ] Solicitação de exportação enfileira o job e retorna 202 Accepted
- [ ] Email com o arquivo de dados é enviado em até 72h
- [ ] Solicitação de exclusão com senha correta inicia o processo de remoção
- [ ] Solicitação de exclusão com senha incorreta retorna 401
- [ ] Após exclusão, tentativa de login com o email retorna 404 (conta não encontrada)
- [ ] Logs de autenticação são anonimizados, não removidos

---

## Requisitos Não Funcionais

- **RNF-01:** Senhas armazenadas com BCrypt (fator mínimo 12)
- **RNF-02:** Tokens JWT assinados com RS256 (chave assimétrica)
- **RNF-03:** Comunicação exclusivamente via HTTPS
- **RNF-04:** Logs de todas as tentativas de autenticação (sucesso e falha) com IP e timestamp
- **RNF-05:** Rate limiting no endpoint de login: máx 10 req/min por IP
- **RNF-06:** Dados de perfil sincronizados com o servidor — sem dependência de cache offline para autenticação
- **RNF-07:** Registros de consentimento imutáveis — tabela `consentimentos` com INSERT apenas, nunca UPDATE/DELETE
- **RNF-08:** Dados de menores (perfis com `IsInfantil = true`) não podem ser usados para fins de marketing ou analytics sem consentimento explícito adicional
- **RNF-09:** Tempo de retenção de logs de autenticação: 2 anos (obrigação legal); após este prazo, anonimização automática

---

## Fora do Escopo deste Módulo

- Progresso de lições (Módulo 3)
- Moedas e XP (Módulo 4)
- Avatar customizado via lojinha (Módulo 5)
- Ranking entre usuários (futuro)
