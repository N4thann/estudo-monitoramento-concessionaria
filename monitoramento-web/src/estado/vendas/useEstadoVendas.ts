import { create } from 'zustand';
import { servicoVendas } from '../../servicos/servicoVendas';
import type { Periodo, Venda } from '../../tipos/venda.tipos';

interface EstadoVendas {
  vendas: Venda[];
  carregando: boolean;
  erro: string | null;
  buscar: (periodo: Periodo) => Promise<void>;
}

export const useEstadoVendas = create<EstadoVendas>((set) => ({
  vendas: [], carregando: false, erro: null,

  buscar: async (periodo) => {
    set({ carregando: true, erro: null });
    try {
      set({ vendas: await servicoVendas.listar(periodo), carregando: false });
    } catch {
      set({ erro: 'Erro ao carregar vendas.', carregando: false });
    }
  },
}));
