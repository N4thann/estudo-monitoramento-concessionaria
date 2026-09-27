import { servicoMotos } from '../../servicos/servicoVeiculos';
import { criarEstadoVeiculos } from './criarEstadoVeiculos';

export const useEstadoMotos = criarEstadoVeiculos(servicoMotos, 'Erro ao carregar motos.');
