# Visão de Produto — Calegrafia

> App de ensino de caligrafia no estilo Duolingo: estruturado, gamificado e acessível,
> voltado para quem quer aprender ou aprimorar a escrita à mão com caneta/stylus.

**Contexto do projeto:** projeto de estudo e aprendizado de desenvolvimento — sem prazo de lançamento, sem restrição de escopo. Todo o escopo definido neste documento pode e deve ser implementado.

---

## 1. Problema

Não existe uma forma estruturada, progressiva e gamificada de aprender caligrafia digitalmente.
Quem quer aprender depende de tutoriais soltos no YouTube ou cadernos físicos sem feedback.
O Calegrafia resolve isso trazendo treino guiado, progressão clara e acessibilidade (incluindo surdos).

---

## 2. Personas

### Criança (6–12 anos)
- Está aprendendo a escrever na escola
- Precisa de interface lúdica, feedback visual imediato e sessões curtas
- Supervisionada por um adulto (perfil familiar)

### Adolescente / Adulto aprendiz
- Quer melhorar a letra ou aprender letra técnica para uso profissional
- Prefere interface mais sóbria e feedback técnico de qualidade do traço
- Usa sozinho, pode ter conta própria

### Surdo
- Necessita de ditado em Libras (substituindo o áudio)
- Demais funcionalidades idênticas

### Responsável familiar
- Cria a conta principal e gerencia perfis dos filhos
- Acompanha progresso de cada membro da família

---

## 3. Plataformas-Alvo

| Plataforma | Dispositivo | Input |
|---|---|---|
| iOS | iPad + Apple Pencil | Stylus nativa |
| Android | Tablet Android + stylus ativa (S Pen, etc.) | Stylus nativa |
| Windows | Tablet Surface + Surface Pen | Stylus nativa |
| Windows | Desktop + mesa digitalizadora (Wacom, XP-Pen) | Stylus via HID |

**Entrada por dedo não é suportada** — caligrafia requer precisão de caneta.

**Stack:** .NET MAUI (iOS + Android + Windows em único codebase)

---

## 4. Tipos de Letra

- **Cursiva** — escrita ligada, fluída
- **Forma (bastão)** — letras separadas, traços simples
- **Técnica** — letra para uso em desenho técnico / engenharia

---

## 5. Estrutura de uma Lição

Cada lição de caligrafia tem **3 níveis obrigatórios e sequenciais**:

| Nível | Nome | Descrição |
|---|---|---|
| 1 | Guiado | Caminho/guia traçado na tela — usuário escreve por cima |
| 2 | Cópia | Modelo visível mas sem guia — usuário copia abaixo ou ao lado |
| 3 | Ditado | Sem modelo visual — áudio fala a letra/palavra (ou Libras para surdos) |

O usuário só avança ao nível seguinte após atingir a pontuação mínima do atual.

---

## 6. Contas e Perfis

- Um **usuário** cria uma conta (titular da conta)
- Pode criar múltiplos **perfis** dentro da conta (ex: filhos, cônjuge)
- Cada perfil tem progresso, moedas e design independentes
- Design da interface se adapta por perfil: **infantil** ou **padrão**
- A flag é definida manualmente pelo responsável na criação do perfil (modelo Netflix) — sem corte por idade
- Perfil infantil: interface lúdica, elementos visuais animados, fontes maiores
- Perfil padrão: interface sóbria, feedback mais técnico

---

## 7. Gamificação

- Conclusão de lições e níveis gera **moedas** e **XP**
- Moedas acumuladas serão usadas na **lojinha** (fase futura)
- XP acumula e define o nível do usuário (ex: iniciante → intermediário → avançado) — escala a definir
- Sistema de **sequência diária** (streak) — incentivo a prática contínua
- **Conquistas** por marcos (ex: "primeira semana completa", "todas as letras do alfabeto cursivo")
- **Ranking:** não existe no MVP — previsto para versão futura entre usuários

---

## 8. Lojinha (Fase Futura)

- Itens comprados com moedas ganhas no treino
- **Apenas itens cosméticos:** avatares, temas visuais, molduras de perfil, acessórios de personagem
- Sem itens com efeito funcional (não é pay-to-win)

**Roadmap da moeda:**
- MVP: moeda virtual interna (ledger no PostgreSQL, rastreável por transação)
- Fase futura: tokenização como **SPL Token na rede Solana**
- Ticker: **`EZJCALE`** (Enzojb Calegrafia)
- A moeda precisa ser projetada desde o MVP com ledger imutável para suportar a migração

---

## 9. Acessibilidade

- **Ditado em Libras:** nível 3 exibe vídeo gerado por **avatar animado** (ex: VLibras ou equivalente) no lugar do áudio
- Habilitado por configuração no perfil (não detectado automaticamente)

---

## 10. Validação do Traço

O núcleo técnico do app — o sistema que avalia se a letra foi escrita corretamente.

**Critério:** semelhança de forma — comparação do traço do usuário com o path SVG de referência da letra.

**Tolerância por nível:**
- Nível 1 (Guiado): mais tolerante — usuário ainda está aprendendo o movimento
- Nível 2 (Cópia): tolerância média
- Nível 3 (Ditado): mais exigente — usuário deve reproduzir a forma sem auxílio

> ❓ **ABERTO:** Qual a métrica exata de semelhança? (ex: % de sobreposição de área, distância de Hausdorff entre paths, ou outro algoritmo de comparação de curvas bezier?)

---

## 11. Backend

**Infraestrutura existente (VM própria):**
- Nginx como reverse proxy
- .NET (ASP.NET Core) como backend
- PostgreSQL como banco de dados
- Domínio apontando para a VM via Cloudflare

**Autenticação:**
- Email + senha (obrigatório)
- Social login (Google, Apple) — se viável tecnicamente
- MFA opcional por usuário (TOTP ou equivalente)

---

## 12. Conteúdo das Lições

**Criação de conteúdo:** interna — os modelos de letra são criados pela equipe do projeto como **paths SVG** que se aproximam visualmente de letras escritas à mão.

- Cada letra/caractere tem um SVG de referência usado para:
  1. Exibir o guia no nível 1
  2. Exibir o modelo no nível 2
  3. Comparar com o traço do usuário na validação

**Escopo do conteúdo (MVP completo):**

| Categoria | Conteúdo |
|---|---|
| Letras | Alfabeto completo — maiúsculas e minúsculas |
| Números | 0–9 |
| Palavras | Nomes de animais, objetos e palavras corriqueiras |
| Frases | Frases formadas com as palavras aprendidas (usadas no ditado) |

Os 3 tipos de letra (cursiva, forma, técnica) se aplicam a todas as categorias acima.

---

## 13. Fora do Escopo (por enquanto)

- Lojinha (fase 2, após gamificação estável)
- Tokenização da moeda em Solana (fase 3, após lojinha)
- Versão web / browser
- Entrada por dedo
- Multiplayer / colaboração em tempo real
- Ranking entre usuários (previsto, sem data)

---

## 14. Decisões Técnicas Tomadas

| Decisão | Escolha | Motivo |
|---|---|---|
| Perfil infantil | Flag manual na criação do perfil (modelo Netflix) | Sem corte por idade — decisão do responsável; evita discriminação por data de nascimento |
| Tipos de letra | Cursiva, Forma, Técnica | Cobre os principais contextos de uso (escolar, profissional, técnico) |
| Plataforma | .NET MAUI | Cobre iOS, Android e Windows num codebase; suporte nativo a stylus via PointerDevice |
| Input mínimo | Stylus/caneta obrigatória | Caligrafia requer precisão — dedo não é adequado |
| Acessibilidade surdos | Libras via avatar animado no ditado | Sem dependência de gravações com intérprete humano; escalável |
| Validação do traço | Semelhança de forma (SVG) com tolerância crescente por nível | Critério mais relevante para caligrafia; tolerância crescente respeita a curva de aprendizado |
| Gamificação | Moedas + XP; ranking futuro | Moedas para lojinha, XP para progressão; ranking adiado para não complicar o MVP |
| Backend | VM própria: Nginx + ASP.NET Core + PostgreSQL + Cloudflare | Infraestrutura já provisionada, sem custo de nuvem gerenciada |
| Autenticação | Email/senha + social login + MFA opcional | Flexibilidade para o usuário; MFA como camada extra de segurança opt-in |
| Conteúdo das lições | SVGs criados internamente | Controle total sobre estilo visual; sem dependência de terceiros |
| Escopo de conteúdo | Alfabeto completo, números, animais, objetos, palavras e frases | Cobre letras, números e contexto real de uso desde o MVP |
| Lojinha | Apenas cosméticos (avatares, temas, molduras, acessórios) | Sem pay-to-win; mantém equidade na progressão |
| Moeda — fase futura | SPL Token na rede Solana, ticker `EZJCALE` | Fees de frações de centavo viabilizam micro-recompensas; ticker único vinculado à marca |
| Ledger da moeda | Tabela de transações imutável desde o MVP | Cada ganho/gasto registrado individualmente para suportar migração futura para blockchain |
