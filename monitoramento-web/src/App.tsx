import { BrowserRouter } from 'react-router-dom';
import { CssBaseline, ThemeProvider } from '@mui/material';
import { tema } from './tema';
import { Rotas } from './rotas';

function App() {
  return (
    <ThemeProvider theme={tema}>
      <CssBaseline />
      <BrowserRouter>
        <Rotas />
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App;
