import { CatalogoVeiculos } from '../../componentes/templates/CatalogoVeiculos';
import { useEstadoMotos } from '../../estado/veiculos/useEstadoMotos';
import type { Moto, RequisicaoMoto } from '../../tipos/veiculo.tipos';

export const PaginaMotos = () => (
  <CatalogoVeiculos<Moto, RequisicaoMoto>
    useEstado={useEstadoMotos}
    textos={{
      titulo: 'Motos',
      botaoNovo: 'Nova moto',
      tituloNovo: 'Nova moto',
      tituloEdicao: 'Editar moto',
      tituloExclusao: 'Excluir moto',
      mensagemSalvo: 'Moto salva com sucesso.',
      mensagemExcluido: 'Moto excluída com sucesso.',
    }}
    campoEspecifico={{ chave: 'cilindrada', rotulo: 'Cilindrada (cc)', minimo: 50, maximo: 2500 }}
    colunaEspecifica={{ id: 'cilindrada', titulo: 'Cilindrada', renderizar: (m) => `${m.cilindrada} cc` }}
  />
);
