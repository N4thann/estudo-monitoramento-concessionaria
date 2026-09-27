import type { ReactNode } from 'react';

export interface ResultadoPaginado<T> {
  itens: T[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}

export interface ColunaTabela<T> {
  id: keyof T | string;
  titulo: string;
  largura?: string;
  renderizar?: (item: T) => ReactNode;
}

export interface AcaoTabela<T> {
  dica: string;
  icone: ReactNode;
  aoClicar: (item: T) => void;
  cor?: 'inherit' | 'primary' | 'secondary' | 'success' | 'error' | 'info' | 'warning';
  desabilitada?: (item: T) => boolean;
}
