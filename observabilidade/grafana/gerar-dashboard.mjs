// Gera o dashboard principal do Grafana. Editar aqui (não no JSON) e rodar: node observabilidade/grafana/gerar-dashboard.mjs
// Depois: docker compose restart grafana (no Windows o Grafana não percebe a mudança do arquivo montado).
import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const saida = join(dirname(fileURLToPath(import.meta.url)), 'provisioning', 'dashboards', 'arquivos', 'concessionaria.json');

// Paleta categórica validada (dataviz, tema escuro do Grafana, fundo #181b1f): cor segue o serviço em todos os painéis.
const COR = { bff: '#3987e5', carros: '#d95926', motos: '#199e70', relatorios: '#c98500' };
// Status reservados: nunca usados como cor de série.
const STATUS = { bom: '#0ca30c', atencao: '#fab219', serio: '#ec835a', critico: '#d03b3b' };
const NEUTRO = '#898781';

const PROM = { type: 'prometheus', uid: 'prometheus' };
const LOKI = { type: 'loki', uid: 'loki' };
const TEMPO = { type: 'tempo', uid: 'tempo' };

const SEL = 'job="concessionaria",servico=~"$servico"';
// /metrics e /saude são tráfego de infraestrutura (scrape e health check), não uso real.
const SRV = `${SEL},http_route!~"/metrics|/saude"`;
// A porta 4317 é o próprio exportador OTLP falando com o Collector.
const CLI = `${SEL},server_port!="4317"`;
// Fração de respostas cuja classe de status casa com o regex, por serviço; 0 (e não "sem dados") quando não houve nenhuma.
const taxaPorServico = (status) =>
  `(sum by (servico) (rate(http_server_request_duration_seconds_count{${SRV},http_response_status_code=~"${status}"}[$__rate_interval])) or 0 * sum by (servico) (rate(http_server_request_duration_seconds_count{${SRV}}[$__rate_interval]))) / sum by (servico) (rate(http_server_request_duration_seconds_count{${SRV}}[$__rate_interval]))`;
// Serviço chamado, a partir do endereço: em contêiner o host diz quem é (carros:8080, motos:8080; Relatórios
// passa pelo toxiproxy:8666); com dotnet run é tudo 127.0.0.1 e só a porta diferencia.
const destino = (expr) =>
  `label_replace(label_replace(label_replace(label_replace(label_replace(${expr}, "destino", "$1", "server_address", "(carros|motos|relatorios)"), "destino", "relatorios", "server_address", "toxiproxy"), "destino", "carros", "server_port", "5128"), "destino", "motos", "server_port", "5010"), "destino", "relatorios", "server_port", "5051")`;
// Eventos de um contador na janela. "increase()" sozinho não vê o primeiro incremento de um contador recém-criado
// (a série já nasce valendo 1, e increase = 0); a parte "unless offset" pega a série que nasceu dentro da janela,
// cujo valor inteiro são eventos da janela. Mesma expressão dos alertas de evento (alertas.yml).
const eventos = (seletor, janela) => `((${seletor} unless ${seletor} offset ${janela}) or increase(${seletor}[${janela}]))`;

let id = 0;
let y = 0;
const paineis = [];

const coresPorServico = (prefixo = '') =>
  Object.entries(COR).map(([s, cor]) => ({
    matcher: { id: 'byRegexp', options: `^${prefixo}${s}\\b.*` },
    properties: [{ id: 'color', value: { mode: 'fixed', fixedColor: cor } }],
  }));

const alvo = (expr, legenda, extra = {}) => ({ datasource: PROM, expr, legendFormat: legenda, refId: String.fromCharCode(65 + (extra.i ?? 0)), ...extra.campos });

function linha(titulo) {
  paineis.push({ id: ++id, type: 'row', title: titulo, collapsed: false, gridPos: { h: 1, w: 24, x: 0, y }, panels: [] });
  y += 1;
}

function serie({ titulo, descricao, x, w, h = 8, alvos, unidade, min = 0, max, limiar, overrides = coresPorServico(), estilo = 'line', empilhar = false, legendaValores = ['lastNotNull', 'max'], semDados }) {
  const limiares = limiar
    ? { mode: 'absolute', steps: [{ color: 'transparent', value: null }, { color: STATUS.critico, value: limiar }] }
    : { mode: 'absolute', steps: [{ color: NEUTRO, value: null }] };
  paineis.push({
    id: ++id, type: 'timeseries', title: titulo, description: descricao, datasource: PROM,
    gridPos: { h, w, x, y },
    targets: alvos,
    fieldConfig: {
      defaults: {
        unit: unidade, min, max, noValue: semDados,
        color: { mode: 'fixed', fixedColor: NEUTRO },
        thresholds: limiares,
        custom: {
          drawStyle: estilo, lineWidth: 2, fillOpacity: estilo === 'bars' ? 80 : 0, pointSize: 8,
          showPoints: 'never', spanNulls: false, axisBorderShow: false, axisPlacement: 'auto',
          gradientMode: 'none', barAlignment: 0,
          stacking: { mode: empilhar ? 'normal' : 'none', group: 'A' },
          thresholdsStyle: { mode: limiar ? 'dashed' : 'off' },
        },
      },
      overrides,
    },
    options: {
      legend: { showLegend: true, displayMode: 'table', placement: 'right', calcs: legendaValores },
      tooltip: { mode: 'multi', sort: 'desc' },
    },
  });
}

function estatistica({ titulo, descricao, x, w, h = 4, alvos, unidade, passos, mapeamentos = [], decimais, textoMode = 'value', orientacao = 'auto', corModo = 'value' }) {
  paineis.push({
    id: ++id, type: 'stat', title: titulo, description: descricao, datasource: PROM,
    gridPos: { h, w, x, y },
    targets: alvos,
    fieldConfig: {
      defaults: { unit: unidade, decimals: decimais, mappings: mapeamentos, thresholds: { mode: 'absolute', steps: passos }, color: { mode: 'thresholds' } },
      overrides: [],
    },
    options: {
      reduceOptions: { calcs: ['lastNotNull'], fields: '', values: false },
      colorMode: corModo, graphMode: 'none', justifyMode: 'center', textMode: textoMode, orientation: orientacao,
      showPercentChange: false, wideLayout: true,
    },
  });
}

// ─── Visão geral ──────────────────────────────────────────────────────────────
linha('Visão geral');
estatistica({
  titulo: 'Disponibilidade', x: 0, w: 8,
  descricao: 'Resultado do último scrape do Prometheus (métrica up). "Fora" = a API não respondeu ao /metrics.',
  alvos: [alvo(`up{${SEL}}`, '{{servico}}', { campos: { instant: true } })],
  passos: [{ color: STATUS.critico, value: null }, { color: STATUS.bom, value: 1 }],
  mapeamentos: [{ type: 'value', options: { 0: { text: '✕ Fora', color: STATUS.critico }, 1: { text: '✓ No ar', color: STATUS.bom } } }],
  textoMode: 'value_and_name', corModo: 'background',
});
estatistica({
  titulo: 'Requisições/s', x: 8, w: 4, unidade: 'reqps', decimais: 2,
  descricao: 'Soma das requisições recebidas por todas as APIs selecionadas (sem /metrics e /saude).',
  alvos: [alvo(`sum(rate(http_server_request_duration_seconds_count{${SRV}}[$__rate_interval]))`, 'total')],
  passos: [{ color: 'text', value: null }], corModo: 'none',
});
estatistica({
  titulo: 'Taxa de erro 5xx', x: 12, w: 4, unidade: 'percentunit', decimais: 1,
  descricao: 'Respostas 5xx ÷ total de respostas. Atenção a partir de 1%, crítico a partir de 5%.',
  alvos: [alvo(`sum(rate(http_server_request_duration_seconds_count{${SRV},http_response_status_code=~"5.."}[$__rate_interval])) / sum(rate(http_server_request_duration_seconds_count{${SRV}}[$__rate_interval])) or vector(0)`, 'erro')],
  passos: [{ color: STATUS.bom, value: null }, { color: STATUS.atencao, value: 0.01 }, { color: STATUS.critico, value: 0.05 }],
});
estatistica({
  titulo: 'Latência p95 (BFF)', x: 16, w: 4, unidade: 's',
  descricao: 'Percentil 95 do tempo de resposta do BFF, que é o que o usuário sente. Atenção a partir de 300 ms, crítico a partir de 500 ms.',
  alvos: [alvo(`histogram_quantile(0.95, sum by (le) (rate(http_server_request_duration_seconds_bucket{job="concessionaria",servico="bff",http_route!~"/metrics|/saude"}[$__rate_interval])))`, 'p95')],
  passos: [{ color: STATUS.bom, value: null }, { color: STATUS.atencao, value: 0.3 }, { color: STATUS.critico, value: 0.5 }],
});
estatistica({
  titulo: 'Painel degradado', x: 20, w: 4, unidade: 'none', decimais: 0,
  descricao: 'Respostas do /api/painel sem indicadores (Relatórios indisponível) no período selecionado.',
  alvos: [alvo(`round(sum(${eventos('concessionaria_painel_degradado_total{job="concessionaria"}', '$__range')})) or vector(0)`, 'degradado', { campos: { instant: true } })],
  passos: [{ color: STATUS.bom, value: null }, { color: STATUS.serio, value: 1 }],
});
y += 4;
// Regras em observabilidade/prometheus/alertas.yml; a série ALERTS é gerada pelo próprio Prometheus.
paineis.push({
  id: ++id, type: 'table', title: 'Alertas ativos', datasource: PROM,
  description: 'Regras de alerta do Prometheus que estão pendentes (condição verdadeira, aguardando o "for") ou disparando.',
  gridPos: { h: 8, w: 24, x: 0, y },
  targets: [alvo('sort_desc(max by (alertname, severidade, servico, alertstate) (ALERTS))', '', { campos: { instant: true, format: 'table' } })],
  fieldConfig: {
    // noValue vale também para célula vazia (ex.: PainelDegradado não tem serviço), por isso um traço neutro.
    defaults: { noValue: '—', custom: { align: 'auto' } },
    overrides: [
      { matcher: { id: 'byName', options: 'alertstate' }, properties: [
        { id: 'displayName', value: 'Estado' },
        { id: 'custom.cellOptions', value: { type: 'color-text' } },
        { id: 'mappings', value: [{ type: 'value', options: { firing: { text: '● Disparando', color: STATUS.critico }, pending: { text: '◌ Pendente', color: STATUS.atencao } } }] },
      ] },
      { matcher: { id: 'byName', options: 'severidade' }, properties: [
        { id: 'displayName', value: 'Severidade' },
        { id: 'custom.cellOptions', value: { type: 'color-text' } },
        { id: 'mappings', value: [{ type: 'value', options: { critica: { text: '▲ Crítica', color: STATUS.critico }, aviso: { text: '■ Aviso', color: STATUS.serio } } }] },
      ] },
    ],
  },
  transformations: [{ id: 'organize', options: {
    excludeByName: { Time: true, Value: true },
    renameByName: { alertname: 'Alerta', servico: 'Serviço' },
    indexByName: { alertstate: 0, severidade: 1, alertname: 2, servico: 3 },
  } }],
  options: { showHeader: true, cellHeight: 'sm', footer: { show: false } },
});
y += 8;

// ─── Tráfego, erros e latência ───────────────────────────────────────────────
linha('Tráfego, erros e latência');
serie({
  titulo: 'Requisições/s por serviço', x: 0, w: 12, unidade: 'reqps',
  descricao: 'Requisições recebidas por segundo, por API.',
  alvos: [alvo(`sum by (servico) (rate(http_server_request_duration_seconds_count{${SRV}}[$__rate_interval]))`, '{{servico}}')],
});
serie({
  titulo: 'Latência p95 por serviço', x: 12, w: 12, unidade: 's', limiar: 0.5,
  descricao: 'Percentil 95 do tempo de resposta. A linha tracejada marca o limite de 500 ms usado no alerta.',
  alvos: [alvo(`histogram_quantile(0.95, sum by (servico, le) (rate(http_server_request_duration_seconds_bucket{${SRV}}[$__rate_interval])))`, '{{servico}}')],
});
y += 8;
serie({
  titulo: 'Taxa de erro 5xx por serviço', x: 0, w: 12, unidade: 'percentunit', max: 1, limiar: 0.05,
  descricao: 'Respostas 5xx ÷ total, por API. A linha tracejada marca 5%. No BFF, falha de integração vira 502/503/504.',
  alvos: [alvo(taxaPorServico('5..'), '{{servico}}')],
});
serie({
  titulo: 'Taxa de erro 4xx por serviço', x: 12, w: 12, unidade: 'percentunit', max: 1,
  descricao: 'Respostas 4xx ÷ total, por API: validação (400), não encontrado (404) e conflito de venda (409). São erros do cliente, não falhas da aplicação.',
  alvos: [alvo(taxaPorServico('4..'), '{{servico}}')],
});
y += 8;
paineis.push({
  id: ++id, type: 'bargauge', title: 'Rotas mais lentas (p95)', datasource: PROM,
  description: 'As 10 rotas com maior percentil 95 no intervalo selecionado.',
  gridPos: { h: 9, w: 12, x: 0, y },
  targets: [alvo(`topk(10, histogram_quantile(0.95, sum by (servico, http_route, le) (rate(http_server_request_duration_seconds_bucket{${SRV}}[$__range]))) > 0)`, '{{servico}} {{http_route}}', { campos: { instant: true } })],
  fieldConfig: {
    defaults: { unit: 's', min: 0, color: { mode: 'fixed', fixedColor: COR.bff }, thresholds: { mode: 'absolute', steps: [{ color: COR.bff, value: null }] } },
    overrides: coresPorServico(),
  },
  options: {
    orientation: 'horizontal', displayMode: 'basic', showUnfilled: true, valueMode: 'text', namePlacement: 'top',
    reduceOptions: { calcs: ['lastNotNull'], fields: '', values: false }, minVizHeight: 16, maxVizHeight: 24, sizing: 'manual',
  },
});
paineis.push({
  id: ++id, type: 'table', title: 'Respostas por rota e status', datasource: PROM,
  description: 'Quantidade de respostas por serviço, rota e código HTTP no intervalo selecionado.',
  gridPos: { h: 9, w: 12, x: 12, y },
  targets: [alvo(`sort_desc(sum by (servico, http_route, http_request_method, http_response_status_code) (round(${eventos(`http_server_request_duration_seconds_count{${SRV}}`, '$__range')})) > 0)`, '', { campos: { instant: true, format: 'table' } })],
  fieldConfig: { defaults: { custom: { align: 'auto' } }, overrides: [
    { matcher: { id: 'byName', options: 'Value' }, properties: [{ id: 'displayName', value: 'Respostas' }, { id: 'decimals', value: 0 }] },
    { matcher: { id: 'byName', options: 'http_response_status_code' }, properties: [
      { id: 'displayName', value: 'Status' },
      { id: 'custom.cellOptions', value: { type: 'color-text' } },
      { id: 'mappings', value: [{ type: 'regex', options: { pattern: '^5', result: { color: STATUS.critico } } }, { type: 'regex', options: { pattern: '^4', result: { color: STATUS.atencao } } }, { type: 'regex', options: { pattern: '^[23]', result: { color: STATUS.bom } } }] },
    ] },
  ] },
  transformations: [{ id: 'organize', options: { excludeByName: { Time: true }, renameByName: { servico: 'Serviço', http_route: 'Rota', http_request_method: 'Método' }, indexByName: { servico: 0, http_request_method: 1, http_route: 2, http_response_status_code: 3, Value: 4 } } }],
  options: { showHeader: true, cellHeight: 'sm', footer: { show: false } },
});
y += 9;

// ─── Integrações e resiliência ───────────────────────────────────────────────
linha('Integrações e resiliência');
serie({
  titulo: 'BFF → Carros/Motos: latência p95', x: 0, w: 12, unidade: 's',
  descricao: 'Tempo das chamadas de saída do BFF, pela cor do serviço chamado. Cada retry conta como uma chamada.',
  alvos: [alvo(destino(`histogram_quantile(0.95, sum by (servico, server_address, server_port, le) (rate(http_client_request_duration_seconds_bucket{${CLI},servico="bff"}[$__rate_interval])))`), '{{destino}}')],
});
serie({
  titulo: 'Carros/Motos → Relatórios: latência p95', x: 12, w: 12, unidade: 's', limiar: 3,
  descricao: 'Tempo das chamadas a Relatórios, pela cor de quem chama. A linha tracejada marca o timeout de 3 s por tentativa.',
  alvos: [alvo(destino(`histogram_quantile(0.95, sum by (servico, server_address, server_port, le) (rate(http_client_request_duration_seconds_bucket{${CLI},servico=~"carros|motos"}[$__rate_interval])))`), '{{servico}}')],
});
y += 8;
serie({
  titulo: 'Retries, timeouts e circuito aberto', x: 0, w: 12, unidade: 'short', estilo: 'bars', empilhar: true,
  descricao: 'Eventos do AddStandardResilienceHandler (Polly) por minuto. A legenda mostra origem → pipeline: evento.',
  alvos: [alvo(`sum by (servico, pipeline_name, event_name) (${eventos(`resilience_polly_strategy_events_total{${SEL},event_name=~"OnRetry|OnTimeout|OnCircuitOpened"}`, '1m')})`, '{{servico}} → {{pipeline_name}}: {{event_name}}')],
  legendaValores: ['sum'], semDados: 'Nenhum evento no intervalo',
});
serie({
  titulo: 'Falhas de integração com Relatórios', x: 12, w: 12, unidade: 'short', estilo: 'bars', empilhar: true,
  descricao: 'Chamadas a Relatórios que falharam mesmo após os retries, por minuto, por serviço e operação.',
  alvos: [alvo(`sum by (servico, operacao) (${eventos(`concessionaria_relatorios_falhas_integracao_total{${SEL}}`, '1m')})`, '{{servico}}: {{operacao}}')],
  legendaValores: ['sum'], semDados: 'Nenhum evento no intervalo',
});
y += 8;

// ─── Recursos ────────────────────────────────────────────────────────────────
linha('Recursos dos processos');
serie({
  titulo: 'CPU por serviço', x: 0, w: 6, unidade: 'percentunit', limiar: 0.8,
  descricao: 'Tempo de CPU do processo ÷ núcleos disponíveis (em contêiner, o limite de CPU dele). A linha tracejada marca 80%.',
  alvos: [alvo(`sum by (servico) (rate(dotnet_process_cpu_time_seconds_total{${SEL}}[$__rate_interval])) / on (servico) max by (servico) (dotnet_process_cpu_count{${SEL}})`, '{{servico}}')],
});
serie({
  titulo: 'Memória (% do limite)', x: 6, w: 6, unidade: 'percentunit', max: 1, limiar: 0.9,
  descricao: 'Working set ÷ limite de memória (do contêiner ou, com dotnet run, a RAM da máquina). A linha tracejada marca 90%.',
  alvos: [alvo(`max by (servico) (dotnet_process_memory_working_set_bytes{${SEL}}) / on (servico) max by (servico) (concessionaria_processo_memoria_limite_bytes{${SEL}} > 0)`, '{{servico}}')],
});
serie({
  titulo: 'Memória (working set)', x: 12, w: 6, unidade: 'bytes',
  descricao: 'Memória física usada pelo processo de cada API.',
  alvos: [alvo(`max by (servico) (dotnet_process_memory_working_set_bytes{${SEL}})`, '{{servico}}')],
});
serie({
  titulo: 'Tempo em pausa de GC', x: 18, w: 6, unidade: 'percentunit',
  descricao: 'Fração do tempo em que o processo ficou parado pelo coletor de lixo do .NET.',
  alvos: [alvo(`sum by (servico) (rate(dotnet_gc_pause_time_seconds_total{${SEL}}[$__rate_interval]))`, '{{servico}}')],
});
y += 8;

// ─── Negócio ─────────────────────────────────────────────────────────────────
linha('Negócio');
// tipo_veiculo vale "carro"/"moto" em Carros e Motos, mas "carros"/"motos" em Relatórios.
const coresVeiculo = [
  { matcher: { id: 'byRegexp', options: '^carros?\\b.*' }, properties: [{ id: 'color', value: { mode: 'fixed', fixedColor: COR.carros } }] },
  { matcher: { id: 'byRegexp', options: '^motos?\\b.*' }, properties: [{ id: 'color', value: { mode: 'fixed', fixedColor: COR.motos } }] },
];
function estatisticaVeiculo(titulo, descricao, x, expr, unidade, decimais) {
  paineis.push({
    id: ++id, type: 'stat', title: titulo, description: descricao, datasource: PROM,
    gridPos: { h: 5, w: 6, x, y },
    targets: [alvo(expr, '{{tipo_veiculo}}', { campos: { instant: true } })],
    fieldConfig: { defaults: { unit: unidade, decimals: decimais, noValue: '0', color: { mode: 'fixed', fixedColor: 'text' } }, overrides: [] },
    options: { reduceOptions: { calcs: ['lastNotNull'], fields: '', values: false }, colorMode: 'none', graphMode: 'none', justifyMode: 'center', textMode: 'value_and_name', orientation: 'vertical', wideLayout: true },
  });
}
// Eventos no período selecionado no Grafana ($__range), contando também o primeiro evento de cada série (ver eventos()).
const noPeriodo = ' No período selecionado.';
estatisticaVeiculo('Vendas registradas', 'Vendas concluídas.' + noPeriodo, 0,
  `round(sum by (tipo_veiculo) (${eventos(`concessionaria_vendas_registradas_total{${SEL}}`, '$__range')}))`, 'none', 0);
estatisticaVeiculo('Valor vendido', 'Soma do valor das vendas.' + noPeriodo, 6,
  `sum by (tipo_veiculo) (${eventos(`concessionaria_vendas_valor_BRL_total{${SEL}}`, '$__range')})`, 'currencyBRL', 0);
estatisticaVeiculo('Vendas recusadas', 'Tentativas de venda recusadas (dados inválidos, veículo já vendido etc.).' + noPeriodo, 12,
  `round(sum by (tipo_veiculo) (${eventos(`concessionaria_vendas_recusadas_total{${SEL}}`, '$__range')}))`, 'none', 0);
estatisticaVeiculo('Planilhas geradas', 'Planilhas .xlsx montadas pelo serviço de Relatórios.' + noPeriodo, 18,
  `round(sum by (tipo_veiculo) (${eventos('concessionaria_planilhas_geradas_total{job="concessionaria"}', '$__range')}))`, 'none', 0);
y += 5;
serie({
  titulo: 'Vendas recusadas por motivo', x: 0, w: 12, unidade: 'short', estilo: 'bars', empilhar: true, overrides: coresVeiculo,
  descricao: 'Recusas por minuto, por tipo de veículo e motivo.',
  alvos: [alvo(`sum by (tipo_veiculo, motivo) (${eventos(`concessionaria_vendas_recusadas_total{${SEL}}`, '1m')})`, '{{tipo_veiculo}}: {{motivo}}')],
  legendaValores: ['sum'], semDados: 'Nenhum evento no intervalo',
});
serie({
  titulo: 'Geração de planilhas: duração p95', x: 12, w: 12, unidade: 's', overrides: coresVeiculo,
  descricao: 'Tempo para montar a planilha no serviço de Relatórios, por tipo de veículo e de relatório.',
  alvos: [alvo(`histogram_quantile(0.95, sum by (tipo_veiculo, tipo_relatorio, le) (rate(concessionaria_planilhas_duracao_seconds_bucket{job="concessionaria"}[$__rate_interval])))`, '{{tipo_veiculo}}: {{tipo_relatorio}}')],
});
y += 8;

// ─── Logs e traces ───────────────────────────────────────────────────────────
linha('Logs e traces');
paineis.push({
  id: ++id, type: 'logs', title: 'Logs de erro e aviso', datasource: LOKI,
  description: 'Logs Warning/Error/Fatal das APIs selecionadas. Expanda uma linha e use "Ver trace no Tempo".',
  gridPos: { h: 10, w: 24, x: 0, y },
  targets: [{ datasource: LOKI, refId: 'A', queryType: 'range', expr: '{service_namespace="concessionaria", service_name=~"$servico"} | severity_text=~"Warning|Error|Fatal"' }],
  options: { showTime: true, wrapLogMessage: true, sortOrder: 'Descending', enableLogDetails: true, dedupStrategy: 'none', prettifyLogMessage: false },
});
y += 10;
function tabelaTraces(titulo, descricao, x, consulta) {
  paineis.push({
    id: ++id, type: 'table', title: titulo, description: descricao, datasource: TEMPO,
    gridPos: { h: 9, w: 12, x, y },
    targets: [{ datasource: TEMPO, refId: 'A', queryType: 'traceql', query: consulta, limit: 20, tableType: 'traces' }],
    fieldConfig: { defaults: {}, overrides: [] },
    options: { showHeader: true, cellHeight: 'sm', footer: { show: false } },
  });
}
tabelaTraces('Traces com erro', 'Traces em que algum span terminou com erro. Clique no ID para abrir o trace.', 0,
  '{ resource.service.namespace = "concessionaria" && status = error }');
tabelaTraces('Traces lentos (> 500 ms)', 'Requisições que passaram de 500 ms em alguma API. Clique no ID para abrir o trace.', 12,
  '{ resource.service.namespace = "concessionaria" && kind = server && duration > 500ms }');
y += 9;

const dashboard = {
  uid: 'concessionaria-visao-geral',
  title: 'Concessionária — Visão geral',
  description: 'Métricas, logs e traces das 4 APIs da concessionária (BFF, Carros, Motos, Relatórios).',
  tags: ['concessionaria', 'observabilidade'],
  timezone: 'browser', editable: true, graphTooltip: 1, refresh: '10s', schemaVersion: 41, version: 1,
  time: { from: 'now-30m', to: 'now' },
  timepicker: { refresh_intervals: ['5s', '10s', '30s', '1m', '5m'] },
  templating: {
    list: [{
      name: 'servico', label: 'Serviço', type: 'query', datasource: PROM,
      query: { query: 'label_values(up{job="concessionaria"}, servico)', refId: 'servico' },
      definition: 'label_values(up{job="concessionaria"}, servico)',
      multi: true, includeAll: true, allValue: '.*', current: { selected: true, text: ['All'], value: ['$__all'] },
      refresh: 1, sort: 1,
    }],
  },
  annotations: { list: [] },
  links: [
    { title: 'Explorar traces', type: 'link', url: '/explore?schemaVersion=1&panes=%7B%22t%22:%7B%22datasource%22:%22tempo%22,%22queries%22:%5B%7B%22refId%22:%22A%22,%22datasource%22:%7B%22type%22:%22tempo%22,%22uid%22:%22tempo%22%7D,%22queryType%22:%22serviceMap%22%7D%5D,%22range%22:%7B%22from%22:%22now-30m%22,%22to%22:%22now%22%7D%7D%7D&orgId=1', targetBlank: false, icon: 'external link' },
  ],
  panels: paineis,
};

mkdirSync(dirname(saida), { recursive: true });
writeFileSync(saida, JSON.stringify(dashboard, null, 2) + '\n');
console.log(`${paineis.length} itens escritos em ${saida}`);
