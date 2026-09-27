namespace Concessionaria.Motos.Comum.Observabilidade;

// Repassa o Id de operação atual em toda chamada HTTP de saída (clientes Refit).
public class PropagadorIdOperacaoHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage requisicao, CancellationToken cancellationToken)
    {
        if (ContextoOperacao.IdAtual is { } idOperacao && !requisicao.Headers.Contains(ContextoOperacao.Cabecalho))
            requisicao.Headers.TryAddWithoutValidation(ContextoOperacao.Cabecalho, idOperacao);

        return base.SendAsync(requisicao, cancellationToken);
    }
}
