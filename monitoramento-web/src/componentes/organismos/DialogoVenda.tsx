import { useState, type FormEvent } from 'react';
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, InputAdornment, Stack, TextField, Typography } from '@mui/material';
import type { VeiculoBase } from '../../tipos/veiculo.tipos';
import type { RequisicaoVenda } from '../../tipos/venda.tipos';
import { extrairMensagemErro } from '../../utilitarios/erroApi';
import { formatarMoeda } from '../../utilitarios/formatacao';

interface DialogoVendaProps {
  veiculo?: VeiculoBase;
  aoConfirmar: (requisicao: RequisicaoVenda) => Promise<void>;
  aoFechar: () => void;
}

export const DialogoVenda = ({ veiculo, aoConfirmar, aoFechar }: DialogoVendaProps) => (
  <Dialog open={!!veiculo} onClose={aoFechar} fullWidth maxWidth="sm">
    {veiculo && <ConteudoDialogoVenda veiculo={veiculo} aoConfirmar={aoConfirmar} aoFechar={aoFechar} />}
  </Dialog>
);

const ConteudoDialogoVenda = ({ veiculo, aoConfirmar, aoFechar }: DialogoVendaProps & { veiculo: VeiculoBase }) => {
  const [nomeCliente, setNomeCliente] = useState('');
  const [documentoCliente, setDocumentoCliente] = useState('');
  const [valor, setValor] = useState(String(veiculo.preco));
  const [salvando, setSalvando] = useState(false);
  const [erroFormulario, setErroFormulario] = useState<string | null>(null);

  const confirmar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErroFormulario(null);
    setSalvando(true);
    try {
      await aoConfirmar({ veiculoId: veiculo.id, nomeCliente, documentoCliente, valor: Number(valor) });
      aoFechar();
    } catch (erro) {
      setErroFormulario(await extrairMensagemErro(erro, 'Erro ao registrar a venda.'));
    } finally {
      setSalvando(false);
    }
  };

  return (
    <>
      <DialogTitle>Registrar venda</DialogTitle>
      <Box component="form" onSubmit={confirmar}>
        <DialogContent>
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              {veiculo.marca} {veiculo.modelo} {veiculo.ano} — preço de tabela {formatarMoeda(veiculo.preco)}
            </Typography>
            {erroFormulario && <Alert severity="error">{erroFormulario}</Alert>}
            <TextField label="Nome do cliente" value={nomeCliente} onChange={(e) => setNomeCliente(e.target.value)} required fullWidth />
            <TextField
              label="CPF ou CNPJ"
              value={documentoCliente}
              onChange={(e) => setDocumentoCliente(e.target.value)}
              helperText="Apenas números ou com pontuação"
              required
              fullWidth
            />
            <TextField
              label="Valor da venda"
              type="number"
              value={valor}
              onChange={(e) => setValor(e.target.value)}
              slotProps={{ input: { startAdornment: <InputAdornment position="start">R$</InputAdornment> }, htmlInput: { min: 0, step: '0.01' } }}
              required
              fullWidth
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={aoFechar} disabled={salvando}>Cancelar</Button>
          <Button type="submit" variant="contained" disabled={salvando}>Confirmar venda</Button>
        </DialogActions>
      </Box>
    </>
  );
};
