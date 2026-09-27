import { useState, type FormEvent } from 'react';
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, InputAdornment, Stack, TextField } from '@mui/material';
import type { VeiculoBase } from '../../tipos/veiculo.tipos';
import { extrairMensagemErro } from '../../utilitarios/erroApi';

// Campo numérico que só um dos tipos de veículo tem (portas no carro, cilindrada na moto).
export interface CampoEspecifico {
  chave: string;
  rotulo: string;
  minimo: number;
  maximo: number;
}

interface DialogoVeiculoProps<TRequisicao> {
  aberto: boolean;
  titulo: string;
  veiculo?: VeiculoBase;
  campoEspecifico: CampoEspecifico;
  aoSalvar: (requisicao: TRequisicao) => Promise<void>;
  aoFechar: () => void;
}

export function DialogoVeiculo<TRequisicao>({ aberto, aoFechar, ...props }: DialogoVeiculoProps<TRequisicao>) {
  return (
    <Dialog open={aberto} onClose={aoFechar} fullWidth maxWidth="sm">
      {aberto && <ConteudoDialogoVeiculo aoFechar={aoFechar} {...props} />}
    </Dialog>
  );
}

function ConteudoDialogoVeiculo<TRequisicao>({ titulo, veiculo, campoEspecifico, aoSalvar, aoFechar }: Omit<DialogoVeiculoProps<TRequisicao>, 'aberto'>) {
  const valorEspecificoInicial = veiculo ? (veiculo as unknown as Record<string, number>)[campoEspecifico.chave] : undefined;

  const [marca, setMarca] = useState(veiculo?.marca ?? '');
  const [modelo, setModelo] = useState(veiculo?.modelo ?? '');
  const [ano, setAno] = useState(String(veiculo?.ano ?? new Date().getFullYear()));
  const [cor, setCor] = useState(veiculo?.cor ?? '');
  const [preco, setPreco] = useState(veiculo ? String(veiculo.preco) : '');
  const [valorEspecifico, setValorEspecifico] = useState(valorEspecificoInicial !== undefined ? String(valorEspecificoInicial) : '');
  const [salvando, setSalvando] = useState(false);
  const [erroFormulario, setErroFormulario] = useState<string | null>(null);

  const salvar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErroFormulario(null);
    setSalvando(true);
    try {
      const requisicao = { marca, modelo, ano: Number(ano), cor, preco: Number(preco), [campoEspecifico.chave]: Number(valorEspecifico) };
      await aoSalvar(requisicao as TRequisicao);
      aoFechar();
    } catch (erro) {
      setErroFormulario(await extrairMensagemErro(erro, 'Erro ao salvar o veículo.'));
    } finally {
      setSalvando(false);
    }
  };

  return (
    <>
      <DialogTitle>{titulo}</DialogTitle>
      <Box component="form" onSubmit={salvar}>
        <DialogContent>
          <Stack spacing={2}>
            {erroFormulario && <Alert severity="error">{erroFormulario}</Alert>}
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <TextField label="Marca" value={marca} onChange={(e) => setMarca(e.target.value)} required fullWidth />
              <TextField label="Modelo" value={modelo} onChange={(e) => setModelo(e.target.value)} required fullWidth />
            </Stack>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <TextField label="Ano" type="number" value={ano} onChange={(e) => setAno(e.target.value)} required fullWidth />
              <TextField label="Cor" value={cor} onChange={(e) => setCor(e.target.value)} required fullWidth />
            </Stack>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <TextField
                label={campoEspecifico.rotulo}
                type="number"
                value={valorEspecifico}
                onChange={(e) => setValorEspecifico(e.target.value)}
                slotProps={{ htmlInput: { min: campoEspecifico.minimo, max: campoEspecifico.maximo } }}
                required
                fullWidth
              />
              <TextField
                label="Preço"
                type="number"
                value={preco}
                onChange={(e) => setPreco(e.target.value)}
                slotProps={{ input: { startAdornment: <InputAdornment position="start">R$</InputAdornment> }, htmlInput: { min: 0, step: '0.01' } }}
                required
                fullWidth
              />
            </Stack>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={aoFechar} disabled={salvando}>Cancelar</Button>
          <Button type="submit" variant="contained" disabled={salvando}>Salvar</Button>
        </DialogActions>
      </Box>
    </>
  );
}
