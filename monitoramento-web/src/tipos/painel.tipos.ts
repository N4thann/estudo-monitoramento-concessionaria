export interface IndicadoresVendas {
  quantidadeVendas: number;
  receita: number;
  ticketMedio: number;
  maiorVenda: number;
  marcaMaisVendida: string | null;
}

// Indicadores nulos = serviço de Relatórios indisponível no momento.
export interface Resumo {
  totalVeiculos: number;
  disponiveis: number;
  vendidos: number;
  valorEmEstoque: number;
  indicadores: IndicadoresVendas | null;
}

export interface ResumoGeral {
  totalVeiculos: number;
  disponiveis: number;
  vendidos: number;
  valorEmEstoque: number;
  quantidadeVendas: number | null;
  receita: number | null;
}

export interface Painel {
  carros: Resumo;
  motos: Resumo;
  total: ResumoGeral;
}
