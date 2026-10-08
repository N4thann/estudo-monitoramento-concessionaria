namespace Concessionaria.Carros.Comum.Observabilidade;

/// <summary>Repassa o Id de operação atual em toda chamada HTTP de saída (clientes Refit).</summary>
public class PropagadorIdOperacaoHandler : DelegatingHandler
{
    /// <summary>Adiciona o cabeçalho X-Id-Operacao, se houver Id atual e a requisição ainda não tiver o cabeçalho.</summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage requisicao, CancellationToken cancellationToken)
    {
        if (ContextoOperacao.IdAtual is { } idOperacao && !requisicao.Headers.Contains(ContextoOperacao.Cabecalho))
            requisicao.Headers.TryAddWithoutValidation(ContextoOperacao.Cabecalho, idOperacao);

        return base.SendAsync(requisicao, cancellationToken);
    }
}
