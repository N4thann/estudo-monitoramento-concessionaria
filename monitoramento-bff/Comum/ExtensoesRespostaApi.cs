using Refit;

namespace Concessionaria.Bff.Comum;

// Converte a resposta de Carros/Motos (Refit) em resposta do BFF:
// - sucesso: devolve o conteúdo tipado;
// - 4xx: repassa o ProblemDetails do serviço como veio (validação, não encontrado, conflito);
// - 5xx: vira 502 (o problema está em um serviço dependente), mantendo o ProblemDetails original;
// - sem resposta (conexão recusada, timeout, circuito aberto): relança para o ManipuladorFalhaIntegracao.
public static class ExtensoesRespostaApi
{
    public static IResult ParaResultado<T>(this IApiResponse<T> resposta, Func<T, IResult> aoSucesso) =>
        resposta.IsSuccessStatusCode && resposta.Content is not null
            ? aoSucesso(resposta.Content)
            : ParaFalha(resposta);

    public static IResult ParaResultado<T>(this IApiResponse<T> resposta) =>
        resposta.ParaResultado(conteudo => Results.Ok(conteudo));

    public static IResult ParaResultadoSemConteudo(this IApiResponse resposta) =>
        resposta.IsSuccessStatusCode ? Results.NoContent() : ParaFalha(resposta);

    public static async Task<IResult> ParaArquivoAsync(this IApiResponse<Stream> resposta, CancellationToken ct)
    {
        if (!resposta.IsSuccessStatusCode || resposta.Content is null)
            return ParaFalha(resposta);

        using var memoria = new MemoryStream();
        await resposta.Content.CopyToAsync(memoria, ct);

        var disposicao = resposta.ContentHeaders?.ContentDisposition;
        var nomeArquivo = disposicao?.FileNameStar ?? disposicao?.FileName?.Trim('"') ?? "relatorio.xlsx";
        var tipoConteudo = resposta.ContentHeaders?.ContentType?.MediaType ?? "application/octet-stream";

        return Results.File(memoria.ToArray(), tipoConteudo, nomeArquivo);
    }

    public static IResult ParaFalha(IApiResponse resposta)
    {
        if (resposta.StatusCode is not { } status)
            throw (Exception?)resposta.Error ?? new HttpRequestException("Falha sem resposta ao chamar serviço dependente.");

        var codigo = (int)status;
        var codigoBff = codigo >= 500 ? StatusCodes.Status502BadGateway : codigo;

        // O corpo (ProblemDetails) do serviço é repassado para manter a mensagem e o código de erro originais.
        return resposta.Error is ApiException { Content: { Length: > 0 } conteudo }
            ? Results.Content(conteudo, "application/problem+json", statusCode: codigoBff)
            : Results.Problem(statusCode: codigoBff, title: $"Serviço dependente respondeu com erro {codigo}.");
    }
}
