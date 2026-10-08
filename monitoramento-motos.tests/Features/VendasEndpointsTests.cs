using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Concessionaria.Motos.Aplicacao.Features.Vendas.Contratos;
using Concessionaria.Motos.Dominio.Entidades;
using Concessionaria.Motos.tests.Common.Hospedagem;
using Concessionaria.Motos.tests.DataBuilder;
using NSubstitute;
using Shouldly;

namespace Concessionaria.Motos.tests.Features
{
    /// <summary>Endpoints de /vendas: registro (201/400/404/409) e listagem por período.</summary>
    public class VendasEndpointsTests : IAsyncLifetime
    {
        private ServidorTeste _servidor = null!;

        /// <summary>Sobe o servidor com os endpoints de Motos.</summary>
        public async Task InitializeAsync() => _servidor = await ServidorTeste.IniciarAsync();

        /// <summary>Derruba o servidor.</summary>
        public async Task DisposeAsync() => await _servidor.DisposeAsync();

        /// <summary>Configura uma moto existente e diz se a marcação como vendida dá certo.</summary>
        private Moto MotoExistente(bool marcaComoVendido)
        {
            var moto = MotoDataBuilder.Create().Build();
            _servidor.RepositorioMotos.ObterPorIdAsync(moto.Id, Arg.Any<CancellationToken>()).Returns(moto);
            _servidor.RepositorioMotos.TentarMarcarComoVendidoAsync(moto.Id, Arg.Any<CancellationToken>()).Returns(marcaComoVendido);
            return moto;
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 201 com o endereço da venda registrada")]
        public async Task PostVenda_WhenCarIsAvailable_ShouldReturnCreatedWithLocation()
        {
            // Arrange
            var moto = MotoExistente(marcaComoVendido: true);

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/vendas", new RequisicaoVenda(moto.Id, "Maria", "123.456.789-09", 99_000m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
            var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
            resposta.Headers.Location!.ToString().ShouldBe($"/vendas/{corpo.GetProperty("id").GetString()}");
            corpo.GetProperty("documentoCliente").GetString().ShouldBe("12345678909");
        }

        [Fact(DisplayName = "ERRO - Deve devolver 409 na segunda venda do mesma moto")]
        public async Task PostVenda_WhenCarIsAlreadySold_ShouldReturnConflict()
        {
            // Arrange
            var moto = MotoExistente(marcaComoVendido: false);

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/vendas", new RequisicaoVenda(moto.Id, "Maria", "12345678909", 1m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString().ShouldBe("Moto.JaVendido");
        }

        [Fact(DisplayName = "ERRO - Deve devolver 404 ao vender moto inexistente")]
        public async Task PostVenda_WhenCarDoesNotExist_ShouldReturnNotFound()
        {
            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/vendas", new RequisicaoVenda("66f0a1b2c3d4e5f6a7b8c9d0", "Maria", "12345678909", 1m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "ERRO - Deve devolver 400 com os erros por campo quando a venda é inválida")]
        public async Task PostVenda_WhenRequestIsInvalid_ShouldReturnValidationProblem()
        {
            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/vendas", new RequisicaoVenda("", "Maria", "123", -5m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var erros = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
            erros.EnumerateObject().Select(p => p.Name).ShouldBe(["VeiculoId", "DocumentoCliente", "Valor"], ignoreOrder: true);
        }

        [Fact(DisplayName = "SUCESSO - Deve listar as vendas do período informado na query string")]
        public async Task GetVendas_WhenPeriodIsInformed_ShouldQueryUtcBoundaries()
        {
            // Arrange
            _servidor.RepositorioVendas.ListarPorPeriodoAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
                .Returns(VendaDataBuilder.AsList(2));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/vendas?de=2026-09-01&ate=2026-09-30");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength().ShouldBe(2);
            await _servidor.RepositorioVendas.Received(1).ListarPorPeriodoAsync(
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                Arg.Any<CancellationToken>());
        }
    }
}
