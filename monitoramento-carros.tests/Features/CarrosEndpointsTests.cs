using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Concessionaria.Carros.Aplicacao.Features.Carros.Contratos;
using Concessionaria.Carros.Comum.Observabilidade;
using Concessionaria.Carros.Dominio.Entidades;
using Concessionaria.Carros.Dominio.Enums;
using Concessionaria.Carros.Dominio.Modelos;
using Concessionaria.Carros.Infraestrutura.Integracoes.Relatorios.Contratos;
using Concessionaria.Carros.tests.Common.Hospedagem;
using Concessionaria.Carros.tests.Common.Refit;
using Concessionaria.Carros.tests.DataBuilder;
using MongoDB.Bson;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Concessionaria.Carros.tests.Features
{
    /// <summary>Endpoints de /carros: binding da query, status HTTP de cada regra e ProblemDetails correlacionado.</summary>
    public class CarrosEndpointsTests : IAsyncLifetime
    {
        private ServidorTeste _servidor = null!;

        /// <summary>Sobe o servidor com os endpoints de Carros.</summary>
        public async Task InitializeAsync() => _servidor = await ServidorTeste.IniciarAsync();

        /// <summary>Derruba o servidor.</summary>
        public async Task DisposeAsync() => await _servidor.DisposeAsync();

        /// <summary>Lê o corpo como ProblemDetails, conferindo o tipo de conteúdo.</summary>
        private static async Task<JsonElement> LerProblemaAsync(HttpResponseMessage resposta)
        {
            resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
            return await resposta.Content.ReadFromJsonAsync<JsonElement>();
        }

        /// <summary>Configura o repositório para devolver o carro pelo Id dele.</summary>
        private Carro Existente(Carro carro)
        {
            _servidor.RepositorioCarros.ObterPorIdAsync(carro.Id, Arg.Any<CancellationToken>()).Returns(carro);
            return carro;
        }

        [Fact(DisplayName = "SUCESSO - Deve repassar paginação, situação e busca da query string e devolver a situação como texto")]
        public async Task GetCarros_WhenQueryHasFilters_ShouldBindThemAndSerializeEnumAsString()
        {
            // Arrange
            var carro = CarroDataBuilder.Create().WithSituacao(SituacaoVeiculo.Vendido).Build();
            _servidor.RepositorioCarros.ListarPaginadoAsync(2, 5, SituacaoVeiculo.Vendido, "corolla", Arg.Any<CancellationToken>())
                .Returns((new List<Carro> { carro }, 6L));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/carros?pagina=2&tamanhoPagina=5&situacao=Vendido&busca=corolla");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
            corpo.GetProperty("total").GetInt64().ShouldBe(6);
            corpo.GetProperty("pagina").GetInt32().ShouldBe(2);
            corpo.GetProperty("itens")[0].GetProperty("situacao").GetString().ShouldBe("Vendido");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 200 com o carro encontrado")]
        public async Task GetCarroPorId_WhenCarExists_ShouldReturnOk()
        {
            // Arrange
            var carro = Existente(CarroDataBuilder.Create().WithModelo("Corolla"));

            // Act
            var resposta = await _servidor.Cliente.GetAsync($"/carros/{carro.Id}");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("modelo").GetString().ShouldBe("Corolla");
        }

        [Fact(DisplayName = "ERRO - Deve devolver 404 com o código do erro e o Id de operação no ProblemDetails")]
        public async Task GetCarroPorId_WhenCarDoesNotExist_ShouldReturnNotFoundProblem()
        {
            // Arrange
            using var requisicao = new HttpRequestMessage(HttpMethod.Get, $"/carros/{ObjectId.GenerateNewId()}");
            requisicao.Headers.Add(ContextoOperacao.Cabecalho, "id-vindo-do-bff");

            // Act
            var resposta = await _servidor.Cliente.SendAsync(requisicao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            resposta.Headers.GetValues(ContextoOperacao.Cabecalho).ShouldBe(["id-vindo-do-bff"]);
            var problema = await LerProblemaAsync(resposta);
            problema.GetProperty("title").GetString().ShouldBe("Carro não encontrado.");
            problema.GetProperty("codigo").GetString().ShouldBe("Carro.NaoEncontrado");
            problema.GetProperty("idOperacao").GetString().ShouldBe("id-vindo-do-bff");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 201 com o endereço do carro cadastrado")]
        public async Task PostCarro_WhenRequestIsValid_ShouldReturnCreatedWithLocation()
        {
            // Arrange
            var requisicao = new RequisicaoCarro("Toyota", "Corolla", 2024, "Prata", 4, 150_000m);

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/carros", requisicao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
            var id = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
            resposta.Headers.Location!.ToString().ShouldBe($"/carros/{id}");
            await _servidor.RepositorioCarros.Received(1).InserirAsync(Arg.Is<Carro>(c => c.Id == id), Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve devolver 400 com os erros por campo quando o cadastro é inválido")]
        public async Task PostCarro_WhenRequestIsInvalid_ShouldReturnValidationProblem()
        {
            // Arrange
            var requisicao = new RequisicaoCarro("", "Corolla", 2024, "Prata", 7, 0m);

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/carros", requisicao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problema = await LerProblemaAsync(resposta);
            problema.GetProperty("title").GetString().ShouldBe("Um ou mais campos são inválidos.");
            var erros = problema.GetProperty("errors");
            erros.EnumerateObject().Select(p => p.Name).ShouldBe(["Marca", "Portas", "Preco"], ignoreOrder: true);
            problema.TryGetProperty("idOperacao", out _).ShouldBeTrue();
        }

        [Fact(DisplayName = "ERRO - Deve devolver 409 ao editar carro já vendido")]
        public async Task PutCarro_WhenCarIsSold_ShouldReturnConflict()
        {
            // Arrange
            var carro = Existente(CarroDataBuilder.Create().WithSituacao(SituacaoVeiculo.Vendido));

            // Act
            var resposta = await _servidor.Cliente.PutAsJsonAsync($"/carros/{carro.Id}", new RequisicaoCarro("Honda", "Civic", 2025, "Preto", 4, 1m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await LerProblemaAsync(resposta)).GetProperty("codigo").GetString().ShouldBe("Carro.JaVendido");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 200 com o carro editado")]
        public async Task PutCarro_WhenCarIsAvailable_ShouldReturnOk()
        {
            // Arrange
            var carro = Existente(CarroDataBuilder.Create());

            // Act
            var resposta = await _servidor.Cliente.PutAsJsonAsync($"/carros/{carro.Id}", new RequisicaoCarro("Honda", "Civic", 2025, "Preto", 4, 1m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("marca").GetString().ShouldBe("Honda");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 204 ao excluir carro disponível")]
        public async Task DeleteCarro_WhenCarIsAvailable_ShouldReturnNoContent()
        {
            // Arrange
            var carro = Existente(CarroDataBuilder.Create());

            // Act
            var resposta = await _servidor.Cliente.DeleteAsync($"/carros/{carro.Id}");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            await _servidor.RepositorioCarros.Received(1).ExcluirAsync(carro.Id, Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve devolver 409 ao excluir carro já vendido")]
        public async Task DeleteCarro_WhenCarIsSold_ShouldReturnConflict()
        {
            // Arrange
            var carro = Existente(CarroDataBuilder.Create().WithSituacao(SituacaoVeiculo.Vendido));

            // Act
            var resposta = await _servidor.Cliente.DeleteAsync($"/carros/{carro.Id}");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 200 no resumo com indicadores nulos quando Relatórios está fora")]
        public async Task GetResumo_WhenReportsServiceIsDown_ShouldReturnOkWithNullIndicators()
        {
            // Arrange
            _servidor.RepositorioCarros.ObterTotaisPorSituacaoAsync(Arg.Any<CancellationToken>())
                .Returns(new Dictionary<SituacaoVeiculo, TotaisEstoque> { [SituacaoVeiculo.Disponivel] = new(2, 100_000m) });
            _servidor.RepositorioVendas.ListarPorPeriodoAsync(null, null, Arg.Any<CancellationToken>()).Returns(new List<Venda>());
            _servidor.RelatoriosApi.CalcularIndicadoresVendasAsync(Arg.Any<SolicitacaoIndicadoresVendas>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<IndicadoresVendas>(new HttpRequestException("Conexão recusada")));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/carros/resumo");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
            corpo.GetProperty("disponiveis").GetInt64().ShouldBe(2);
            corpo.GetProperty("indicadores").ValueKind.ShouldBe(JsonValueKind.Null);
        }

        [Fact(DisplayName = "ERRO - Deve devolver 500 em ProblemDetails, com Id de operação, quando o Mongo falha")]
        public async Task GetCarros_WhenDatabaseFails_ShouldReturnInternalErrorProblemWithOperationId()
        {
            // Arrange
            _servidor.RepositorioCarros.ListarPaginadoAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<SituacaoVeiculo?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new TimeoutException("A timeout occurred after 3000ms selecting a server"));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/carros");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            var problema = await LerProblemaAsync(resposta);
            problema.GetProperty("idOperacao").GetString().ShouldBe(resposta.Headers.GetValues(ContextoOperacao.Cabecalho).Single());
        }
    }
}
