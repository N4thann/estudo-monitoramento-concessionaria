using System.Net;

namespace Concessionaria.Bff.tests.Fakes.Http
{
    /// <summary>Fim da cadeia de handlers: guarda a requisição que chegou e responde 200 sem ir à rede.</summary>
    public sealed class HandlerCapturaRequisicao : HttpMessageHandler
    {
        /// <summary>Última requisição recebida.</summary>
        public HttpRequestMessage? Recebida { get; private set; }

        /// <summary>Captura a requisição e devolve 200.</summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Recebida = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
