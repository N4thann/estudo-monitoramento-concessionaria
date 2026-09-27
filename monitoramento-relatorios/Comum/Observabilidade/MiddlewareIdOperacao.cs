using System.Diagnostics;
using Serilog.Context;

namespace Concessionaria.Relatorios.Comum.Observabilidade;

public class MiddlewareIdOperacao(RequestDelegate proximo)
{
    private const int TamanhoMaximo = 64;

    public async Task InvokeAsync(HttpContext contexto)
    {
        var idOperacao = contexto.Request.Headers[ContextoOperacao.Cabecalho].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idOperacao) || idOperacao.Length > TamanhoMaximo)
            idOperacao = Guid.NewGuid().ToString("N");

        ContextoOperacao.IdAtual = idOperacao;
        // Liga o Id de operação ao trace: dá para buscar no Tempo/Jaeger pelo mesmo id que aparece no log e no erro.
        Activity.Current?.SetTag("concessionaria.id_operacao", idOperacao);
        contexto.Response.OnStarting(() =>
        {
            contexto.Response.Headers[ContextoOperacao.Cabecalho] = idOperacao;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(ContextoOperacao.PropriedadeLog, idOperacao))
        {
            await proximo(contexto);
        }
    }
}
