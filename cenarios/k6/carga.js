// Carga alta: sobe a taxa de requisições em rampa até ALVO req/s, concentrada no que mais consome CPU
// (painel, que passa pelas 4 APIs, e planilhas, montadas com ClosedXML em Relatórios).
//   docker compose run --rm -e ALVO=150 k6 run /cenarios/k6/carga.js
import http from 'k6/http';
import { check } from 'k6';

const API = `${__ENV.BFF_URL || 'http://host.docker.internal:5101'}/api`;
const ALVO = Number(__ENV.ALVO || 150);

export const options = {
  scenarios: {
    carga: {
      executor: 'ramping-arrival-rate',
      startRate: 5,
      timeUnit: '1s',
      preAllocatedVUs: 50,
      maxVUs: 600,
      stages: [
        { target: ALVO, duration: __ENV.RAMPA || '30s' },
        { target: ALVO, duration: __ENV.PATAMAR || '2m' },
        { target: 0, duration: '10s' },
      ],
    },
  },
  summaryTrendStats: ['avg', 'p(95)', 'max'],
};

const cabecalhos = { headers: { 'X-Id-Operacao': 'k6-carga' } };

export default function () {
  const sorteio = Math.random();
  let resposta;
  if (sorteio < 0.5) resposta = http.get(`${API}/painel`, { ...cabecalhos, tags: { name: 'painel' } });
  else if (sorteio < 0.8) resposta = http.get(`${API}/${sorteio < 0.65 ? 'carros' : 'motos'}/relatorios/estoque`, { ...cabecalhos, tags: { name: 'planilha estoque' } });
  else resposta = http.get(`${API}/carros?pagina=1&tamanhoPagina=20`, { ...cabecalhos, tags: { name: 'listar carros' } });
  check(resposta, { 'sem erro 5xx': (r) => r.status > 0 && r.status < 500 });
}
