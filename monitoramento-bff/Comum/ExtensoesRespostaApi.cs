using Refit;

namespace Concessionaria.Bff.Comum;

/// <summary>
/// Converte a resposta de Carros/Motos (Refit) em resposta do BFF:
/// sucesso devolve o conteúdo; 4xx repassa o ProblemDetails do serviço como veio (validação, não encontrado,
/// conflito); 5xx vira 502 (o problema está num serviço dependente), mantendo o ProblemDetails original;
/// sem resposta (conexão recusada, timeout, circuito aberto) relança para o <see cref="ManipuladorFalhaIntegracao"/>.
/// </summary>
public static class ExtensoesRespostaApi
{
    /// <summary>Sucesso com conteúdo vira o resultado de <paramref name="aoSucesso"/>; qualquer outro caso, <see cref="ParaFalha"/>.</summary>
    public static IResult ParaResultado<T>(this IApiResponse<T> resposta, Func<T, IResult> aoSucesso) =>
        resposta.IsSuccessStatusCode && resposta.Content is not null
            ? aoSucesso(resposta.Content)
            : ParaFalha(resposta);

    /// <summary>Sucesso com conteúdo vira 200 com o conteúdo; qualquer outro caso, <see cref="ParaFalha"/>.</summary>
    public static IResult ParaResultado<T>(this IApiResponse<T> resposta) =>
        resposta.ParaResultado(conteudo => Results.Ok(conteudo));

    /// <summary>Sucesso vira 204; qualquer outro caso, <see cref="ParaFalha"/>.</summary>
    public static IResult ParaResultadoSemConteudo(this IApiResponse resposta) =>
        resposta.IsSuccessStatusCode ? Results.NoContent() : ParaFalha(resposta);

    /// <summary>
    /// Sucesso vira download do arquivo, com o nome e o tipo de conteúdo que o serviço mandou
    /// (padrão "relatorio.xlsx" e application/octet-stream); qualquer outro caso, <see cref="ParaFalha"/>.
    /// </summary>
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

    /// <summary>
    /// Resposta de erro do serviço: 4xx passa com o mesmo status e 5xx vira 502, os dois com o corpo original.
    /// Sem resposta (StatusCode nulo), relança a exceção capturada pelo Refit.
    /// </summary>
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
