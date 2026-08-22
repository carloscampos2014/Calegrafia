---
inclusion: always
---

# Workflow de Desenvolvimento — Ciclo Completo de Módulo/Feature

Este documento define o fluxo obrigatório para qualquer implementação de módulo, feature ou conjunto de mudanças não triviais no projeto Calegrafia. O agente DEVE seguir cada etapa na ordem definida, sem pular etapas.

---

## Módulos do Projeto

O projeto está organizado nos seguintes módulos, a serem implementados nesta ordem:

| # | Módulo | Branch padrão |
|---|---|---|
| 1 | Autenticação + Perfis | `feature/modulo-1-auth-perfis` |
| 2 | Engine de Escrita (canvas, stylus, validação SVG) | `feature/modulo-2-engine-escrita` |
| 3 | Estrutura de Lições (conteúdo, 3 níveis, progressão) | `feature/modulo-3-licoes` |
| 4 | Gamificação (moedas, XP, streak, conquistas) | `feature/modulo-4-gamificacao` |
| 5 | Lojinha (cosméticos, ledger de moeda) | `feature/modulo-5-lojinha` |
| 6 | Acessibilidade Libras (avatar animado no ditado) | `feature/modulo-6-libras` |
| 7 | Tokenização EZJCALE (SPL Token Solana) | `feature/modulo-7-token` |

---

## Etapas do ciclo

### Etapa 1 — Criar branch a partir do master atualizado

```powershell
git fetch origin
git checkout master
git pull origin master
git checkout -b feature/<nome-do-modulo-ou-feature>
```

- Nunca implementar diretamente no `master`
- Para sub-features dentro de um módulo: `feature/modulo-X-descricao-especifica`

---

### Etapa 2 — Publicar a branch no GitHub imediatamente

```powershell
git push -u origin feature/<nome>
```

- Publicar antes de qualquer commit de código
- Confirmar que a branch existe no remoto antes de continuar

---

### Etapa 3 — Gerar o Briefing

Antes de escrever qualquer arquivo de código, o agente DEVE emitir um briefing ao usuário seguindo o padrão definido em `briefing-detalhado.md`, contendo:

- **O que será criado/modificado** — lista de arquivos com descrição de cada um
- **O que cada parte faz** — responsabilidade de cada componente
- **Decisões de design relevantes** — padrões, trade-offs, dependências entre módulos
- **O que NÃO será feito** — escopo negativo explícito

---

### Etapa 4 — Aguardar aprovação explícita

O agente DEVE pausar e aguardar o usuário dizer: "aprovado", "pode implementar", "sim", "ok" ou equivalente.

- **Sem aprovação = sem código**
- Se o usuário pedir mudanças no briefing, revisar e aguardar nova aprovação
- Correções de build/lint/teste já em andamento não precisam de nova aprovação

---

### Etapa 5 — Implementar

- Seguir a stack definida: **.NET MAUI** (frontend iOS/Android/Windows) + **ASP.NET Core** (backend API) + **PostgreSQL** (banco)
- Backend exposto via **Nginx** na VM, domínio configurado no **Cloudflare**
- Manter coerência com a arquitetura dos módulos já implementados
- Cada feature é implementada de forma atômica e completa

**Padrões por camada:**
- MAUI: MVVM, ContentPage por tela, styles em ResourceDictionary
- API: Controllers → Services → Repositories (sem lógica no controller)
- Banco: migrations versionadas, sem DDL manual

---

### Etapa 6 — Commit por feature/tarefa implementada

```powershell
git add <arquivos-relevantes>
git commit -m "feat(modulo-X): #N descrição do que foi implementado"
```

Tipos de commit:
- `feat(modulo-X):` — nova funcionalidade do módulo
- `fix:` — correção de bug
- `docs:` — documentação
- `chore:` — configuração / infraestrutura
- `tests:` — adição ou ajuste de testes

Incluir `#N` quando o commit avança ou fecha uma issue.

---

### Etapa 7 — Verificar build e testes

Após cada commit (ou grupo coeso), verificar:

```powershell
# Build da solution completa
dotnet build Calegrafia.sln -c Debug

# Testes
dotnet test tests/ --logger "console;verbosity=minimal"
```

- Build deve passar sem erros
- Todos os testes existentes devem continuar aprovados
- Novos testes devem cobrir nova lógica de negócio (exceto se dispensado explicitamente)

---

### Etapa 8 — Avaliar necessidade de testes manuais

Após verificação automatizada, avaliar se há cenários que precisam de validação manual:

- **Engine de escrita** — testar captura de stylus no dispositivo real ou emulador
- **Validação de traço SVG** — verificar tolerância por nível com traços reais
- **Autenticação / MFA** — depende de estado real do backend
- **Integração com Solana** (módulo 7) — requer rede devnet/testnet configurada

Se testes manuais forem necessários, informar o usuário e aguardar confirmação antes do push.

---

### Etapa 9 — Atualizar documentação

Antes do push, atualizar a documentação afetada:

**Sempre verificar:**
- `docs/vision.md` — se alguma decisão mudou durante a implementação
- `README.md` — atualizar status do módulo na tabela de progresso
- `docs/ARCHITECTURE.md` — documentar novos componentes, decisões de design (criar se não existir)

```powershell
git add docs/ README.md
git commit -m "docs: atualizar documentacao para modulo-X"
```

---

### Etapa 10 — Push da branch

Somente após build, testes e documentação atualizados:

```powershell
git push origin feature/<nome>
```

---

### Etapa 11 — Criar Pull Request

```powershell
gh pr create `
  --base master `
  --head feature/<nome> `
  --title "feat(modulo-X): Descrição concisa (máx 70 chars)" `
  --body "..."
```

**Body do PR deve conter:**
- Resumo do que foi implementado
- Lista de mudanças por área (MAUI, API, banco, infra)
- Resultado dos testes (build ✅, N testes passando)
- `Closes #N` para cada issue fechada

---

### Etapa 12 — Após merge: limpar repositório local

Quando o usuário sinalizar que o PR foi mergeado:

```powershell
git checkout master
git pull origin master
git remote prune origin
git branch -D feature/<nome>
```

---

## Resumo visual do ciclo

```
master atualizado
      ↓
  criar branch  →  publicar branch
      ↓
  briefing  →  aguardar aprovação
      ↓
  implementar (MAUI + ASP.NET Core + PostgreSQL)
      ↓  (loop por feature/tarefa)
  commit  →  build  →  testes
      ↓
  testes manuais? (stylus, SVG, auth, Solana)
      ↓
  atualizar documentação (vision + README + ARCHITECTURE)
      ↓
  push  →  criar PR (com Closes #N)
      ↓
  aguardar merge
      ↓
  pull master  →  limpar branches locais
```

---

## Exceções ao workflow

As etapas 3 e 4 (briefing + aprovação) **não se aplicam** a:
- Correções de build ou testes já em andamento
- Ajustes simples em arquivos de configuração (`.gitignore`, `appsettings.json`, hooks)
- Respostas a erros identificados durante a execução

Todas as outras etapas são obrigatórias sem exceção.
