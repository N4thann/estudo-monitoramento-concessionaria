// Tráfego "normal" contínuo contra o BFF, simulando uso da concessionária enquanto as falhas são injetadas.
//   docker compose run --rm k6 run /cenarios/k6/trafego.js
//   docker compose run --rm -e TAXA=10 -e DURACAO=5m k6 run /cenarios/k6/trafego.js
import http from 'k6/http';
import { check } from 'k6';

const API = `${__ENV.BFF_URL || 'http://host.docker.internal:5101'}/api`;

// 400 (venda inválida) e 409 (revenda) fazem parte da mistura: não contam em http_req_failed.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 399 }, 400, 409));

export const options = {
  scenarios: {
    trafego: {
      // Taxa fixa de chegada: se o sistema ficar lento, o k6 abre mais usuários virtuais em vez de reduzir o ritmo,
      // como aconteceria com usuários reais que continuam chegando.
      executor: 'constant-arrival-rate',
      rate: Number(__ENV.TAXA || 5),
      timeUnit: '1s',
      duration: __ENV.DURACAO || '30m',
      preAllocatedVUs: 20,
      maxVUs: 200,
    },
  },
  summaryTrendStats: ['avg', 'p(95)', 'max'],
};

const hoje = new Date().toISOString().slice(0, 10);
const inicioMes = `${hoje.slice(0, 8)}01`;

const cabecalhos = () => ({
  // __ITER não existe no setup(); o typeof evita o ReferenceError.
  headers: { 'Content-Type': 'application/json', 'X-Id-Operacao': `k6-${__VU}-${typeof __ITER === 'undefined' ? 'setup' : __ITER}` },
});

// Precisa de um carro já vendido para gerar tentativas de revenda (409). Se ainda não houver, vende um.
export function setup() {
  const vendidos = http.get(`${API}/carros?pagina=1&tamanhoPagina=1&situacao=Vendido`).json('itens') || [];
  if (vendidos.length > 0) return { carroVendido: vendidos[0].id };

  const disponiveis = http.get(`${API}/carros?pagina=1&tamanhoPagina=1&situacao=Disponivel`).json('itens') || [];
  if (disponiveis.length === 0) return { carroVendido: null };

  const carro = disponiveis[0];
  http.post(`${API}/carros/vendas`, JSON.stringify({
    veiculoId: carro.id, nomeCliente: 'Cliente do k6', documentoCliente: '12345678909', valor: carro.preco,
  }), cabecalhos());
  return { carroVendido: carro.id };
}

// Peso de cada operação na mistura de tráfego (soma 100).
const operacoes = [
  [30, 'painel', () => http.get(`${API}/painel`, { ...cabecalhos(), tags: { name: 'painel' } })],
  [15, 'listar carros', () => http.get(`${API}/carros?pagina=1&tamanhoPagina=10`, { ...cabecalhos(), tags: { name: 'listar carros' } })],
  [15, 'listar motos', () => http.get(`${API}/motos?pagina=1&tamanhoPagina=10`, { ...cabecalhos(), tags: { name: 'listar motos' } })],
  [10, 'listar vendas', () => http.get(`${API}/vendas?de=${inicioMes}&ate=${hoje}`, { ...cabecalhos(), tags: { name: 'listar vendas' } })],
  [10, 'planilha estoque', () => http.get(`${API}/${Math.random() < 0.5 ? 'carros' : 'motos'}/relatorios/estoque`, { ...cabecalhos(), tags: { name: 'planilha estoque' } })],
  [10, 'planilha vendas', () => http.get(`${API}/${Math.random() < 0.5 ? 'carros' : 'motos'}/relatorios/vendas`, { ...cabecalhos(), tags: { name: 'planilha vendas' } })],
  [5, 'venda invalida', () => http.post(`${API}/motos/vendas`, JSON.stringify({ veiculoId: '', nomeCliente: '', documentoCliente: '1', valor: 0 }),
    { ...cabecalhos(), tags: { name: 'venda invalida' } })],
  [5, 'revenda', (dados) => dados.carroVendido && http.post(`${API}/carros/vendas`,
    JSON.stringify({ veiculoId: dados.carroVendido, nomeCliente: 'Cliente do k6', documentoCliente: '12345678909', valor: 1000 }),
    { ...cabecalhos(), tags: { name: 'revenda' } })],
];

export default function (dados) {
  let sorteio = Math.random() * 100;
  for (const [peso, nome, executar] of operacoes) {
    sorteio -= peso;
    if (sorteio > 0) continue;
    const resposta = executar(dados);
    // 400 e 409 são esperados (venda inválida e revenda); o que conta como falha é 5xx ou erro de rede.
    if (resposta) check(resposta, { [`${nome}: sem erro 5xx`]: (r) => r.status > 0 && r.status < 500 });
    return;
  }
}
