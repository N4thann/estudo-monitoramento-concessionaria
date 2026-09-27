import { CatalogoVeiculos } from '../../componentes/templates/CatalogoVeiculos';
import { useEstadoCarros } from '../../estado/veiculos/useEstadoCarros';
import type { Carro, RequisicaoCarro } from '../../tipos/veiculo.tipos';

export const PaginaCarros = () => (
  <CatalogoVeiculos<Carro, RequisicaoCarro>
    useEstado={useEstadoCarros}
    textos={{
      titulo: 'Carros',
      botaoNovo: 'Novo carro',
      tituloNovo: 'Novo carro',
      tituloEdicao: 'Editar carro',
      tituloExclusao: 'Excluir carro',
      mensagemSalvo: 'Carro salvo com sucesso.',
      mensagemExcluido: 'Carro excluído com sucesso.',
    }}
    campoEspecifico={{ chave: 'portas', rotulo: 'Portas', minimo: 2, maximo: 5 }}
    colunaEspecifica={{ id: 'portas', titulo: 'Portas' }}
  />
);
