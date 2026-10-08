using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Concessionaria.Motos.Aplicacao.Features.Motos.Contratos;
using Concessionaria.Motos.Comum.Observabilidade;
using Concessionaria.Motos.Dominio.Entidades;
using Concessionaria.Motos.Dominio.Enums;
using Concessionaria.Motos.Dominio.Modelos;
using Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios.Contratos;
using Concessionaria.Motos.tests.Common.Hospedagem;
using Concessionaria.Motos.tests.Common.Refit;
using Concessionaria.Motos.tests.DataBuilder;
using MongoDB.Bson;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Concessionaria.Motos.tests.Features
{
    /// <summary>Endpoints de /motos: binding da query, status HTTP de cada regra e ProblemDetails correlacionado.</summary>
    public class MotosEndpointsTests : IAsyncLifetime
    {
        private ServidorTeste _servidor = null!;

        /// <summary>Sobe o servidor com os endpoints de Motos.</summary>
        public async Task InitializeAsync() => _servidor = await ServidorTeste.IniciarAsync();

        /// <summary>Derruba o servidor.</summary>
        public async Task DisposeAsync() => await _servidor.DisposeAsync();

        /// <summary>Lê o corpo como ProblemDetails, conferindo o tipo de conteúdo.</summary>
        private static async Task<JsonElement> LerProblemaAsync(HttpResponseMessage resposta)
        {
            resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
            return await resposta.Content.ReadFromJsonAsync<JsonElement>();
        }

        /// <summary>Configura o repositório para devolver a moto pelo Id dele.</summary>
        private Moto Existente(Moto moto)
        {
            _servidor.RepositorioMotos.ObterPorIdAsync(moto.Id, Arg.Any<CancellationToken>()).Returns(moto);
            return moto;
        }

        [Fact(DisplayName = "SUCESSO - Deve repassar paginação, situação e busca da query string e devolver a situação como texto")]
        public async Task GetMotos_WhenQueryHasFilters_ShouldBindThemAndSerializeEnumAsString()
        {
            // Arrange
            var moto = MotoDataBuilder.Create().WithSituacao(SituacaoVeiculo.Vendido).Build();
            _servidor.RepositorioMotos.ListarPaginadoAsync(2, 5, SituacaoVeiculo.Vendido, "titan", Arg.Any<CancellationToken>())
                .Returns((new List<Moto> { moto }, 6L));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/motos?pagina=2&tamanhoPagina=5&situacao=Vendido&busca=titan");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
            corpo.GetProperty("total").GetInt64().ShouldBe(6);
            corpo.GetProperty("pagina").GetInt32().ShouldBe(2);
            corpo.GetProperty("itens")[0].GetProperty("situacao").GetString().ShouldBe("Vendido");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 200 com a moto encontrada")]
        public async Task GetMotoPorId_WhenCarExists_ShouldReturnOk()
        {
            // Arrange
            var moto = Existente(MotoDataBuilder.Create().WithModelo("Corolla"));

            // Act
            var resposta = await _servidor.Cliente.GetAsync($"/motos/{moto.Id}");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("modelo").GetString().ShouldBe("Corolla");
        }

        [Fact(DisplayName = "ERRO - Deve devolver 404 com o código do erro e o Id de operação no ProblemDetails")]
        public async Task GetMotoPorId_WhenCarDoesNotExist_ShouldReturnNotFoundProblem()
        {
            // Arrange
            using var requisicao = new HttpRequestMessage(HttpMethod.Get, $"/motos/{ObjectId.GenerateNewId()}");
            requisicao.Headers.Add(ContextoOperacao.Cabecalho, "id-vindo-do-bff");

            // Act
            var resposta = await _servidor.Cliente.SendAsync(requisicao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            resposta.Headers.GetValues(ContextoOperacao.Cabecalho).ShouldBe(["id-vindo-do-bff"]);
            var problema = await LerProblemaAsync(resposta);
            problema.GetProperty("title").GetString().ShouldBe("Moto não encontrada.");
            problema.GetProperty("codigo").GetString().ShouldBe("Moto.NaoEncontrado");
            problema.GetProperty("idOperacao").GetString().ShouldBe("id-vindo-do-bff");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 201 com o endereço da moto cadastrada")]
        public async Task PostMoto_WhenRequestIsValid_ShouldReturnCreatedWithLocation()
        {
            // Arrange
            var requisicao = new RequisicaoMoto("Toyota", "Corolla", 2024, "Prata", 160, 150_000m);

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/motos", requisicao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
            var id = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
            resposta.Headers.Location!.ToString().ShouldBe($"/motos/{id}");
            await _servidor.RepositorioMotos.Received(1).InserirAsync(Arg.Is<Moto>(c => c.Id == id), Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve devolver 400 com os erros por campo quando o cadastro é inválido")]
        public async Task PostMoto_WhenRequestIsInvalid_ShouldReturnValidationProblem()
        {
            // Arrange
            var requisicao = new RequisicaoMoto("", "Corolla", 2024, "Prata", 30, 0m);

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/motos", requisicao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problema = await LerProblemaAsync(resposta);
            problema.GetProperty("title").GetString().ShouldBe("Um ou mais campos são inválidos.");
            var erros = problema.GetProperty("errors");
            erros.EnumerateObject().Select(p => p.Name).ShouldBe(["Marca", "Cilindrada", "Preco"], ignoreOrder: true);
            problema.TryGetProperty("idOperacao", out _).ShouldBeTrue();
        }

        [Fact(DisplayName = "ERRO - Deve devolver 409 ao editar moto já vendida")]
        public async Task PutMoto_WhenCarIsSold_ShouldReturnConflict()
        {
            // Arrange
            var moto = Existente(MotoDataBuilder.Create().WithSituacao(SituacaoVeiculo.Vendido));

            // Act
            var resposta = await _servidor.Cliente.PutAsJsonAsync($"/motos/{moto.Id}", new RequisicaoMoto("Honda", "Civic", 2025, "Preto", 160, 1m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await LerProblemaAsync(resposta)).GetProperty("codigo").GetString().ShouldBe("Moto.JaVendido");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 200 com a moto editada")]
        public async Task PutMoto_WhenCarIsAvailable_ShouldReturnOk()
        {
            // Arrange
            var moto = Existente(MotoDataBuilder.Create());

            // Act
            var resposta = await _servidor.Cliente.PutAsJsonAsync($"/motos/{moto.Id}", new RequisicaoMoto("Honda", "Civic", 2025, "Preto", 160, 1m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("marca").GetString().ShouldBe("Honda");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 204 ao excluir moto disponível")]
        public async Task DeleteMoto_WhenCarIsAvailable_ShouldReturnNoContent()
        {
            // Arrange
            var moto = Existente(MotoDataBuilder.Create());

            // Act
            var resposta = await _servidor.Cliente.DeleteAsync($"/motos/{moto.Id}");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            await _servidor.RepositorioMotos.Received(1).ExcluirAsync(moto.Id, Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve devolver 409 ao excluir moto já vendida")]
        public async Task DeleteMoto_WhenCarIsSold_ShouldReturnConflict()
        {
            // Arrange
            var moto = Existente(MotoDataBuilder.Create().WithSituacao(SituacaoVeiculo.Vendido));

            // Act
            var resposta = await _servidor.Cliente.DeleteAsync($"/motos/{moto.Id}");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 200 no resumo com indicadores nulos quando Relatórios está fora")]
        public async Task GetResumo_WhenReportsServiceIsDown_ShouldReturnOkWithNullIndicators()
        {
            // Arrange
            _servidor.RepositorioMotos.ObterTotaisPorSituacaoAsync(Arg.Any<CancellationToken>())
                .Returns(new Dictionary<SituacaoVeiculo, TotaisEstoque> { [SituacaoVeiculo.Disponivel] = new(2, 100_000m) });
            _servidor.RepositorioVendas.ListarPorPeriodoAsync(null, null, Arg.Any<CancellationToken>()).Returns(new List<Venda>());
            _servidor.RelatoriosApi.CalcularIndicadoresVendasAsync(Arg.Any<SolicitacaoIndicadoresVendas>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<IndicadoresVendas>(new HttpRequestException("Conexão recusada")));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/motos/resumo");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
            corpo.GetProperty("disponiveis").GetInt64().ShouldBe(2);
            corpo.GetProperty("indicadores").ValueKind.ShouldBe(JsonValueKind.Null);
        }

        [Fact(DisplayName = "ERRO - Deve devolver 500 em ProblemDetails, com Id de operação, quando o Mongo falha")]
        public async Task GetMotos_WhenDatabaseFails_ShouldReturnInternalErrorProblemWithOperationId()
        {
            // Arrange
            _servidor.RepositorioMotos.ListarPaginadoAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<SituacaoVeiculo?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new TimeoutException("A timeout occurred after 3000ms selecting a server"));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/motos");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            var problema = await LerProblemaAsync(resposta);
            problema.GetProperty("idOperacao").GetString().ShouldBe(resposta.Headers.GetValues(ContextoOperacao.Cabecalho).Single());
        }
    }
}
