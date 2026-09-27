import type { Periodo } from '../tipos/venda.tipos';

const formatadorMoeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const formatadorDataHora = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' });

export const formatarMoeda = (valor: number) => formatadorMoeda.format(valor);

export const formatarDataHora = (dataIso: string) => formatadorDataHora.format(new Date(dataIso));

export const formatarDocumento = (digitos: string) =>
  digitos.length === 11
    ? digitos.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4')
    : digitos.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, '$1.$2.$3/$4-$5');

// yyyy-MM-dd no fuso local, formato aceito pelo <input type="date"> e pela API.
export const paraValorCampoData = (data: Date) => {
  const deslocamento = data.getTimezoneOffset() * 60_000;
  return new Date(data.getTime() - deslocamento).toISOString().slice(0, 10);
};

export const ultimosDias = (dias: number): Periodo => {
  const ate = new Date();
  const de = new Date();
  de.setDate(ate.getDate() - dias);
  return { de: paraValorCampoData(de), ate: paraValorCampoData(ate) };
};
