import { Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle } from '@mui/material';

interface DialogoConfirmacaoProps {
  aberto: boolean;
  titulo: string;
  mensagem: string;
  aoConfirmar: () => void;
  aoCancelar: () => void;
  processando?: boolean;
}

export const DialogoConfirmacao = ({ aberto, titulo, mensagem, aoConfirmar, aoCancelar, processando }: DialogoConfirmacaoProps) => (
  <Dialog open={aberto} onClose={aoCancelar}>
    <DialogTitle>{titulo}</DialogTitle>
    <DialogContent>
      <DialogContentText>{mensagem}</DialogContentText>
    </DialogContent>
    <DialogActions>
      <Button onClick={aoCancelar} disabled={processando}>Cancelar</Button>
      <Button onClick={aoConfirmar} color="error" variant="contained" disabled={processando}>Excluir</Button>
    </DialogActions>
  </Dialog>
);
