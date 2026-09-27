import { useState } from 'react';
import { Alert, Box, Button, Grid, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import { Download } from '@mui/icons-material';
import { FiltroPeriodo } from '../../componentes/moleculas/FiltroPeriodo';
import { servicoCarros, servicoMotos } from '../../servicos/servicoVeiculos';
import { extrairMensagemErro } from '../../utilitarios/erroApi';
import { ultimosDias } from '../../utilitarios/formatacao';

type TipoRelatorio = 'vendas' | 'estoque';
type TipoVeiculoRelatorio = 'carros' | 'motos';

const SERVICOS = { carros: servicoCarros, motos: servicoMotos };

export const PaginaRelatorios = () => {
  const [tipoVeiculo, setTipoVeiculo] = useState<TipoVeiculoRelatorio>('carros');
  const [periodo, setPeriodo] = useState(() => ultimosDias(30));
  const [gerando, setGerando] = useState<TipoRelatorio | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  const baixar = async (tipo: TipoRelatorio) => {
    setErro(null);
    setGerando(tipo);
    try {
      const servico = SERVICOS[tipoVeiculo];
      if (tipo === 'vendas') await servico.baixarRelatorioVendas(periodo);
      else await servico.baixarRelatorioEstoque();
    } catch (e) {
      setErro(await extrairMensagemErro(e, 'Erro ao gerar o relatório.'));
    } finally {
      setGerando(null);
    }
  };

  return (
    <Box>
      <Typography variant="h4" sx={{ mb: 3 }}>Relatórios</Typography>

      <TextField
        select
        label="Tipo de veículo"
        size="small"
        value={tipoVeiculo}
        onChange={(e) => setTipoVeiculo(e.target.value as TipoVeiculoRelatorio)}
        sx={{ minWidth: 200, mb: 3 }}
      >
        <MenuItem value="carros">Carros</MenuItem>
        <MenuItem value="motos">Motos</MenuItem>
      </TextField>

      {erro && <Alert severity="error" sx={{ mb: 2 }}>{erro}</Alert>}

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper sx={{ p: 3, height: '100%' }}>
            <Stack spacing={2}>
              <Typography variant="h6">Vendas por período</Typography>
              <Typography variant="body2" color="text.secondary">
                Planilha com os indicadores do período (quantidade, receita, ticket médio, maior venda e marca mais vendida) e a lista de vendas.
              </Typography>
              <FiltroPeriodo valor={periodo} aoMudar={setPeriodo} />
              <Box>
                <Button variant="contained" startIcon={<Download />} onClick={() => baixar('vendas')} disabled={gerando !== null}>
                  {gerando === 'vendas' ? 'Gerando...' : 'Baixar planilha'}
                </Button>
              </Box>
            </Stack>
          </Paper>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <Paper sx={{ p: 3, height: '100%' }}>
            <Stack spacing={2}>
              <Typography variant="h6">Estoque atual</Typography>
              <Typography variant="body2" color="text.secondary">
                Planilha com os veículos disponíveis para venda, o valor total em estoque e o preço médio.
              </Typography>
              <Box>
                <Button variant="contained" startIcon={<Download />} onClick={() => baixar('estoque')} disabled={gerando !== null}>
                  {gerando === 'estoque' ? 'Gerando...' : 'Baixar planilha'}
                </Button>
              </Box>
            </Stack>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};
