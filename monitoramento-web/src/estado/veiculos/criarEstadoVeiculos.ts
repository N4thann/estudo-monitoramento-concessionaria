import { create } from 'zustand';
import type { ServicoVeiculos } from '../../servicos/servicoVeiculos';
import type { FiltroVeiculos, SituacaoVeiculo, VeiculoBase } from '../../tipos/veiculo.tipos';
import type { RequisicaoVenda } from '../../tipos/venda.tipos';

export interface EstadoVeiculos<TVeiculo extends VeiculoBase, TRequisicao> {
  itens: TVeiculo[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
  situacao: SituacaoVeiculo | '';
  busca: string;
  carregando: boolean;
  erro: string | null;
  buscar: (filtro?: FiltroVeiculos) => Promise<void>;
  criar: (requisicao: TRequisicao) => Promise<void>;
  atualizar: (id: string, requisicao: TRequisicao) => Promise<void>;
  excluir: (id: string) => Promise<void>;
  vender: (requisicao: RequisicaoVenda) => Promise<void>;
}

export type HookEstadoVeiculos<TVeiculo extends VeiculoBase, TRequisicao> = () => EstadoVeiculos<TVeiculo, TRequisicao>;

export function criarEstadoVeiculos<TVeiculo extends VeiculoBase, TRequisicao>(
  servico: ServicoVeiculos<TVeiculo, TRequisicao>,
  mensagemErroCarregamento: string,
) {
  return create<EstadoVeiculos<TVeiculo, TRequisicao>>((set, get) => ({
    itens: [], total: 0, pagina: 1, tamanhoPagina: 10, situacao: '', busca: '', carregando: false, erro: null,

    buscar: async (filtro) => {
      const pagina = filtro?.pagina ?? get().pagina;
      const tamanhoPagina = filtro?.tamanhoPagina ?? get().tamanhoPagina;
      const situacao = filtro?.situacao ?? get().situacao;
      const busca = filtro?.busca ?? get().busca;

      set({ carregando: true, erro: null });
      try {
        const resultado = await servico.listar({ pagina, tamanhoPagina, situacao, busca });
        set({ itens: resultado.itens, total: resultado.total, pagina: resultado.pagina, tamanhoPagina: resultado.tamanhoPagina, situacao, busca, carregando: false });
      } catch {
        set({ erro: mensagemErroCarregamento, carregando: false });
      }
    },

    criar: async (requisicao) => {
      await servico.criar(requisicao);
      await get().buscar({ pagina: 1 });
    },

    atualizar: async (id, requisicao) => {
      await servico.atualizar(id, requisicao);
      await get().buscar();
    },

    excluir: async (id) => {
      await servico.excluir(id);
      await get().buscar();
    },

    vender: async (requisicao) => {
      await servico.vender(requisicao);
      await get().buscar();
    },
  }));
}
