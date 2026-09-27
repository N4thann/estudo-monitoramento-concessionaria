import { useEffect, useState } from 'react';
import { Alert, Box, Button, Chip, MenuItem, Snackbar, TextField, Typography } from '@mui/material';
import { Add, Delete, Edit, PointOfSale } from '@mui/icons-material';
import { TabelaGenerica } from '../organismos/TabelaGenerica';
import { DialogoConfirmacao } from '../organismos/DialogoConfirmacao';
import { DialogoVenda } from '../organismos/DialogoVenda';
import { DialogoVeiculo, type CampoEspecifico } from '../organismos/DialogoVeiculo';
import type { HookEstadoVeiculos } from '../../estado/veiculos/criarEstadoVeiculos';
import type { AcaoTabela, ColunaTabela } from '../../tipos/comum.tipos';
import { ROTULOS_SITUACAO, type SituacaoVeiculo, type VeiculoBase } from '../../tipos/veiculo.tipos';
import { extrairMensagemErro } from '../../utilitarios/erroApi';
import { formatarMoeda } from '../../utilitarios/formatacao';

export interface TextosCatalogo {
  titulo: string;
  botaoNovo: string;
  tituloNovo: string;
  tituloEdicao: string;
  tituloExclusao: string;
  mensagemSalvo: string;
  mensagemExcluido: string;
}

interface CatalogoVeiculosProps<TVeiculo extends VeiculoBase, TRequisicao> {
  textos: TextosCatalogo;
  useEstado: HookEstadoVeiculos<TVeiculo, TRequisicao>;
  campoEspecifico: CampoEspecifico;
  colunaEspecifica: ColunaTabela<TVeiculo>;
}

// Tela de catálogo compartilhada entre carros e motos: listagem com filtro, cadastro, edição, exclusão e venda.
export function CatalogoVeiculos<TVeiculo extends VeiculoBase, TRequisicao>({ textos, useEstado, campoEspecifico, colunaEspecifica }: CatalogoVeiculosProps<TVeiculo, TRequisicao>) {
  const { itens, total, pagina, tamanhoPagina, situacao, carregando, erro, buscar, criar, atualizar, excluir, vender } = useEstado();
  const [busca, setBusca] = useState('');
  const [formularioAberto, setFormularioAberto] = useState(false);
  const [emEdicao, setEmEdicao] = useState<TVeiculo | undefined>(undefined);
  const [emExclusao, setEmExclusao] = useState<TVeiculo | undefined>(undefined);
  const [emVenda, setEmVenda] = useState<TVeiculo | undefined>(undefined);
  const [excluindo, setExcluindo] = useState(false);
  const [mensagemSucesso, setMensagemSucesso] = useState<string | null>(null);
  const [mensagemErro, setMensagemErro] = useState<string | null>(null);

  useEffect(() => {
    buscar({ pagina: 1 });
  }, [buscar]);

  const salvar = async (requisicao: TRequisicao) => {
    if (emEdicao) await atualizar(emEdicao.id, requisicao);
    else await criar(requisicao);
    setMensagemSucesso(textos.mensagemSalvo);
  };

  const confirmarExclusao = async () => {
    if (!emExclusao) return;
    setExcluindo(true);
    try {
      await excluir(emExclusao.id);
      setMensagemSucesso(textos.mensagemExcluido);
    } catch (e) {
      setMensagemErro(await extrairMensagemErro(e, 'Erro ao excluir.'));
    } finally {
      setExcluindo(false);
      setEmExclusao(undefined);
    }
  };

  const vendido = (veiculo: TVeiculo) => veiculo.situacao === 'Vendido';

  const colunas: ColunaTabela<TVeiculo>[] = [
    { id: 'descricao', titulo: 'Veículo', renderizar: (v) => `${v.marca} ${v.modelo}` },
    { id: 'ano', titulo: 'Ano' },
    { id: 'cor', titulo: 'Cor' },
    colunaEspecifica,
    { id: 'preco', titulo: 'Preço', renderizar: (v) => formatarMoeda(v.preco) },
    {
      id: 'situacao',
      titulo: 'Situação',
      renderizar: (v) => <Chip size="small" label={ROTULOS_SITUACAO[v.situacao]} color={vendido(v) ? 'default' : 'success'} />,
    },
  ];

  const acoes: AcaoTabela<TVeiculo>[] = [
    { dica: 'Registrar venda', icone: <PointOfSale fontSize="small" />, cor: 'primary', aoClicar: setEmVenda, desabilitada: vendido },
    { dica: 'Editar', icone: <Edit fontSize="small" />, aoClicar: (v) => { setEmEdicao(v); setFormularioAberto(true); }, desabilitada: vendido },
    { dica: 'Excluir', icone: <Delete fontSize="small" />, cor: 'error', aoClicar: setEmExclusao, desabilitada: vendido },
  ];

  return (
    <Box>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'center', gap: 2, mb: 3 }}>
        <Typography variant="h4">{textos.titulo}</Typography>
        <Button variant="contained" startIcon={<Add />} onClick={() => { setEmEdicao(undefined); setFormularioAberto(true); }}>
          {textos.botaoNovo}
        </Button>
      </Box>

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mb: 2 }}>
        <TextField
          label="Buscar por marca ou modelo"
          value={busca}
          onChange={(e) => setBusca(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && buscar({ pagina: 1, busca })}
          size="small"
        />
        <TextField
          select
          label="Situação"
          value={situacao}
          onChange={(e) => buscar({ pagina: 1, busca, situacao: e.target.value as SituacaoVeiculo | '' })}
          size="small"
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">Todas</MenuItem>
          {(Object.keys(ROTULOS_SITUACAO) as SituacaoVeiculo[]).map((s) => (
            <MenuItem key={s} value={s}>{ROTULOS_SITUACAO[s]}</MenuItem>
          ))}
        </TextField>
        <Button onClick={() => buscar({ pagina: 1, busca })}>Buscar</Button>
      </Box>

      {erro && <Alert severity="error" sx={{ mb: 2 }}>{erro}</Alert>}

      <TabelaGenerica
        dados={itens}
        colunas={colunas}
        acoes={acoes}
        obterId={(v) => v.id}
        carregando={carregando}
        paginacao={{
          total,
          pagina: pagina - 1,
          tamanhoPagina,
          aoMudarPagina: (novaPagina) => buscar({ pagina: novaPagina + 1 }),
          aoMudarTamanhoPagina: (novoTamanho) => buscar({ pagina: 1, tamanhoPagina: novoTamanho }),
        }}
      />

      <DialogoVeiculo<TRequisicao>
        aberto={formularioAberto}
        titulo={emEdicao ? textos.tituloEdicao : textos.tituloNovo}
        veiculo={emEdicao}
        campoEspecifico={campoEspecifico}
        aoSalvar={salvar}
        aoFechar={() => setFormularioAberto(false)}
      />

      <DialogoVenda
        veiculo={emVenda}
        aoConfirmar={async (requisicao) => {
          await vender(requisicao);
          setMensagemSucesso('Venda registrada com sucesso.');
        }}
        aoFechar={() => setEmVenda(undefined)}
      />

      <DialogoConfirmacao
        aberto={!!emExclusao}
        titulo={textos.tituloExclusao}
        mensagem={`Tem certeza que deseja excluir "${emExclusao?.marca} ${emExclusao?.modelo}"?`}
        aoConfirmar={confirmarExclusao}
        aoCancelar={() => setEmExclusao(undefined)}
        processando={excluindo}
      />

      <Snackbar open={!!mensagemSucesso} autoHideDuration={4000} onClose={() => setMensagemSucesso(null)} anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}>
        <Alert severity="success" variant="filled" onClose={() => setMensagemSucesso(null)}>{mensagemSucesso}</Alert>
      </Snackbar>

      <Snackbar open={!!mensagemErro} autoHideDuration={6000} onClose={() => setMensagemErro(null)} anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}>
        <Alert severity="error" variant="filled" onClose={() => setMensagemErro(null)}>{mensagemErro}</Alert>
      </Snackbar>
    </Box>
  );
}
