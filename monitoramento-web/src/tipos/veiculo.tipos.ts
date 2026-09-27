export type SituacaoVeiculo = 'Disponivel' | 'Vendido';

export const ROTULOS_SITUACAO: Record<SituacaoVeiculo, string> = {
  Disponivel: 'Disponível',
  Vendido: 'Vendido',
};

export interface FiltroVeiculos {
  pagina?: number;
  tamanhoPagina?: number;
  situacao?: SituacaoVeiculo | '';
  busca?: string;
}

export interface VeiculoBase {
  id: string;
  marca: string;
  modelo: string;
  ano: number;
  cor: string;
  preco: number;
  situacao: SituacaoVeiculo;
  criadoEm: string;
}

export interface Carro extends VeiculoBase {
  portas: number;
}

export interface Moto extends VeiculoBase {
  cilindrada: number;
}

export type RequisicaoCarro = Omit<Carro, 'id' | 'situacao' | 'criadoEm'>;
export type RequisicaoMoto = Omit<Moto, 'id' | 'situacao' | 'criadoEm'>;
