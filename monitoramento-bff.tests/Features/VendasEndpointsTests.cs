using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;
using Concessionaria.Bff.tests.Common.Hospedagem;
using Concessionaria.Bff.tests.Common.Refit;
using Concessionaria.Bff.tests.DataBuilder;
using NSubstitute;
using Shouldly;

namespace Concessionaria.Bff.tests.Features
{
    /// <summary>GET /api/vendas: binding do período e formato JSON da lista unificada.</summary>
    public class VendasEndpointsTests : IAsyncLifetime
    {
        private ServidorTeste _servidor = null!;

        /// <summary>Sobe o servidor com os endpoints do BFF.</summary>
        public async Task InitializeAsync() => _servidor = await ServidorTeste.IniciarAsync();

        /// <summary>Derruba o servidor.</summary>
        public async Task DisposeAsync() => await _servidor.DisposeAsync();

        [Fact(DisplayName = "SUCESSO - Deve repassar o período da query string e devolver o tipo de veículo como texto")]
        public async Task GetVendas_WhenPeriodIsInformed_ShouldForwardItAndSerializeTypeAsString()
        {
            // Arrange
            var periodo = new FiltroPeriodo(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));
            _servidor.CarrosApi.ListarVendasAsync(periodo, Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(VendaDataBuilder.AsList(1)));
            _servidor.MotosApi.ListarVendasAsync(periodo, Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(new List<Venda>()));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/vendas?de=2026-09-01&ate=2026-09-30");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            var vendas = await resposta.Content.ReadFromJsonAsync<JsonElement>();
            vendas.GetArrayLength().ShouldBe(1);
            vendas[0].GetProperty("tipoVeiculo").GetString().ShouldBe("Carro");
        }

        [Fact(DisplayName = "SUCESSO - Deve aceitar a listagem sem período, repassando datas nulas")]
        public async Task GetVendas_WhenPeriodIsMissing_ShouldForwardNullDates()
        {
            // Arrange
            _servidor.CarrosApi.ListarVendasAsync(Arg.Any<FiltroPeriodo>(), Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(new List<Venda>()));
            _servidor.MotosApi.ListarVendasAsync(Arg.Any<FiltroPeriodo>(), Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(new List<Venda>()));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/vendas");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            await _servidor.MotosApi.Received(1).ListarVendasAsync(new FiltroPeriodo(null, null), Arg.Any<CancellationToken>());
        }
    }
}
