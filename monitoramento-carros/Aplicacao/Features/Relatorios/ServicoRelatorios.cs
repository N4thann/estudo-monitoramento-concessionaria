using Concessionaria.Carros.Aplicacao.Comum;
using Concessionaria.Carros.Aplicacao.Features.Relatorios.Contratos;
using Concessionaria.Carros.Comum.Observabilidade;
using Concessionaria.Carros.Dominio.Entidades;
using Concessionaria.Carros.Dominio.Enums;
using Concessionaria.Carros.Dominio.Erros;
using Concessionaria.Carros.Dominio.Interfaces;
using Concessionaria.Carros.Infraestrutura.Integracoes.Relatorios;
using Concessionaria.Carros.Infraestrutura.Integracoes.Relatorios.Contratos;
using ErrorOr;
using Refit;

namespace Concessionaria.Carros.Aplicacao.Features.Relatorios;

/// <summary>Lado de Carros da integração com a API de Relatórios: reúne os dados daqui e pede o cálculo ou a planilha lá.</summary>
public class ServicoRelatorios(
    IRepositorioCarros repositorioCarros,
    IRepositorioVendas repositorioVendas,
    IRelatoriosApi relatoriosApi,
    ILogger<ServicoRelatorios> logger)
{
    private const string TipoVeiculo = "Carros";

    /// <summary>
    /// Pede a Relatórios os indicadores das vendas. Os indicadores são complementares ao resumo: se Relatórios
    /// falhar, devolve nulo (e conta a falha) em vez de propagar o erro.
    /// </summary>
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

    /// <summary>Gera a planilha de vendas do período: padrão últimos 30 dias, máximo <see cref="ErrosRelatorio.PeriodoMaximoDias"/>.</summary>
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

    /// <summary>Gera a planilha dos carros disponíveis.</summary>
    public async Task<ErrorOr<ArquivoRelatorio>> GerarPlanilhaEstoqueAsync(CancellationToken ct)
    {
        var carros = await repositorioCarros.ListarPorSituacaoAsync(SituacaoVeiculo.Disponivel, ct);
        var veiculos = carros
            .Select(c => new VeiculoRelatorio(c.Marca, c.Modelo, c.Ano, c.Cor, c.Portas.ToString(), c.Preco, c.CriadoEm))
            .ToList();

        using var resposta = await relatoriosApi.GerarPlanilhaEstoqueAsync(
            new SolicitacaoPlanilhaEstoque(TipoVeiculo, "Portas", veiculos), ct);

        return await LerArquivoAsync(resposta, "estoque", ct);
    }

    /// <summary>
    /// Lê o arquivo devolvido por Relatórios. Erro HTTP ou falta de resposta vira <see cref="ErrosRelatorio.FalhaNaGeracao"/>
    /// e é contado na métrica de falhas de integração.
    /// </summary>
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

    /// <summary>Converte a venda no formato que Relatórios espera.</summary>
    private static VendaRelatorio ParaVendaRelatorio(Venda venda) =>
        new(venda.DescricaoVeiculo, venda.Marca, venda.NomeCliente, venda.DocumentoCliente, venda.Valor, venda.VendidaEm);
}
