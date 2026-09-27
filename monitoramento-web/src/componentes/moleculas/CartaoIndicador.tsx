import { Paper, Typography } from '@mui/material';

interface CartaoIndicadorProps {
  rotulo: string;
  valor: string;
  legenda?: string;
}

export const CartaoIndicador = ({ rotulo, valor, legenda }: CartaoIndicadorProps) => (
  <Paper sx={{ p: 2.5, height: '100%' }}>
    <Typography variant="body2" color="text.secondary">{rotulo}</Typography>
    <Typography variant="h5" sx={{ fontWeight: 600, mt: 0.5, fontVariantNumeric: 'tabular-nums' }}>{valor}</Typography>
    {legenda && <Typography variant="caption" color="text.secondary">{legenda}</Typography>}
  </Paper>
);
