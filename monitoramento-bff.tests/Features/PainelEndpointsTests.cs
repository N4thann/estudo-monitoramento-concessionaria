using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;
using Concessionaria.Bff.tests.Common.Hospedagem;
using Concessionaria.Bff.tests.Common.Observabilidade;
using Concessionaria.Bff.tests.Common.Refit;
using Concessionaria.Bff.tests.DataBuilder;
using NSubstitute;
using Shouldly;

namespace Concessionaria.Bff.tests.Features
{
    /// <summary>
    /// GET /api/painel: formato JSON consumido pelo front e degradação sem indicadores. Fica na coleção de telemetria
    /// porque o painel degradado incrementa o contador estático que outros testes medem.
    /// </summary>
    [Collection(ColecaoTelemetria.Nome)]
    public class PainelEndpointsTests : IAsyncLifetime
    {
        private ServidorTeste _servidor = null!;

        /// <summary>Sobe o servidor com os endpoints do BFF.</summary>
        public async Task InitializeAsync() => _servidor = await ServidorTeste.IniciarAsync();

        /// <summary>Derruba o servidor.</summary>
        public async Task DisposeAsync() => await _servidor.DisposeAsync();

        [Fact(DisplayName = "SUCESSO - Deve devolver o resumo de carros, de motos e o total com vendas e receita")]
        public async Task GetPainel_WhenBothServicesRespond_ShouldReturnPanelJson()
        {
            // Arrange
            _servidor.CarrosApi.ObterResumoAsync(Arg.Any<CancellationToken>())
                .Returns(RespostasApi.Sucesso(ResumoDataBuilder.Create().WithEstoque(4, 1, 400_000m).WithIndicadores(1, 100_000m).Build()));
            _servidor.MotosApi.ObterResumoAsync(Arg.Any<CancellationToken>())
                .Returns(RespostasApi.Sucesso(ResumoDataBuilder.Create().WithEstoque(6, 2, 120_000m).WithIndicadores(2, 40_000m).Build()));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/painel");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
            corpo.GetProperty("carros").GetProperty("disponiveis").GetInt64().ShouldBe(4);
            corpo.GetProperty("motos").GetProperty("indicadores").GetProperty("quantidadeVendas").GetInt32().ShouldBe(2);
            var total = corpo.GetProperty("total");
            total.GetProperty("totalVeiculos").GetInt64().ShouldBe(13);
            total.GetProperty("quantidadeVendas").GetInt32().ShouldBe(3);
            total.GetProperty("receita").GetDecimal().ShouldBe(140_000m);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver indicadores nulos quando Relatórios está fora, sem falhar o painel")]
        public async Task GetPainel_WhenIndicatorsAreMissing_ShouldReturnNullSales()
        {
            // Arrange
            _servidor.CarrosApi.ObterResumoAsync(Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(ResumoDataBuilder.Create().SemIndicadores().Build()));
            _servidor.MotosApi.ObterResumoAsync(Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(ResumoDataBuilder.Create().SemIndicadores().Build()));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/painel");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
            corpo.GetProperty("carros").GetProperty("indicadores").ValueKind.ShouldBe(JsonValueKind.Null);
            corpo.GetProperty("total").GetProperty("quantidadeVendas").ValueKind.ShouldBe(JsonValueKind.Null);
            corpo.GetProperty("total").GetProperty("receita").ValueKind.ShouldBe(JsonValueKind.Null);
        }

        [Fact(DisplayName = "ERRO - Deve devolver 502 com o Id de operação quando Motos está inalcançável")]
        public async Task GetPainel_WhenMotorcyclesServiceIsUnreachable_ShouldReturnBadGateway()
        {
            // Arrange
            _servidor.CarrosApi.ObterResumoAsync(Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(ResumoDataBuilder.Create().Build()));
            _servidor.MotosApi.ObterResumoAsync(Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<Resumo>(new HttpRequestException("Conexão recusada")));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/painel");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
            (await resposta.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("idOperacao", out _).ShouldBeTrue();
        }
    }
}
