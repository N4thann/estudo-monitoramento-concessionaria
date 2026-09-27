using Concessionaria.Motos.Comum;
using Concessionaria.Motos.Comum.Observabilidade;
using Concessionaria.Motos.Dados;
using Concessionaria.Motos.Entidades;
using Concessionaria.Motos.Integracoes.Relatorios;
using ErrorOr;
using Refit;

namespace Concessionaria.Motos.Servicos;

public record ArquivoRelatorio(byte[] Conteudo, string NomeArquivo);

// Lado de Motos da integração com a API de Relatórios: reúne os dados daqui e pede o cálculo/geração lá.
public class ServicoRelatorios(
    IRepositorioMotos repositorioMotos,
    IRepositorioVendas repositorioVendas,
    IRelatoriosApi relatoriosApi,
    ILogger<ServicoRelatorios> logger)
{
    private const string TipoVeiculo = "Motos";

    // Indicadores são complementares ao resumo: se Relatórios falhar, o resumo sai sem eles em vez de falhar inteiro.
    public async Task<IndicadoresVendas?> CalcularIndicadoresAsync(IReadOnlyList<Venda> vendas, CancellationToken ct)
    {
        using var resposta = await relatoriosApi.CalcularIndicadoresVendasAsync(
            new SolicitacaoIndicadoresVendas(vendas.Select(ParaVendaRelatorio).ToList()), ct);

        if (resposta.IsSuccessStatusCode && resposta.Content is not null)
            return resposta.Content;

        // StatusCode nulo = a requisição nem chegou a ter resposta (conexão recusada, timeout, circuito aberto).
        logger.LogWarning(resposta.Error,
            "Falha ao calcular indicadores no serviço de Relatórios (status {StatusCode}); resumo segue sem indicadores",
            (int?)resposta.StatusCode);

        Telemetria.FalhasRelatorios.Add(1, Telemetria.TagTipoVeiculo, new KeyValuePair<string, object?>("operacao", "indicadores"));
        return null;
    }

    public async Task<ErrorOr<ArquivoRelatorio>> GerarPlanilhaVendasAsync(FiltroPeriodo filtro, CancellationToken ct)
    {
        var ate = filtro.Ate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var de = filtro.De ?? ate.AddDays(-30);

        if (de > ate || ate.DayNumber - de.DayNumber > ErrosRelatorio.PeriodoMaximoDias)
            return ErrosRelatorio.PeriodoInvalido;

        var periodo = new FiltroPeriodo(de, ate);
        var vendas = await repositorioVendas.ListarPorPeriodoAsync(periodo.ObterInicioUtc(), periodo.ObterFimExclusivoUtc(), ct);

        using var resposta = await relatoriosApi.GerarPlanilhaVendasAsync(
            new SolicitacaoPlanilhaVendas(TipoVeiculo, de, ate, vendas.Select(ParaVendaRelatorio).ToList()), ct);

        return await LerArquivoAsync(resposta, "vendas", ct);
    }

    public async Task<ErrorOr<ArquivoRelatorio>> GerarPlanilhaEstoqueAsync(CancellationToken ct)
    {
        var motos = await repositorioMotos.ListarPorSituacaoAsync(SituacaoVeiculo.Disponivel, ct);
        var veiculos = motos
            .Select(m => new VeiculoRelatorio(m.Marca, m.Modelo, m.Ano, m.Cor, m.Cilindrada.ToString(), m.Preco, m.CriadoEm))
            .ToList();

        using var resposta = await relatoriosApi.GerarPlanilhaEstoqueAsync(
            new SolicitacaoPlanilhaEstoque(TipoVeiculo, "Cilindrada (cc)", veiculos), ct);

        return await LerArquivoAsync(resposta, "estoque", ct);
    }

    private async Task<ErrorOr<ArquivoRelatorio>> LerArquivoAsync(IApiResponse<Stream> resposta, string tipoRelatorio, CancellationToken ct)
    {
        if (!resposta.IsSuccessStatusCode || resposta.Content is null)
        {
            logger.LogError(resposta.Error,
                "Falha ao gerar a planilha de {TipoRelatorio} no serviço de Relatórios (status {StatusCode}): {Detalhe}",
                tipoRelatorio, (int?)resposta.StatusCode, (resposta.Error as ApiException)?.Content);
            Telemetria.FalhasRelatorios.Add(1, Telemetria.TagTipoVeiculo, new KeyValuePair<string, object?>("operacao", $"planilha_{tipoRelatorio}"));
            return ErrosRelatorio.FalhaNaGeracao;
        }

        using var memoria = new MemoryStream();
        await resposta.Content.CopyToAsync(memoria, ct);

        var disposicao = resposta.ContentHeaders?.ContentDisposition;
        var nomeArquivo = disposicao?.FileNameStar ?? disposicao?.FileName?.Trim('"') ?? $"relatorio-{tipoRelatorio}.xlsx";

        logger.LogInformation("Planilha de {TipoRelatorio} gerada ({Bytes} bytes)", tipoRelatorio, memoria.Length);
        return new ArquivoRelatorio(memoria.ToArray(), nomeArquivo);
    }

    private static VendaRelatorio ParaVendaRelatorio(Venda venda) =>
        new(venda.DescricaoVeiculo, venda.Marca, venda.NomeCliente, venda.DocumentoCliente, venda.Valor, venda.VendidaEm);
}
