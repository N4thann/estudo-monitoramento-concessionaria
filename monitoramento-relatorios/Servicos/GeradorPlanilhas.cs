using System.Diagnostics;
using ClosedXML.Excel;
using Concessionaria.Relatorios.Comum.Observabilidade;
using Concessionaria.Relatorios.Contratos;

namespace Concessionaria.Relatorios.Servicos;

public record ArquivoPlanilha(byte[] Conteudo, string NomeArquivo);

public class GeradorPlanilhas(CalculadoraIndicadores calculadora, ILogger<GeradorPlanilhas> logger)
{
    private static readonly TimeZoneInfo FusoBrasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public ArquivoPlanilha GerarVendas(SolicitacaoPlanilhaVendas solicitacao) =>
        Medir("vendas", solicitacao.TipoVeiculo, solicitacao.Vendas?.Count ?? 0, () => MontarVendas(solicitacao));

    public ArquivoPlanilha GerarEstoque(SolicitacaoPlanilhaEstoque solicitacao) =>
        Medir("estoque", solicitacao.TipoVeiculo, solicitacao.Veiculos?.Count ?? 0, () => MontarEstoque(solicitacao));

    // Span e métricas da montagem do arquivo: no trace, mostra quanto da requisição foi gasto gerando a planilha.
    private static ArquivoPlanilha Medir(string tipoRelatorio, string tipoVeiculo, int linhas, Func<ArquivoPlanilha> montar)
    {
        using var atividade = Telemetria.Fonte.StartActivity($"Gerar planilha de {tipoRelatorio}");
        atividade?.SetTag("concessionaria.tipo_veiculo", tipoVeiculo);
        atividade?.SetTag("concessionaria.linhas", linhas);

        var inicio = Stopwatch.GetTimestamp();
        var arquivo = montar();

        var tags = new TagList { { "tipo_relatorio", tipoRelatorio }, { "tipo_veiculo", tipoVeiculo.ToLowerInvariant() } };
        Telemetria.DuracaoGeracaoPlanilha.Record(Stopwatch.GetElapsedTime(inicio).TotalSeconds, tags);
        Telemetria.LinhasPorPlanilha.Record(linhas, tags);
        Telemetria.PlanilhasGeradas.Add(1, tags);
        atividade?.SetTag("concessionaria.bytes", arquivo.Conteudo.Length);

        return arquivo;
    }

    private ArquivoPlanilha MontarVendas(SolicitacaoPlanilhaVendas solicitacao)
    {
        var vendas = solicitacao.Vendas ?? [];
        var indicadores = calculadora.CalcularVendas(vendas);

        using var pasta = new XLWorkbook();

        pasta.AdicionarAba("Resumo",
            [new("Indicador"), new("Valor")],
            [
                ["Tipo de veículo", solicitacao.TipoVeiculo],
                ["Período", $"{solicitacao.De:dd/MM/yyyy} a {solicitacao.Ate:dd/MM/yyyy}"],
                ["Quantidade de vendas", indicadores.QuantidadeVendas],
                ["Receita (R$)", indicadores.Receita],
                ["Ticket médio (R$)", indicadores.TicketMedio],
                ["Maior venda (R$)", indicadores.MaiorVenda],
                ["Marca mais vendida", indicadores.MarcaMaisVendida ?? "-"]
            ]);

        pasta.AdicionarAba("Vendas",
            [
                new("Data", ExtensoesPlanilha.FormatoDataHora),
                new("Veículo"),
                new("Marca"),
                new("Cliente"),
                new("Documento"),
                new("Valor", ExtensoesPlanilha.FormatoMoeda)
            ],
            vendas.Select(v => new object?[] { ParaHorarioBrasilia(v.VendidaEm), v.Veiculo, v.Marca, v.Cliente, v.Documento, v.Valor }));

        logger.LogInformation(
            "Planilha de vendas de {TipoVeiculo} gerada para {De} a {Ate} com {QuantidadeVendas} vendas",
            solicitacao.TipoVeiculo, solicitacao.De, solicitacao.Ate, vendas.Count);

        var nome = $"relatorio-vendas-{solicitacao.TipoVeiculo.ToLowerInvariant()}_{solicitacao.De:yyyyMMdd}_{solicitacao.Ate:yyyyMMdd}.xlsx";
        return new ArquivoPlanilha(pasta.ParaBytes(), nome);
    }

    private ArquivoPlanilha MontarEstoque(SolicitacaoPlanilhaEstoque solicitacao)
    {
        var veiculos = solicitacao.Veiculos ?? [];
        var valorEmEstoque = veiculos.Sum(v => v.Preco);

        using var pasta = new XLWorkbook();

        pasta.AdicionarAba("Resumo",
            [new("Indicador"), new("Valor")],
            [
                ["Tipo de veículo", solicitacao.TipoVeiculo],
                ["Veículos disponíveis", veiculos.Count],
                ["Valor em estoque (R$)", valorEmEstoque],
                ["Preço médio (R$)", veiculos.Count == 0 ? 0m : Math.Round(valorEmEstoque / veiculos.Count, 2)]
            ]);

        pasta.AdicionarAba("Estoque",
            [
                new("Marca"),
                new("Modelo"),
                new("Ano"),
                new("Cor"),
                new(solicitacao.RotuloDetalhe),
                new("Preço", ExtensoesPlanilha.FormatoMoeda),
                new("Cadastrado em", ExtensoesPlanilha.FormatoDataHora)
            ],
            veiculos.Select(v => new object?[]
            {
                v.Marca, v.Modelo, v.Ano, v.Cor,
                int.TryParse(v.Detalhe, out var numero) ? numero : v.Detalhe,
                v.Preco, ParaHorarioBrasilia(v.CadastradoEm)
            }));

        logger.LogInformation("Planilha de estoque de {TipoVeiculo} gerada com {QuantidadeVeiculos} veículos", solicitacao.TipoVeiculo, veiculos.Count);

        var nome = $"relatorio-estoque-{solicitacao.TipoVeiculo.ToLowerInvariant()}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return new ArquivoPlanilha(pasta.ParaBytes(), nome);
    }

    private static DateTime ParaHorarioBrasilia(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), FusoBrasilia);
}
