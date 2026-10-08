namespace Concessionaria.Bff.tests.Fakes.Http
{
    /// <summary>
    /// Handler primário no lugar da rede: cada requisição recebe a resposta (ou a exceção) da função informada, e as
    /// requisições que chegaram são contadas e guardadas. Serve para medir quantas tentativas o pipeline de
    /// resiliência deixou passar.
    /// </summary>
    public sealed class HandlerRespostasProgramadas(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly List<HttpRequestMessage> _recebidas = [];

        /// <summary>Quantidade de requisições que chegaram até aqui (tentativas reais).</summary>
        public int Chamadas
        {
            get { lock (_recebidas) return _recebidas.Count; }
        }

        /// <summary>Requisições recebidas, na ordem.</summary>
        public IReadOnlyList<HttpRequestMessage> Recebidas
        {
            get { lock (_recebidas) return _recebidas.ToList(); }
        }

        /// <summary>Registra a requisição e devolve o resultado da função; exceção dela vira tarefa com falha, como na rede.</summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (_recebidas) _recebidas.Add(request);
            try
            {
                var resposta = responder(request);
                resposta.RequestMessage ??= request;
                return Task.FromResult(resposta);
            }
            catch (Exception excecao)
            {
                return Task.FromException<HttpResponseMessage>(excecao);
            }
        }
    }
}
