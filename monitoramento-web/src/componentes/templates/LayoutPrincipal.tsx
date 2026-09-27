import { Suspense, useState } from 'react';
import { Outlet } from 'react-router-dom';
import { AppBar, Box, CircularProgress, IconButton, Toolbar, Typography, useMediaQuery } from '@mui/material';
import { Close as IconeFechar, Menu as IconeMenu } from '@mui/icons-material';
import { useTheme } from '@mui/material/styles';
import { MenuLateral, LARGURA_MENU } from '../organismos/MenuLateral';

export const LayoutPrincipal = () => {
  const temaMui = useTheme();
  const ehDesktop = useMediaQuery(temaMui.breakpoints.up('md'));
  const [abertoMobile, setAbertoMobile] = useState(false);
  const [abertoDesktop, setAbertoDesktop] = useState(true);

  const menuAberto = ehDesktop ? abertoDesktop : abertoMobile;

  const alternarMenu = () => {
    if (ehDesktop) setAbertoDesktop((aberto) => !aberto);
    else setAbertoMobile((aberto) => !aberto);
  };

  return (
    <Box sx={{ display: 'flex' }}>
      <AppBar position="fixed" sx={{ zIndex: (tema) => tema.zIndex.drawer + 1, bgcolor: 'chrome.main', color: 'chrome.contrastText' }}>
        <Toolbar sx={{ gap: 1 }}>
          <IconButton color="inherit" edge="start" onClick={alternarMenu} aria-label={menuAberto ? 'Recolher menu' : 'Expandir menu'}>
            {menuAberto ? <IconeFechar /> : <IconeMenu />}
          </IconButton>
          <Typography variant="h6" component="div">Concessionária</Typography>
        </Toolbar>
      </AppBar>

      <MenuLateral abertoMobile={abertoMobile} abertoDesktop={abertoDesktop} aoFechar={() => setAbertoMobile(false)} />

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          minWidth: 0,
          p: { xs: 2, md: 3 },
          width: { xs: '100%', md: abertoDesktop ? `calc(100% - ${LARGURA_MENU}px)` : '100%' },
        }}
      >
        <Toolbar />
        <Suspense fallback={(
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
            <CircularProgress />
          </Box>
        )}>
          <Outlet />
        </Suspense>
      </Box>
    </Box>
  );
};
