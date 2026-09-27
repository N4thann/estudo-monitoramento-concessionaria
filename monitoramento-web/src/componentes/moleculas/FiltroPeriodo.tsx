import { Stack, TextField } from '@mui/material';
import type { Periodo } from '../../tipos/venda.tipos';

interface FiltroPeriodoProps {
  valor: Periodo;
  aoMudar: (valor: Periodo) => void;
}

export const FiltroPeriodo = ({ valor, aoMudar }: FiltroPeriodoProps) => (
  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
    <TextField
      label="De"
      type="date"
      size="small"
      value={valor.de ?? ''}
      onChange={(e) => aoMudar({ ...valor, de: e.target.value })}
      slotProps={{ inputLabel: { shrink: true } }}
    />
    <TextField
      label="Até"
      type="date"
      size="small"
      value={valor.ate ?? ''}
      onChange={(e) => aoMudar({ ...valor, ate: e.target.value })}
      slotProps={{ inputLabel: { shrink: true } }}
    />
  </Stack>
);
