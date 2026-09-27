import type { ReactNode } from 'react';
import {
  Box, CircularProgress, IconButton, Paper, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TablePagination, TableRow, Tooltip, Typography,
} from '@mui/material';
import type { AcaoTabela, ColunaTabela } from '../../tipos/comum.tipos';

interface Paginacao {
  total: number;
  pagina: number;
  tamanhoPagina: number;
  aoMudarPagina: (novaPagina: number) => void;
  aoMudarTamanhoPagina: (novoTamanho: number) => void;
}

interface TabelaGenericaProps<T> {
  dados: T[];
  colunas: ColunaTabela<T>[];
  acoes?: AcaoTabela<T>[];
  obterId: (item: T) => string;
  carregando?: boolean;
  paginacao?: Paginacao;
}

export function TabelaGenerica<T>({ dados, colunas, acoes = [], obterId, carregando, paginacao }: TabelaGenericaProps<T>) {
  const temAcoes = acoes.length > 0;
  const totalColunas = colunas.length + (temAcoes ? 1 : 0);

  const renderizarCelula = (coluna: ColunaTabela<T>, item: T): ReactNode =>
    coluna.renderizar ? coluna.renderizar(item) : String(item[coluna.id as keyof T] ?? '');

  const renderizarAcoes = (item: T) => acoes.map((acao) => (
    <Tooltip key={acao.dica} title={acao.dica}>
      <span>
        <IconButton
          size="small"
          color={acao.cor ?? 'default'}
          disabled={acao.desabilitada?.(item) ?? false}
          onClick={() => acao.aoClicar(item)}
        >
          {acao.icone}
        </IconButton>
      </span>
    </Tooltip>
  ));

  const vazioOuCarregando = carregando
    ? <CircularProgress size={24} sx={{ my: 2 }} />
    : <Typography variant="body2" color="text.secondary" sx={{ my: 2 }}>Nenhum registro encontrado.</Typography>;

  const temLinhas = !carregando && dados.length > 0;

  return (
    <Paper>
      <TableContainer sx={{ display: { xs: 'none', sm: 'block' } }}>
        <Table>
          <TableHead>
            <TableRow>
              {colunas.map((coluna) => (
                <TableCell key={String(coluna.id)} sx={{ width: coluna.largura }}>{coluna.titulo}</TableCell>
              ))}
              {temAcoes && <TableCell align="right">Ações</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {!temLinhas ? (
              <TableRow>
                <TableCell colSpan={totalColunas} align="center">{vazioOuCarregando}</TableCell>
              </TableRow>
            ) : dados.map((item) => (
              <TableRow key={obterId(item)}>
                {colunas.map((coluna) => (
                  <TableCell key={String(coluna.id)}>{renderizarCelula(coluna, item)}</TableCell>
                ))}
                {temAcoes && (
                  <TableCell align="right">
                    <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 0.5 }}>{renderizarAcoes(item)}</Box>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Stack spacing={1.5} sx={{ display: { xs: 'flex', sm: 'none' }, p: 2 }}>
        {!temLinhas ? (
          <Box sx={{ display: 'flex', justifyContent: 'center' }}>{vazioOuCarregando}</Box>
        ) : dados.map((item) => (
          <Paper key={obterId(item)} variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>{renderizarCelula(colunas[0], item)}</Typography>
            {colunas.slice(1).map((coluna) => (
              <Box key={String(coluna.id)} sx={{ mt: 1 }}>
                <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', display: 'block' }}>
                  {coluna.titulo}
                </Typography>
                <Typography variant="body2" component="div">{renderizarCelula(coluna, item)}</Typography>
              </Box>
            ))}
            {temAcoes && (
              <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 0.5, mt: 1.5 }}>{renderizarAcoes(item)}</Box>
            )}
          </Paper>
        ))}
      </Stack>

      {paginacao && (
        <TablePagination
          component="div"
          count={paginacao.total}
          page={paginacao.pagina}
          rowsPerPage={paginacao.tamanhoPagina}
          rowsPerPageOptions={[5, 10, 25]}
          labelRowsPerPage="Linhas por página:"
          labelDisplayedRows={({ from, to, count }) => `${from}–${to} de ${count}`}
          onPageChange={(_, novaPagina) => paginacao.aoMudarPagina(novaPagina)}
          onRowsPerPageChange={(e) => paginacao.aoMudarTamanhoPagina(Number(e.target.value))}
        />
      )}
    </Paper>
  );
}
