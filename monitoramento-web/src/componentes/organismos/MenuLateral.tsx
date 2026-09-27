import type { ReactNode } from 'react';
import { Drawer, List, ListItemButton, ListItemIcon, ListItemText, Toolbar } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { Assessment, DirectionsCar, ReceiptLong, Speed, TwoWheeler } from '@mui/icons-material';
import { useLocation, useNavigate } from 'react-router-dom';

export const LARGURA_MENU = 220;

interface MenuLateralProps {
  abertoMobile: boolean;
  abertoDesktop: boolean;
  aoFechar: () => void;
}

interface ItemMenu {
  rotulo: string;
  destino: string;
  icone: ReactNode;
}

const ITENS_MENU: ItemMenu[] = [
  { rotulo: 'Painel', destino: '/', icone: <Speed /> },
  { rotulo: 'Carros', destino: '/carros', icone: <DirectionsCar /> },
  { rotulo: 'Motos', destino: '/motos', icone: <TwoWheeler /> },
  { rotulo: 'Vendas', destino: '/vendas', icone: <ReceiptLong /> },
  { rotulo: 'Relatórios', destino: '/relatorios', icone: <Assessment /> },
];

export const MenuLateral = ({ abertoMobile, abertoDesktop, aoFechar }: MenuLateralProps) => {
  const { pathname } = useLocation();
  const navegar = useNavigate();

  const irPara = (destino: string) => {
    navegar(destino);
    aoFechar();
  };

  const lista = (
    <List>
      {ITENS_MENU.map((item) => (
        <ListItemButton
          key={item.destino}
          selected={item.destino === '/' ? pathname === '/' : pathname.startsWith(item.destino)}
          onClick={() => irPara(item.destino)}
          sx={{
            '&:hover': { bgcolor: (tema) => alpha(tema.palette.chrome.contrastText, 0.08) },
            '&.Mui-selected': {
              bgcolor: (tema) => alpha(tema.palette.chrome.contrastText, 0.16),
              '&:hover': { bgcolor: (tema) => alpha(tema.palette.chrome.contrastText, 0.24) },
            },
          }}
        >
          <ListItemIcon sx={{ color: 'inherit', minWidth: 40 }}>{item.icone}</ListItemIcon>
          <ListItemText primary={item.rotulo} />
        </ListItemButton>
      ))}
    </List>
  );

  const estiloPapel = { width: LARGURA_MENU, bgcolor: 'chrome.main', color: 'chrome.contrastText' };

  return (
    <>
      <Drawer
        variant="temporary"
        open={abertoMobile}
        onClose={aoFechar}
        ModalProps={{ keepMounted: true }}
        sx={{ display: { xs: 'block', md: 'none' }, '& .MuiDrawer-paper': estiloPapel }}
      >
        <Toolbar />
        {lista}
      </Drawer>

      <Drawer
        variant="persistent"
        open={abertoDesktop}
        sx={{
          display: { xs: 'none', md: 'block' },
          width: abertoDesktop ? LARGURA_MENU : 0,
          flexShrink: 0,
          transition: (tema) => tema.transitions.create('width'),
          '& .MuiDrawer-paper': estiloPapel,
        }}
      >
        <Toolbar />
        {lista}
      </Drawer>
    </>
  );
};
