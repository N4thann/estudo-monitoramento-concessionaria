export type TipoVeiculo = 'Carro' | 'Moto';

export interface Venda {
  id: string;
  tipoVeiculo: TipoVeiculo;
  veiculoId: string;
  descricaoVeiculo: string;
  marca: string;
  nomeCliente: string;
  documentoCliente: string;
  valor: number;
  vendidaEm: string;
}

export interface RequisicaoVenda {
  veiculoId: string;
  nomeCliente: string;
  documentoCliente: string;
  valor: number;
}

export interface Periodo {
  de?: string;
  ate?: string;
}
