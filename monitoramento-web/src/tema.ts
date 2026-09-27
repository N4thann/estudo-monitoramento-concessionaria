import { createTheme } from '@mui/material/styles';

declare module '@mui/material/styles' {
  interface Palette {
    chrome: Palette['primary'];
  }
  interface PaletteOptions {
    chrome?: PaletteOptions['primary'];
  }
}

const { palette: paletaBase } = createTheme();

export const tema = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#1E40AF' },
    secondary: { main: '#0D9488' },
    background: { default: '#F1F5F9', paper: '#FFFFFF' },
    chrome: paletaBase.augmentColor({ color: { main: '#0F172A' } }),
  },
  shape: { borderRadius: 12 },
});
