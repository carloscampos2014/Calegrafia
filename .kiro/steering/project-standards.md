---
inclusion: always
---

# Padrões do Projeto — Calegrafia

Referência central de padrões técnicos e de produto para todas as sessões de desenvolvimento.
Consultar antes de implementar qualquer feature. Referência completa: `docs/vision.md`.

---

## Identidade do Projeto

| Campo | Valor |
|---|---|
| Nome | Enzojb Calegrafia |
| Tipo | App de ensino de caligrafia (estilo Duolingo) |
| Contexto | Projeto de estudo/aprendizado — sem prazo, escopo completo |
| Repositório | `carloscampos2014/Calegrafia` |
| GitHub Project | [#5 — Enzojb Calegrafia](https://github.com/users/carloscampos2014/projects/5) |

---

## Stack Técnica

| Camada | Tecnologia | Observação |
|---|---|---|
| Frontend / App | .NET MAUI | iOS + Android + Windows num único codebase |
| Backend API | ASP.NET Core | RESTful, hospedado na VM |
| Banco de dados | PostgreSQL | Rodando localmente na VM |
| Proxy reverso | Nginx | Expõe a API via HTTPS |
| DNS / CDN / SSL | Cloudflare | Aponta para a VM |
| Autenticação | JWT + Identity | Email/senha + social login + MFA opcional (TOTP) |
| Token / Blockchain | Solana SPL Token | Fase futura — ticker `EZJCALE` |

---

## Estrutura de Solução

```
Calegrafia.sln
├── src/
│   ├── Calegrafia.App/           # Projeto MAUI (frontend iOS/Android/Windows)
│   ├── Calegrafia.Api/           # ASP.NET Core Web API
│   ├── Calegrafia.Application/   # Use cases, serviços de aplicação
│   ├── Calegrafia.Domain/        # Entidades, value objects, interfaces
│   └── Calegrafia.Infrastructure/# Repositórios, PostgreSQL, serviços externos
└── tests/
    ├── Calegrafia.Application.Tests/
    └── Calegrafia.Api.Tests/
```

---

## Padrões por Camada

### MAUI (App)
- Padrão: **MVVM** — `ContentPage` + `ViewModel` + `Model`
- Estilos centralizados em `ResourceDictionary` (sem estilos inline)
- Temas: `InfantilTheme` (lúdico, fontes maiores) e `PadraoTheme` (sóbrio)
- Tela de exercício de escrita: canvas com captura de `PointerDevice.Stylus`
- **Entrada por dedo não é suportada** — validar input type antes de processar traço

### API (ASP.NET Core)
- Estrutura: `Controller` → `IService` → `IRepository`
- Sem lógica de negócio nos controllers — apenas validação de entrada e resposta HTTP
- Retornos padronizados: `200 OK`, `201 Created`, `400 Bad Request`, `401 Unauthorized`, `404 Not Found`
- Autenticação via JWT Bearer em todas as rotas protegidas

### Domínio
- Entidades ricas — comportamento nos objetos de domínio, não em serviços anêmicos
- Value objects para conceitos com identidade por valor (ex: `Traço`, `PontuacaoLicao`)
- Sem dependências de infraestrutura no domínio

### Banco de Dados
- Migrations versionadas via EF Core — sem DDL manual
- Naming: tabelas em `snake_case`, ex: `perfis_usuario`, `licoes`, `transacoes_moeda`
- Ledger de moeda: tabela `transacoes_moeda` **imutável** — apenas INSERT, nunca UPDATE/DELETE

---

## Módulos e Ordem de Implementação

| # | Módulo | Branch padrão | Status |
|---|---|---|---|
| 1 | Autenticação + Perfis | `feature/modulo-1-auth-perfis` | ⬜ Pendente |
| 2 | Engine de Escrita (canvas, stylus, SVG) | `feature/modulo-2-engine-escrita` | ⬜ Pendente |
| 3 | Estrutura de Lições (3 níveis, progressão) | `feature/modulo-3-licoes` | ⬜ Pendente |
| 4 | Gamificação (moedas, XP, streak, conquistas) | `feature/modulo-4-gamificacao` | ⬜ Pendente |
| 5 | Lojinha (cosméticos, ledger) | `feature/modulo-5-lojinha` | ⬜ Pendente |
| 6 | Acessibilidade Libras (avatar no ditado) | `feature/modulo-6-libras` | ⬜ Pendente |
| 7 | Tokenização EZJCALE (SPL Solana) | `feature/modulo-7-token` | ⬜ Pendente |

---

## Regras de Domínio Críticas

### Perfis
- Uma conta pode ter múltiplos perfis
- Cada perfil tem flag `IsInfantil` (bool) — definida manualmente pelo responsável na criação
- Progresso, moedas e XP são **por perfil**, não por conta

### Lições
- Cada lição tem exatamente **3 níveis** — Guiado (1), Cópia (2), Ditado (3)
- Avanço de nível só ocorre após atingir pontuação mínima do nível atual
- Conteúdo em SVG — cada letra/caractere tem um path SVG de referência

### Validação de Traço
- Critério: **semelhança de forma** — comparação do traço com o SVG de referência
- Tolerância aumenta conforme o nível: nível 1 = mais tolerante, nível 3 = mais exigente
- Nunca aceitar traço de dedo — verificar `PointerDeviceType` antes de avaliar

### Moeda (EZJCALE)
- Cada ganho e gasto é um registro separado na tabela `transacoes_moeda`
- Nunca somar/subtrair diretamente num campo `saldo` — calcular saldo via SUM do ledger
- Preparado para migração futura para SPL Token na Solana

### Libras
- Ativado por flag `UsaLibras` no perfil — não detectado automaticamente
- No nível 3 (Ditado): exibir vídeo de avatar animado (VLibras ou equivalente) no lugar do áudio

---

## Conteúdo das Lições

| Categoria | Escopo |
|---|---|
| Letras | Alfabeto completo — maiúsculas e minúsculas |
| Números | 0–9 |
| Palavras | Nomes de animais, objetos e palavras corriqueiras |
| Frases | Frases formadas com as palavras aprendidas (ditado) |

Os 3 tipos de letra (cursiva, forma, técnica) se aplicam a todas as categorias.

---

## Padrões de Nomenclatura

| Contexto | Padrão | Exemplo |
|---|---|---|
| Classes C# | PascalCase | `PerfilUsuario`, `LicaoService` |
| Métodos C# | PascalCase | `ObterLicaoAsync`, `ValidarTraco` |
| Propriedades | PascalCase | `IsInfantil`, `SaldoMoeda` |
| Variáveis locais | camelCase | `perfilAtivo`, `tracoCapturado` |
| Tabelas PostgreSQL | snake_case | `perfis_usuario`, `transacoes_moeda` |
| Colunas PostgreSQL | snake_case | `is_infantil`, `criado_em` |
| Endpoints API | kebab-case | `/api/perfis`, `/api/licoes/{id}/niveis` |
| Branches Git | kebab-case | `feature/modulo-1-auth-perfis` |
| Commits | Conventional Commits | `feat(modulo-1): #N descrição` |

---

## Segurança

- Nunca commitar: `.env`, `appsettings.*.json` com secrets, `vm-oracle.md`, chaves privadas
- `mcp.json` e `vm-oracle.md` estão no `.gitignore` — não subir ao repo
- JWT: expiração curta com refresh token
- MFA: TOTP (Google Authenticator / Authy compatível) — opt-in por usuário
- Senhas: hash com BCrypt (nunca MD5/SHA1)
- SQL: sempre via EF Core parametrizado — sem string concatenation em queries

---

## Comandos do Projeto

```powershell
# Build completo
dotnet build Calegrafia.sln -c Debug

# Testes
dotnet test tests/ --logger "console;verbosity=minimal"

# Migrations (quando banco estiver configurado)
dotnet ef migrations add <Nome> --project src/Calegrafia.Infrastructure
dotnet ef database update --project src/Calegrafia.Infrastructure
```

---

## Referências

- Visão completa do produto: `docs/vision.md`
- Arquitetura detalhada (a criar): `docs/ARCHITECTURE.md`
- Workflow de desenvolvimento: `.kiro/steering/workflow-desenvolvimento.md`
- Padrões de commit: `.kiro/steering/git-commits.md`
