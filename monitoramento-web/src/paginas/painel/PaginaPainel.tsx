import { useEffect } from 'react';
import { Alert, Box, Button, Grid, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import { Refresh } from '@mui/icons-material';
import { CartaoIndicador } from '../../componentes/moleculas/CartaoIndicador';
import { useEstadoPainel } from '../../estado/painel/useEstadoPainel';
import type { Resumo } from '../../tipos/painel.tipos';
import { formatarMoeda } from '../../utilitarios/formatacao';

const INDISPONIVEL = 'Indisponível';

export const PaginaPainel = () => {
  const { painel, carregando, erro, buscar } = useEstadoPainel();

  useEffect(() => {
    buscar();
  }, [buscar]);

  const total = painel?.total;
  const semIndicadores = !!painel && (!painel.carros.indicadores || !painel.motos.indicadores);
  const linhas: [string, Resumo | undefined][] = [
    ['Carros', painel?.carros],
    ['Motos', painel?.motos],
  ];

  const moedaOuIndisponivel = (valor: number | null | undefined) =>
    valor === null ? INDISPONIVEL : valor === undefined ? '—' : formatarMoeda(valor);

  return (
    <Box>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'center', gap: 2, mb: 3 }}>
        <Typography variant="h4">Painel</Typography>
        <Button startIcon={<Refresh />} onClick={buscar} disabled={carregando}>Atualizar</Button>
      </Box>

      {erro && <Alert severity="error" sx={{ mb: 2 }}>{erro}</Alert>}
      {semIndicadores && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Indicadores de vendas indisponíveis no momento (serviço de relatórios fora do ar). Os dados de estoque continuam atualizados.
        </Alert>
      )}

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <CartaoIndicador rotulo="Veículos disponíveis" valor={total ? String(total.disponiveis) : '—'} legenda={total ? `${total.totalVeiculos} cadastrados` : undefined} />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <CartaoIndicador rotulo="Valor em estoque" valor={total ? formatarMoeda(total.valorEmEstoque) : '—'} />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <CartaoIndicador rotulo="Vendas realizadas" valor={total ? String(total.quantidadeVendas ?? INDISPONIVEL) : '—'} />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <CartaoIndicador rotulo="Receita total" valor={moedaOuIndisponivel(total?.receita)} />
        </Grid>
      </Grid>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Tipo</TableCell>
              <TableCell align="right">Disponíveis</TableCell>
              <TableCell align="right">Vendidos</TableCell>
              <TableCell align="right">Valor em estoque</TableCell>
              <TableCell align="right">Receita</TableCell>
              <TableCell align="right">Ticket médio</TableCell>
              <TableCell>Marca mais vendida</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {linhas.map(([rotulo, resumo]) => (
              <TableRow key={rotulo}>
                <TableCell>{rotulo}</TableCell>
                <TableCell align="right">{resumo?.disponiveis ?? '—'}</TableCell>
                <TableCell align="right">{resumo?.vendidos ?? '—'}</TableCell>
                <TableCell align="right">{resumo ? formatarMoeda(resumo.valorEmEstoque) : '—'}</TableCell>
                <TableCell align="right">{moedaOuIndisponivel(resumo && (resumo.indicadores?.receita ?? null))}</TableCell>
                <TableCell align="right">{moedaOuIndisponivel(resumo && (resumo.indicadores?.ticketMedio ?? null))}</TableCell>
                <TableCell>{resumo ? (resumo.indicadores ? resumo.indicadores.marcaMaisVendida ?? '—' : INDISPONIVEL) : '—'}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
};
