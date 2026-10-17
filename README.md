# Concessionária: observabilidade em microsserviços .NET

Projeto acadêmico que simula o sistema de uma concessionária de veículos, dividido em microsserviços, para
**injetar falhas de propósito e mostrar como elas são detectadas e diagnosticadas com os três pilares da
observabilidade: métricas, logs e traces**.

Foi desenvolvido para a Atividade 04 (Projeto Básico de Observabilidade) da disciplina de Gerenciamento e
Monitoramento de Aplicações e Infraestrutura, da Pós-Graduação Lato Sensu em Engenharia DevOps do Instituto
Federal de Mato Grosso (IFMT). O resultado foi descrito no artigo *"Observabilidade em microsserviços .NET:
métricas, logs e traces na detecção de falhas de uma concessionária"*.

> O produto é simples de propósito: o foco não são as regras de negócio, e sim a telemetria que a aplicação
> produz e o que ela revela quando algo dá errado.

## Sumário

- [O que a aplicação faz](#o-que-a-aplicação-faz)
- [Arquitetura](#arquitetura)
- [Como a observabilidade foi montada](#como-a-observabilidade-foi-montada)
- [Como executar](#como-executar)
- [Cenários de falha](#cenários-de-falha)
- [Resultados](#resultados)
- [Estrutura do repositório](#estrutura-do-repositório)
- [Testes](#testes)

## O que a aplicação faz

- Cadastro, consulta, edição e exclusão de **carros** e **motos**.
- Registro de **vendas** (nome do comprador, CPF ou CNPJ e valor).
- **Painel** com indicadores de estoque e de vendas.
- **Planilhas Excel** de vendas e de estoque.

Regras de negócio: um veículo vendido não pode ser editado, excluído nem vendido de novo (409); a venda exige
nome, CPF/CNPJ com 11 ou 14 dígitos e valor positivo (400).

## Arquitetura

```
                 ┌──────────────────────── aplicação ────────────────────────┐
navegador ──▶ web (React) ──▶ BFF ─┬─▶ Carros ─┬─▶ MongoDB (concessionaria_carros)
                                   │           └─▶ Relatórios
                                   └─▶ Motos  ─┬─▶ MongoDB (concessionaria_motos)
                                               └─▶ Relatórios

                 ┌──────────────────── observabilidade ──────────────────────┐
APIs ── logs e traces (OTLP) ──▶ OpenTelemetry Collector ─┬─▶ Loki  (logs)
                                                          └─▶ Tempo (traces)
APIs ◀── coleta de /metrics a cada 15 s ── Prometheus (métricas e alertas)
Grafana ──▶ consulta Prometheus, Loki e Tempo em uma única tela
```

| Serviço | Pasta | Papel |
|---|---|---|
| Web | `monitoramento-web` | Interface em React + Vite + MUI. Conversa **só** com o BFF |
| BFF | `monitoramento-bff` | *Backend for Frontend*: única API exposta ao navegador; agrega o painel e as vendas |
| Carros | `monitoramento-carros` | CRUD de carros e vendas; pede indicadores e planilhas a Relatórios |
| Motos | `monitoramento-motos` | Mesmo contrato de Carros, com cilindrada no lugar de portas |
| Relatórios | `monitoramento-relatorios` | Sem banco: recebe dados e devolve indicadores ou o arquivo `.xlsx` |

Os três fluxos que atravessam os serviços, e que aparecem nos traces:

- **Painel:** BFF → Carros e Motos em paralelo → MongoDB + Relatórios. Se Relatórios cair, o painel é
  entregue **sem indicadores** em vez de falhar (degradação).
- **Venda:** BFF → Carros ou Motos → MongoDB.
- **Planilha:** BFF → Carros ou Motos (lê o MongoDB) → Relatórios gera o arquivo → volta pela cadeia.

As chamadas entre serviços usam **Refit** com a resiliência do **Polly** (`AddStandardResilienceHandler`):
tempo limite por tentativa e total, novas tentativas e circuit breaker. Os tempos são escalonados para que
quem chama sempre espere mais do que o total de quem é chamado (BFF → Carros/Motos: 10 s / 15 s;
Carros/Motos → Relatórios: 3 s / 8 s).

**Tecnologias:** .NET 10 (ASP.NET Core Minimal API + Carter), MongoDB 8, React, Docker Compose.

## Como a observabilidade foi montada

### Nas aplicações

Configuração em `Comum/Observabilidade/` de cada API (duplicada de propósito: cada microsserviço é independente).

| Pilar | Como | Destino |
|---|---|---|
| **Métricas** | OpenTelemetry: HTTP servidor e cliente, runtime do .NET (CPU, memória, GC, threads), eventos do Polly e métricas de negócio `concessionaria_*` | `/metrics` → Prometheus |
| **Logs** | Serilog estruturado: console, arquivo (7 dias) e OTLP | Collector → Loki |
| **Traces** | OpenTelemetry: ASP.NET Core, HttpClient (cada retry vira um span), driver do MongoDB e spans próprios | Collector → Tempo |

**Correlação:** toda requisição recebe um identificador de operação (cabeçalho `X-Id-Operacao`), repassado
entre os serviços, gravado em todo log e no span, e devolvido na resposta de erro (o front mostra esse id ao
usuário). O `TraceId` também vai em cada log. No Grafana, dá para ir de um log ao trace e do trace aos logs.

### Fora das aplicações

| Ferramenta | Versão | Papel |
|---|---|---|
| OpenTelemetry Collector | 0.162.0 | Recebe logs e traces das APIs e encaminha ao Loki e ao Tempo |
| Prometheus | 3.15.0 | Coleta as métricas e avalia as regras de alerta |
| Grafana Loki | 3.7.8 | Armazena os logs |
| Grafana Tempo | 3.1.0 | Armazena os traces e gera o mapa de dependências entre serviços |
| Grafana | 13.2.3 | Dashboard e exploração de logs e traces |
| Toxiproxy | 2.12.0 | Proxy entre Carros/Motos e suas dependências, para injetar latência e cortes |
| k6 | 2.3.0 | Gera o tráfego de fundo e a carga alta dos cenários |

### Dashboard

**"Concessionária — Visão geral"** (Grafana), organizado do sintoma à causa: visão geral (disponibilidade,
requisições/s, erro 5xx, p95, painel degradado e alertas ativos) → tráfego, erros e latência → integrações e
resiliência → recursos dos processos → negócio → logs e traces com erro. O JSON é gerado por
`observabilidade/grafana/gerar-dashboard.mjs`.

### Alertas

Regras do Prometheus em `observabilidade/prometheus/alertas.yml`. Não há Alertmanager: os alertas aparecem no
Prometheus e no dashboard, mas não geram notificação.

| Alerta | Condição | Severidade |
|---|---|---|
| `ServicoFora` | serviço não responde à coleta (`up == 0`) por 30 s | crítica |
| `TaxaErro5xxAlta` | mais de 5% de respostas 5xx em 2 min | crítica |
| `LatenciaP95Alta` | p95 acima de 500 ms em 2 min | aviso |
| `CpuAlta` | CPU do processo acima de 80% por 2 min | aviso |
| `MemoriaAlta` | memória acima de 90% do limite do contêiner | aviso |
| `FalhaIntegracaoRelatorios` | falha ao chamar Relatórios nos últimos 2 min | aviso |
| `PainelDegradado` | painel entregue sem indicadores nos últimos 2 min | aviso |
| `RetriesFrequentes` | mais de 10 retries em 2 min para o mesmo destino | aviso |

## Como executar

**Pré-requisito:** Docker Desktop (ou Docker Engine com Compose).

```bash
# Sobe tudo: aplicação (4 APIs + web + MongoDB + Toxiproxy) e stack de observabilidade
docker compose --profile app up -d --build

# Derruba tudo (acrescente -v para apagar também os dados)
docker compose --profile app down
```

Em ambiente de desenvolvimento, as APIs de Carros e Motos já criam 8 veículos de exemplo cada.

| O quê | Endereço |
|---|---|
| Aplicação web | http://localhost:5173 |
| BFF (API) | http://localhost:5101/api/ |
| Grafana | http://localhost:3000 (leitura sem login; para usar o Explore: `admin` / `admin`) |
| Prometheus (alvos e alertas) | http://localhost:9090/targets e http://localhost:9090/alerts |

Para gerar tráfego contínuo e ver o dashboard em movimento:

```bash
docker compose run --rm -e TAXA=5 -e DURACAO=5m k6 run /cenarios/k6/trafego.js
```

<details>
<summary>Rodar as APIs fora do Docker (desenvolvimento)</summary>

Requer .NET 10 SDK, Node.js e MongoDB local em `localhost:27017` sem autenticação.

```bash
docker compose up -d                          # só a stack de observabilidade
cd monitoramento-carros && dotnet run --launch-profile http   # repetir para bff, motos e relatorios
cd monitoramento-web && npm install && npm run dev
```

Portas: BFF 5101, Carros 5128, Motos 5010, Relatórios 5051, web 5173. Não misture os dois modos: com o
perfil `app` no ar, essas portas já estão ocupadas.
</details>

## Cenários de falha

O script `cenarios/executar-cenario.ps1` (PowerShell, com o perfil `app` no ar) automatiza cada experimento:

1. sobe tráfego de fundo com o k6 (5 req/s, com a mistura de uso da concessionária);
2. espera uma linha de base sem alertas;
3. **injeta a falha**;
4. a cada 5 s, sonda o sistema como um usuário (painel e planilha) e acompanha os alertas;
5. após ~150 s, **desfaz a falha** e espera a recuperação;
6. coleta as evidências dos três pilares e grava um relatório em `cenarios/resultados/`.

```powershell
.\cenarios\executar-cenario.ps1 -Cenario relatorios-fora
# Se a política de execução bloquear:
powershell -ExecutionPolicy Bypass -File .\cenarios\executar-cenario.ps1 -Cenario relatorios-fora
```

| Cenário | Falha injetada |
|---|---|
| `relatorios-fora` | contêiner de Relatórios parado |
| `relatorios-lento` | 4 s de atraso nas respostas de Relatórios (Toxiproxy) |
| `carros-fora` | contêiner de Carros parado |
| `mongo-fora` | MongoDB parado |
| `carga-alta` | rampa de carga até ~350 req/s |
| `memoria-insuficiente` | limite de memória de Relatórios reduzido em execução |

**Medidas:** **TTD** (tempo até detectar) = da injeção até o primeiro alerta; **MTTR** = da injeção até o
sistema voltar a responder. O reparo é simulado com duração fixa (~150 s), então o MTTR medido é reparo +
recuperação, e não inclui o tempo humano de diagnóstico.

## Resultados

Execução de 29/09/2026 (dados brutos em `cenarios/resultados/dados/`):

| Cenário | TTD | MTTR | O que revelou a causa |
|---|---|---|---|
| Relatórios fora do ar | 41 s | 2 min 42 s | métrica (`up`) + trace (timeout na chamada a Relatórios) |
| Relatórios lento | 40 s | 2 min 35 s | **trace**: Relatórios responde em 5 ms; o tempo é gasto no caminho |
| Carros fora do ar | 56 s | 2 min 42 s | métrica (`up`) + log (`Connection refused`) |
| MongoDB fora do ar | 1 min 41 s | 2 min 37 s | **trace**: spans do driver do MongoDB com tempo esgotado em 3 s |
| Carga alta | 4 min 21 s | — | métrica: CPU de 86–98% com latência estável |
| Memória insuficiente | 1 min 26 s | — | métrica: 97% do limite de memória |

Principais conclusões:

- **Nenhum pilar bastou sozinho.** As métricas avisaram que havia um problema; os traces e os logs mostraram
  onde e por quê.
- **O MongoDB fora do ar foi o mais difícil de detectar:** as APIs continuam respondendo à coleta, então
  `up` fica em 1. Só a taxa de erro e o trace revelaram a causa.
- **A observabilidade encontrou um defeito de projeto:** com só Relatórios fora, o circuit breaker do BFF
  abria o circuito para Carros inteiro e derrubava rotas saudáveis (5xx do BFF em 41%). O circuit breaker
  passou a contar só falhas de transporte, e a taxa caiu para 22%, restrita às planilhas.

## Estrutura do repositório

```
monitoramento-web/              front-end React
monitoramento-bff/              BFF (.NET)
monitoramento-carros/           API de carros (.NET)
monitoramento-motos/            API de motos (.NET)
monitoramento-relatorios/       API de relatórios (.NET)
monitoramento-*.tests/          testes unitários de cada API
observabilidade/
  otel-collector/               configuração do Collector
  prometheus/                   coleta (prometheus.yml) e regras de alerta (alertas.yml)
  loki/  tempo/                 configuração de logs e traces
  grafana/                      fontes de dados provisionadas e gerador do dashboard
  toxiproxy/                    proxies usados para injetar falhas
cenarios/
  executar-cenario.ps1          automação dos cenários de falha
  k6/                           tráfego de fundo (trafego.js) e carga (carga.js)
  resultados/dados/             dados brutos de cada execução
docker/                         Dockerfiles das APIs e do front
docker-compose.yml              orquestração de tudo
```

Cada API segue a mesma organização em pastas: `Dominio/`, `Aplicacao/`, `Infraestrutura/`, `Features/`
(um endpoint por arquivo) e `Comum/` (tratamento de erros e observabilidade). O código está em português,
por escolha do projeto.

## Testes

Testes unitários com xUnit, NSubstitute, Shouldly e Bogus, um projeto por API (BFF, Carros, Motos e
Relatórios). Cobrem regras de negócio, endpoints, resiliência e as métricas e spans próprios.

```bash
cd monitoramento-carros && dotnet test monitoramento-carros.slnx
```
