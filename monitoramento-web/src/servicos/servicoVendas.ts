import { api } from './api';
import type { Periodo, Venda } from '../tipos/venda.tipos';

export const servicoVendas = {
  listar: async ({ de, ate }: Periodo): Promise<Venda[]> => {
    const resposta = await api.get<Venda[]>('vendas', { params: { de: de || undefined, ate: ate || undefined } });
    return resposta.data;
  },
};
