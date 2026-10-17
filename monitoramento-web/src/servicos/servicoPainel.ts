import { api } from './api';
import type { Painel } from '../tipos/painel.tipos';

export const servicoPainel = {
  obter: async (): Promise<Painel> => {
    const resposta = await api.get<Painel>('painel');   // api = axios com baseURL do BFF
    return resposta.data;
  },
};
