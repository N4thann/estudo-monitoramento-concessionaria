using System.Diagnostics;
using ClosedXML.Excel;
using Concessionaria.Relatorios.Aplicacao.Features.Indicadores;
using Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Contratos;
using Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Excel;
using Concessionaria.Relatorios.Comum.Observabilidade;

namespace Concessionaria.Relatorios.Aplicacao.Features.Planilhas;

/// <summary>Geração das planilhas .xlsx de vendas e de estoque, com span e métricas da montagem.</summary>
public class GeradorPlanilhas(CalculadoraIndicadores calculadora, ILogger<GeradorPlanilhas> logger)
{
    private static readonly TimeZoneInfo FusoBrasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    /// <summary>Gera a planilha de vendas: aba "Resumo" com os indicadores e aba "Vendas" com uma linha por venda.</summary>
    public ArquivoPlanilha GerarVendas(SolicitacaoPlanilhaVendas solicitacao) =>
        Medir("vendas", solicitacao.TipoVeiculo, solicitacao.Vendas?.Count ?? 0, () => MontarVendas(solicitacao));

    /// <summary>Gera a planilha de estoque: aba "Resumo" com os totais e aba "Estoque" com uma linha por veículo.</summary>
    public ArquivoPlanilha GerarEstoque(SolicitacaoPlanilhaEstoque solicitacao) =>
        Medir("estoque", solicitacao.TipoVeiculo, solicitacao.Veiculos?.Count ?? 0, () => MontarEstoque(solicitacao));

    /// <summary>Span e métricas da montagem do arquivo: no trace, mostra quanto da requisição foi gasto gerando a planilha.</summary>
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

    /// <summary>Monta as abas da planilha de vendas e o nome do arquivo com o período.</summary>
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

    /// <summary>Monta as abas da planilha de estoque e o nome do arquivo com a data e hora da geração.</summary>
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

    /// <summary>Converte uma data UTC para o horário de Brasília, que é o mostrado na planilha.</summary>
    private static DateTime ParaHorarioBrasilia(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), FusoBrasilia);
}
