import { isAxiosError } from 'axios';

interface ProblemDetails {
  title?: string;
  errors?: Record<string, string[]>;
  idOperacao?: string;
}

// Validação devolve a lista de erros por campo; os demais erros (404/409/502) trazem só o título.
// O Id de operação vai junto na mensagem para localizar a requisição nos logs das APIs.
export async function extrairMensagemErro(erro: unknown, padrao: string): Promise<string> {
  if (!isAxiosError(erro) || !erro.response?.data) return padrao;

  const bruto = erro.response.data;
  let dados: ProblemDetails;
  try {
    dados = bruto instanceof Blob ? JSON.parse(await bruto.text()) : bruto;
  } catch {
    return padrao;
  }

  const errosPorCampo = dados.errors ? Object.values(dados.errors).flat() : [];
  const mensagem = errosPorCampo.length > 0 ? errosPorCampo.join(' ') : dados.title ?? padrao;

  return dados.idOperacao ? `${mensagem} (Id da operação: ${dados.idOperacao})` : mensagem;
}
