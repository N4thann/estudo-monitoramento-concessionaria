using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Concessionaria.Carros.Dominio.Entidades;
using Concessionaria.Carros.Dominio.Enums;
using Concessionaria.Carros.Infraestrutura.Integracoes.Relatorios.Contratos;
using Concessionaria.Carros.tests.Common.Hospedagem;
using Concessionaria.Carros.tests.Common.Refit;
using NSubstitute;
using Shouldly;

namespace Concessionaria.Carros.tests.Features
{
    /// <summary>Endpoints de /relatorios em Carros: arquivo .xlsx repassado, período inválido e Relatórios fora (502).</summary>
    public class RelatoriosEndpointsTests : IAsyncLifetime
    {
        private const string TipoConteudoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private static readonly byte[] Planilha = [0x50, 0x4B, 0x03, 0x04];

        private ServidorTeste _servidor = null!;

        /// <summary>Sobe o servidor com os endpoints de Carros; vendas e estoque começam vazios.</summary>
        public async Task InitializeAsync()
        {
            _servidor = await ServidorTeste.IniciarAsync();
            _servidor.RepositorioVendas.ListarPorPeriodoAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns(new List<Venda>());
            _servidor.RepositorioCarros.ListarPorSituacaoAsync(Arg.Any<SituacaoVeiculo>(), Arg.Any<CancellationToken>()).Returns(new List<Carro>());
        }

        /// <summary>Derruba o servidor.</summary>
        public async Task DisposeAsync() => await _servidor.DisposeAsync();

        [Fact(DisplayName = "SUCESSO - Deve devolver a planilha de vendas como .xlsx com o nome gerado por Relatórios")]
        public async Task GetPlanilhaVendas_WhenReportsServiceResponds_ShouldReturnXlsxFile()
        {
            // Arrange
            _servidor.RelatoriosApi.GerarPlanilhaVendasAsync(Arg.Any<SolicitacaoPlanilhaVendas>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.Arquivo(Planilha, new ContentDispositionHeaderValue("attachment") { FileName = "relatorio-vendas-carros_20260901_20260930.xlsx" }));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/relatorios/vendas?de=2026-09-01&ate=2026-09-30");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            resposta.Content.Headers.ContentType!.MediaType.ShouldBe(TipoConteudoXlsx);
            resposta.Content.Headers.ContentDisposition!.FileName.ShouldBe("relatorio-vendas-carros_20260901_20260930.xlsx");
            (await resposta.Content.ReadAsByteArrayAsync()).ShouldBe(Planilha);
        }

        [Fact(DisplayName = "ERRO - Deve devolver 400 com erro no campo Periodo quando o intervalo passa de 366 dias")]
        public async Task GetPlanilhaVendas_WhenPeriodIsTooLong_ShouldReturnValidationProblem()
        {
            // Act
            var resposta = await _servidor.Cliente.GetAsync("/relatorios/vendas?de=2025-01-01&ate=2026-09-30");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var erros = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
            erros.TryGetProperty("Periodo", out _).ShouldBeTrue();
        }

        [Fact(DisplayName = "ERRO - Deve devolver 502 com o código da falha quando Relatórios está fora")]
        public async Task GetPlanilhaEstoque_WhenReportsServiceIsDown_ShouldReturnBadGateway()
        {
            // Arrange
            _servidor.RelatoriosApi.GerarPlanilhaEstoqueAsync(Arg.Any<SolicitacaoPlanilhaEstoque>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<Stream>(new HttpRequestException("Conexão recusada")));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/relatorios/estoque");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
            var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>();
            problema.GetProperty("codigo").GetString().ShouldBe("Relatorios.FalhaNaGeracao");
            problema.GetProperty("title").GetString().ShouldBe("Não foi possível gerar a planilha no serviço de relatórios.");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver a planilha de estoque como .xlsx")]
        public async Task GetPlanilhaEstoque_WhenReportsServiceResponds_ShouldReturnXlsxFile()
        {
            // Arrange
            _servidor.RelatoriosApi.GerarPlanilhaEstoqueAsync(Arg.Any<SolicitacaoPlanilhaEstoque>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.Arquivo(Planilha));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/relatorios/estoque");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            resposta.Content.Headers.ContentType!.MediaType.ShouldBe(TipoConteudoXlsx);
            resposta.Content.Headers.ContentDisposition!.FileName.ShouldBe("relatorio-estoque.xlsx");
        }
    }
}
