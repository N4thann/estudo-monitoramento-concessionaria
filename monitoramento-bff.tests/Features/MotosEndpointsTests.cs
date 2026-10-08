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
    /// <summary>
    /// Endpoints de /api/motos: mesmas regras de /api/carros (cobertas em <see cref="CarrosEndpointsTests"/>);
    /// aqui o foco é cada rota chegar à API de Motos, e não à de Carros.
    /// </summary>
    public class MotosEndpointsTests : IAsyncLifetime
    {
        private ServidorTeste _servidor = null!;

        /// <summary>Sobe o servidor com os endpoints do BFF.</summary>
        public async Task InitializeAsync() => _servidor = await ServidorTeste.IniciarAsync();

        /// <summary>Derruba o servidor.</summary>
        public async Task DisposeAsync() => await _servidor.DisposeAsync();

        [Fact(DisplayName = "SUCESSO - Deve listar as motos pela API de Motos, com a cilindrada")]
        public async Task GetMotos_WhenCalled_ShouldUseMotorcyclesApi()
        {
            // Arrange
            var moto = MotoDataBuilder.Create().WithCilindrada(689).Build();
            _servidor.MotosApi.ListarAsync(Arg.Any<FiltroVeiculos>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.Sucesso(new ResultadoPaginado<Moto>([moto], 1, 1, 10)));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/motos");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("itens")[0].GetProperty("cilindrada").GetInt32().ShouldBe(689);
            await _servidor.CarrosApi.DidNotReceiveWithAnyArgs().ListarAsync(default!, default);
        }

        [Fact(DisplayName = "SUCESSO - Deve buscar a moto pelo Id na API de Motos")]
        public async Task GetMotoPorId_WhenMotorcycleExists_ShouldReturnIt()
        {
            // Arrange
            var moto = MotoDataBuilder.Create().Build();
            _servidor.MotosApi.ObterPorIdAsync(moto.Id, Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(moto));

            // Act
            var resposta = await _servidor.Cliente.GetAsync($"/api/motos/{moto.Id}");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString().ShouldBe(moto.Id);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 201 com o endereço da moto no BFF")]
        public async Task PostMoto_WhenMotorcyclesServiceCreates_ShouldReturnCreatedWithBffLocation()
        {
            // Arrange
            var moto = MotoDataBuilder.Create().Build();
            _servidor.MotosApi.CriarAsync(Arg.Any<RequisicaoMoto>(), Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(moto));

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/api/motos", new RequisicaoMoto(moto.Marca, moto.Modelo, moto.Ano, moto.Cor, moto.Cilindrada, moto.Preco));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
            resposta.Headers.Location!.ToString().ShouldBe($"/api/motos/{moto.Id}");
        }

        [Fact(DisplayName = "SUCESSO - Deve repassar a atualização para a API de Motos")]
        public async Task PutMoto_WhenMotorcycleIsUpdated_ShouldForwardIdAndBody()
        {
            // Arrange
            var moto = MotoDataBuilder.Create().Build();
            var requisicao = new RequisicaoMoto("Yamaha", "MT-07", 2023, "Cinza", 689, 52_900m);
            _servidor.MotosApi.AtualizarAsync(moto.Id, requisicao, Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(moto));

            // Act
            var resposta = await _servidor.Cliente.PutAsJsonAsync($"/api/motos/{moto.Id}", requisicao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 204 quando a exclusão da moto dá certo")]
        public async Task DeleteMoto_WhenMotorcycleIsDeleted_ShouldReturnNoContent()
        {
            // Arrange
            _servidor.MotosApi.ExcluirAsync("abc", Arg.Any<CancellationToken>()).Returns(RespostasApi.SemConteudo());

            // Act
            var resposta = await _servidor.Cliente.DeleteAsync("/api/motos/abc");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [Fact(DisplayName = "SUCESSO - Deve registrar a venda na API de Motos")]
        public async Task PostVenda_WhenSaleIsRegistered_ShouldUseMotorcyclesApi()
        {
            // Arrange
            var venda = VendaDataBuilder.Create().Build();
            _servidor.MotosApi.RegistrarVendaAsync(Arg.Any<RequisicaoVenda>(), Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(venda));

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/api/motos/vendas", new RequisicaoVenda(venda.VeiculoId, "João", "12345678000190", 30_000m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
            await _servidor.CarrosApi.DidNotReceiveWithAnyArgs().RegistrarVendaAsync(default!, default);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver as planilhas de vendas e de estoque geradas pela API de Motos")]
        public async Task GetPlanilhas_WhenMotorcyclesServiceSendsFiles_ShouldReturnThem()
        {
            // Arrange
            _servidor.MotosApi.GerarPlanilhaVendasAsync(Arg.Any<FiltroPeriodo>(), Arg.Any<CancellationToken>()).Returns(RespostasApi.Arquivo([1]));
            _servidor.MotosApi.GerarPlanilhaEstoqueAsync(Arg.Any<CancellationToken>()).Returns(RespostasApi.Arquivo([2]));

            // Act
            var vendas = await _servidor.Cliente.GetAsync("/api/motos/relatorios/vendas");
            var estoque = await _servidor.Cliente.GetAsync("/api/motos/relatorios/estoque");

            // Assert
            (await vendas.Content.ReadAsByteArrayAsync()).ShouldBe(new byte[] { 1 });
            (await estoque.Content.ReadAsByteArrayAsync()).ShouldBe(new byte[] { 2 });
        }
    }
}
