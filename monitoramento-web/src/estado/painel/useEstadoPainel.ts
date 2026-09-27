import { create } from 'zustand';
import { servicoPainel } from '../../servicos/servicoPainel';
import type { Painel } from '../../tipos/painel.tipos';
import { extrairMensagemErro } from '../../utilitarios/erroApi';

interface EstadoPainel {
  painel: Painel | null;
  carregando: boolean;
  erro: string | null;
  buscar: () => Promise<void>;
}

export const useEstadoPainel = create<EstadoPainel>((set) => ({
  painel: null, carregando: false, erro: null,

  buscar: async () => {
    set({ carregando: true, erro: null });
    try {
      set({ painel: await servicoPainel.obter(), carregando: false });
    } catch (erro) {
      set({ erro: await extrairMensagemErro(erro, 'Erro ao carregar o painel.'), carregando: false });
    }
  },
}));
