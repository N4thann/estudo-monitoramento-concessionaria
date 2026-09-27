import { lazy } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { LayoutPrincipal } from '../componentes/templates/LayoutPrincipal';

const PaginaPainel = lazy(() => import('../paginas/painel/PaginaPainel').then((m) => ({ default: m.PaginaPainel })));
const PaginaCarros = lazy(() => import('../paginas/carros/PaginaCarros').then((m) => ({ default: m.PaginaCarros })));
const PaginaMotos = lazy(() => import('../paginas/motos/PaginaMotos').then((m) => ({ default: m.PaginaMotos })));
const PaginaVendas = lazy(() => import('../paginas/vendas/PaginaVendas').then((m) => ({ default: m.PaginaVendas })));
const PaginaRelatorios = lazy(() => import('../paginas/relatorios/PaginaRelatorios').then((m) => ({ default: m.PaginaRelatorios })));

export const Rotas = () => (
  <Routes>
    <Route element={<LayoutPrincipal />}>
      <Route path="/" element={<PaginaPainel />} />
      <Route path="/carros" element={<PaginaCarros />} />
      <Route path="/motos" element={<PaginaMotos />} />
      <Route path="/vendas" element={<PaginaVendas />} />
      <Route path="/relatorios" element={<PaginaRelatorios />} />
    </Route>

    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
);
