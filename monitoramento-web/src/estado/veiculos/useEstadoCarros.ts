import { servicoCarros } from '../../servicos/servicoVeiculos';
import { criarEstadoVeiculos } from './criarEstadoVeiculos';

export const useEstadoCarros = criarEstadoVeiculos(servicoCarros, 'Erro ao carregar carros.');
