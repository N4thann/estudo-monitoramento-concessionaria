import { api } from './api';
import type { ResultadoPaginado } from '../tipos/comum.tipos';
import type { Carro, FiltroVeiculos, Moto, RequisicaoCarro, RequisicaoMoto } from '../tipos/veiculo.tipos';
import type { Periodo, RequisicaoVenda, Venda } from '../tipos/venda.tipos';

const nomeArquivoDoCabecalho = (cabecalho: string | undefined, padrao: string): string => {
  const correspondencia = cabecalho?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
  return correspondencia ? decodeURIComponent(correspondencia[1]) : padrao;
};

const baixarArquivo = async (caminho: string, parametros: object, nomePadrao: string) => {
  const resposta = await api.get<Blob>(caminho, { params: parametros, responseType: 'blob' });
  const url = URL.createObjectURL(resposta.data);
  const link = document.createElement('a');
  link.href = url;
  link.download = nomeArquivoDoCabecalho(resposta.headers['content-disposition'], nomePadrao);
  link.click();
  URL.revokeObjectURL(url);
};

// Carros e motos têm o mesmo contrato no BFF; só mudam o recurso e o formato do veículo.
function criarServicoVeiculos<TVeiculo, TRequisicao>(recurso: string) {
  return {
    listar: async ({ situacao, busca, ...filtro }: FiltroVeiculos): Promise<ResultadoPaginado<TVeiculo>> => {
      const resposta = await api.get<ResultadoPaginado<TVeiculo>>(recurso, {
        params: { ...filtro, situacao: situacao || undefined, busca: busca || undefined },
      });
      return resposta.data;
    },
    criar: async (requisicao: TRequisicao): Promise<TVeiculo> => {
      const resposta = await api.post<TVeiculo>(recurso, requisicao);
      return resposta.data;
    },
    atualizar: async (id: string, requisicao: TRequisicao): Promise<TVeiculo> => {
      const resposta = await api.put<TVeiculo>(`${recurso}/${id}`, requisicao);
      return resposta.data;
    },
    excluir: async (id: string): Promise<void> => {
      await api.delete(`${recurso}/${id}`);
    },
    vender: async (requisicao: RequisicaoVenda): Promise<Venda> => {
      const resposta = await api.post<Venda>(`${recurso}/vendas`, requisicao);
      return resposta.data;
    },
    baixarRelatorioVendas: ({ de, ate }: Periodo) =>
      baixarArquivo(`${recurso}/relatorios/vendas`, { de: de || undefined, ate: ate || undefined }, `relatorio-vendas-${recurso}.xlsx`),
    baixarRelatorioEstoque: () =>
      baixarArquivo(`${recurso}/relatorios/estoque`, {}, `relatorio-estoque-${recurso}.xlsx`),
  };
}

export type ServicoVeiculos<TVeiculo, TRequisicao> = ReturnType<typeof criarServicoVeiculos<TVeiculo, TRequisicao>>;

export const servicoCarros = criarServicoVeiculos<Carro, RequisicaoCarro>('carros');
export const servicoMotos = criarServicoVeiculos<Moto, RequisicaoMoto>('motos');
