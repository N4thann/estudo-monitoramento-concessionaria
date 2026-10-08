using System.Net;
using System.Net.Http.Headers;
using Refit;

namespace Concessionaria.Carros.tests.Common.Refit
{
    /// <summary>
    /// Monta as respostas que o cliente Refit de Relatórios devolve: sucesso, erro HTTP e falha de rede
    /// (que o <see cref="IApiResponse{T}"/> captura sem lançar exceção, com <c>StatusCode</c> nulo).
    /// </summary>
    public static class RespostasApi
    {
        private static readonly RefitSettings Configuracao = new();

        /// <summary>Resposta 200 com o conteúdo já desserializado.</summary>
        public static IApiResponse<T> Sucesso<T>(T conteudo) =>
            new ApiResponse<T>(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = NovaRequisicao() }, conteudo, Configuracao);

        /// <summary>Resposta 200 com um arquivo e, opcionalmente, o cabeçalho Content-Disposition.</summary>
        public static IApiResponse<Stream> Arquivo(byte[] conteudo, ContentDispositionHeaderValue? disposicao = null)
        {
            var resposta = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(conteudo), RequestMessage = NovaRequisicao() };
            resposta.Content.Headers.ContentDisposition = disposicao;
            return new ApiResponse<Stream>(resposta, new MemoryStream(conteudo), Configuracao);
        }

        /// <summary>Resposta de erro HTTP (4xx/5xx) com o <see cref="ApiException"/> que o Refit anexaria.</summary>
        public static async Task<IApiResponse<T>> ErroHttpAsync<T>(HttpStatusCode status, string corpo = "")
        {
            var requisicao = NovaRequisicao();
            var resposta = new HttpResponseMessage(status) { Content = new StringContent(corpo), RequestMessage = requisicao };
            var erro = await ApiException.Create(requisicao, HttpMethod.Post, resposta, Configuracao);
            return new ApiResponse<T>(resposta, default, Configuracao, erro);
        }

        /// <summary>Requisição sem resposta (conexão recusada, timeout, circuito aberto): a causa fica em InnerException.</summary>
        public static IApiResponse<T> FalhaRede<T>(Exception causa)
        {
            var requisicao = NovaRequisicao();
            var erro = new ApiRequestException(requisicao, HttpMethod.Post, Configuracao, causa);
            return new ApiResponse<T>(requisicao, null!, default, Configuracao, erro);
        }

        /// <summary>Requisição de origem: o Refit exige que toda resposta aponte para a requisição que a gerou.</summary>
        private static HttpRequestMessage NovaRequisicao() => new(HttpMethod.Post, "http://relatorios/");
    }
}
