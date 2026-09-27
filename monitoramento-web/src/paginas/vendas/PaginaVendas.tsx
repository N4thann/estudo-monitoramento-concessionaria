import { useEffect, useMemo, useState } from 'react';
import { Alert, Box, Button, Chip, Stack, Typography } from '@mui/material';
import { TabelaGenerica } from '../../componentes/organismos/TabelaGenerica';
import { FiltroPeriodo } from '../../componentes/moleculas/FiltroPeriodo';
import { useEstadoVendas } from '../../estado/vendas/useEstadoVendas';
import type { ColunaTabela } from '../../tipos/comum.tipos';
import type { Venda } from '../../tipos/venda.tipos';
import { formatarDataHora, formatarDocumento, formatarMoeda, ultimosDias } from '../../utilitarios/formatacao';

const colunas: ColunaTabela<Venda>[] = [
  { id: 'descricaoVeiculo', titulo: 'Veículo' },
  { id: 'tipoVeiculo', titulo: 'Tipo', renderizar: (v) => <Chip size="small" variant="outlined" label={v.tipoVeiculo} /> },
  { id: 'vendidaEm', titulo: 'Data', renderizar: (v) => formatarDataHora(v.vendidaEm) },
  { id: 'nomeCliente', titulo: 'Cliente' },
  { id: 'documentoCliente', titulo: 'Documento', renderizar: (v) => formatarDocumento(v.documentoCliente) },
  { id: 'valor', titulo: 'Valor', renderizar: (v) => formatarMoeda(v.valor) },
];

export const PaginaVendas = () => {
  const { vendas, carregando, erro, buscar } = useEstadoVendas();
  const [periodo, setPeriodo] = useState(() => ultimosDias(30));

  useEffect(() => {
    buscar(ultimosDias(30));
  }, [buscar]);

  const receita = useMemo(() => vendas.reduce((soma, v) => soma + v.valor, 0), [vendas]);

  return (
    <Box>
      <Typography variant="h4" sx={{ mb: 3 }}>Vendas</Typography>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2, alignItems: { sm: 'center' } }}>
        <FiltroPeriodo valor={periodo} aoMudar={setPeriodo} />
        <Button onClick={() => buscar(periodo)}>Filtrar</Button>
      </Stack>

      {erro && <Alert severity="error" sx={{ mb: 2 }}>{erro}</Alert>}

      <TabelaGenerica dados={vendas} colunas={colunas} obterId={(v) => v.id} carregando={carregando} />

      {!carregando && vendas.length > 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 2, textAlign: 'right' }}>
          {vendas.length} venda(s) no período — total de {formatarMoeda(receita)}
        </Typography>
      )}
    </Box>
  );
};
