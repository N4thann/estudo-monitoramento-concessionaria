using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Concessionaria.Relatorios.Aplicacao.Features.Indicadores.Contratos;
using Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Contratos;
using Concessionaria.Relatorios.Comum.Observabilidade;
using Concessionaria.Relatorios.tests.Common.Hospedagem;
using Concessionaria.Relatorios.tests.DataBuilder;
using Shouldly;

namespace Concessionaria.Relatorios.tests.Features
{
    /// <summary>
    /// Endpoints de Relatórios (indicadores e planilhas) em um servidor em memória com o pipeline de Id de operação
    /// e ProblemDetails (sem Serilog/OpenTelemetry do Program.cs, que exportariam para o Collector).
    /// </summary>
    public class RelatoriosEndpointsTests : IAsyncLifetime
    {
        private const string TipoConteudoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private ServidorTeste _servidor = null!;

        /// <summary>Sobe o servidor com os endpoints de Relatórios.</summary>
        public async Task InitializeAsync() => _servidor = await ServidorTeste.IniciarAsync();

        /// <summary>Derruba o servidor.</summary>
        public async Task DisposeAsync() => await _servidor.DisposeAsync();

        [Fact(DisplayName = "SUCESSO - Deve devolver os indicadores calculados das vendas enviadas")]
        public async Task PostIndicadoresVendas_WhenSalesAreSent_ShouldReturnIndicators()
        {
            // Arrange
            var solicitacao = new SolicitacaoIndicadoresVendas(
            [
                VendaRelatorioDataBuilder.Create().WithMarca("Fiat").WithValor(50_000m),
                VendaRelatorioDataBuilder.Create().WithMarca("Fiat").WithValor(70_000m)
            ]);

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/relatorios/indicadores/vendas", solicitacao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            var indicadores = await resposta.Content.ReadFromJsonAsync<IndicadoresVendas>();
            indicadores.ShouldBe(new IndicadoresVendas(2, 120_000m, 60_000m, 70_000m, "Fiat"));
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver indicadores zerados quando a lista de vendas não é enviada")]
        public async Task PostIndicadoresVendas_WhenSalesListIsNull_ShouldReturnZeroedIndicators()
        {
            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/relatorios/indicadores/vendas", new SolicitacaoIndicadoresVendas(null));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            var indicadores = await resposta.Content.ReadFromJsonAsync<IndicadoresVendas>();
            indicadores.ShouldBe(new IndicadoresVendas(0, 0m, 0m, 0m, null));
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver a planilha de vendas como arquivo .xlsx")]
        public async Task PostPlanilhaVendas_WhenRequestIsValid_ShouldReturnXlsxFile()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), VendaRelatorioDataBuilder.AsList(2));

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/relatorios/planilhas/vendas", solicitacao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            resposta.Content.Headers.ContentType!.MediaType.ShouldBe(TipoConteudoXlsx);
            resposta.Content.Headers.ContentDisposition!.FileName.ShouldBe("relatorio-vendas-carros_20260901_20260930.xlsx");
            using var pasta = new XLWorkbook(await resposta.Content.ReadAsStreamAsync());
            pasta.Worksheet("Vendas").LastRowUsed()!.RowNumber().ShouldBe(3);
        }

        [Fact(DisplayName = "ERRO - Deve devolver 400 com os erros por campo quando a solicitação de planilha de vendas é inválida")]
        public async Task PostPlanilhaVendas_WhenRequestIsInvalid_ShouldReturnValidationProblem()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("", new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 1), null);

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/relatorios/planilhas/vendas", solicitacao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problema = await LerProblemaAsync(resposta);
            problema.GetProperty("title").GetString().ShouldBe("Um ou mais campos são inválidos.");
            var erros = problema.GetProperty("errors");
            erros.TryGetProperty("TipoVeiculo", out _).ShouldBeTrue();
            erros.TryGetProperty("Periodo", out _).ShouldBeTrue();
            erros.TryGetProperty("Vendas", out _).ShouldBeTrue();
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver a planilha de estoque como arquivo .xlsx")]
        public async Task PostPlanilhaEstoque_WhenRequestIsValid_ShouldReturnXlsxFile()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Motos", "Cilindrada", VeiculoRelatorioDataBuilder.AsList(3));

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/relatorios/planilhas/estoque", solicitacao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            resposta.Content.Headers.ContentType!.MediaType.ShouldBe(TipoConteudoXlsx);
            resposta.Content.Headers.ContentDisposition!.FileName!.ShouldStartWith("relatorio-estoque-motos_");
            using var pasta = new XLWorkbook(await resposta.Content.ReadAsStreamAsync());
            pasta.Worksheet("Estoque").LastRowUsed()!.RowNumber().ShouldBe(4);
        }

        [Fact(DisplayName = "ERRO - Deve devolver 400 com os erros por campo quando a solicitação de planilha de estoque é inválida")]
        public async Task PostPlanilhaEstoque_WhenRequestIsInvalid_ShouldReturnValidationProblem()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Carros", "", null);

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/relatorios/planilhas/estoque", solicitacao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var erros = (await LerProblemaAsync(resposta)).GetProperty("errors");
            erros.TryGetProperty("TipoVeiculo", out _).ShouldBeFalse();
            erros.TryGetProperty("RotuloDetalhe", out _).ShouldBeTrue();
            erros.TryGetProperty("Veiculos", out _).ShouldBeTrue();
        }

        [Fact(DisplayName = "ERRO - Deve devolver o Id de operação no ProblemDetails e no cabeçalho da resposta de erro")]
        public async Task PostPlanilhaVendas_WhenRequestIsInvalid_ShouldCorrelateErrorWithOperationId()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), []);
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/relatorios/planilhas/vendas") { Content = JsonContent.Create(solicitacao) };
            requisicao.Headers.Add(ContextoOperacao.Cabecalho, "id-vindo-do-carros");

            // Act
            var resposta = await _servidor.Cliente.SendAsync(requisicao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            resposta.Headers.GetValues(ContextoOperacao.Cabecalho).ShouldBe(["id-vindo-do-carros"]);
            (await LerProblemaAsync(resposta)).GetProperty("idOperacao").GetString().ShouldBe("id-vindo-do-carros");
        }

        private static async Task<JsonElement> LerProblemaAsync(HttpResponseMessage resposta)
        {
            resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
            return await resposta.Content.ReadFromJsonAsync<JsonElement>();
        }
    }
}
