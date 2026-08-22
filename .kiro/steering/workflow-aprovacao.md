---
inclusion: always
---

# Workflow de Aprovação antes de Implementar

Antes de implementar qualquer módulo, feature ou conjunto de mudanças não triviais, o agente DEVE:

1. **Enviar um briefing** ao usuário com:
   - O que será criado/modificado (lista de arquivos)
   - O que cada parte faz (descrição concisa)
   - Dependências ou decisões de design relevantes
   - O que NÃO será feito (escopo negativo, se houver)

2. **Aguardar aprovação explícita** do usuário ("pode implementar", "aprovado", "sim", etc.) antes de escrever qualquer arquivo de código.

3. Só após aprovação: implementar, buildar e testar conforme os critérios do módulo.

**Este fluxo se aplica a:**
- Implementação de módulos do plano de desenvolvimento (Autenticação, Engine de Escrita, Lições, Gamificação, Lojinha, Libras, Tokenização)
- Novas features ou refactors de múltiplos arquivos
- Mudanças em arquitetura ou configuração de build/CI/infraestrutura

**Não se aplica a:**
- Correções de build/lint/teste já em andamento
- Ajustes de arquivos de configuração simples (ex: .gitignore, appsettings, mcp.json)
- Perguntas, explicações, análises ou atualização de docs sem escrita de código
