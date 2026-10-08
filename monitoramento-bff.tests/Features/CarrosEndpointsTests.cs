using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Concessionaria.Bff.Comum.Observabilidade;
using Concessionaria.Bff.Dominio.Enums;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;
using Concessionaria.Bff.tests.Common.Hospedagem;
using Concessionaria.Bff.tests.Common.Refit;
using Concessionaria.Bff.tests.DataBuilder;
using NSubstitute;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Shouldly;

namespace Concessionaria.Bff.tests.Features
{
    /// <summary>
    /// Endpoints de /api/carros: repasse da query, do corpo e do Id para a API de Carros, status de sucesso do BFF,
    /// erros repassados (4xx), convertidos (5xx → 502) e falhas de rede (502/503/504) com Id de operação.
    /// </summary>
    public class CarrosEndpointsTests : IAsyncLifetime
    {
        private const string ProblemaConflito = """{"title":"Carro já foi vendido.","status":409,"codigo":"Carro.JaVendido"}""";

        private ServidorTeste _servidor = null!;

        /// <summary>Sobe o servidor com os endpoints do BFF.</summary>
        public async Task InitializeAsync() => _servidor = await ServidorTeste.IniciarAsync();

        /// <summary>Derruba o servidor.</summary>
        public async Task DisposeAsync() => await _servidor.DisposeAsync();

        /// <summary>Lê o corpo como ProblemDetails, conferindo o tipo de conteúdo.</summary>
        private static async Task<JsonElement> LerProblemaAsync(HttpResponseMessage resposta)
        {
            resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
            return await resposta.Content.ReadFromJsonAsync<JsonElement>();
        }

        [Fact(DisplayName = "SUCESSO - Deve repassar paginação, situação e busca da query string para Carros")]
        public async Task GetCarros_WhenQueryHasFilters_ShouldForwardThem()
        {
            // Arrange
            var carro = CarroDataBuilder.Create().WithSituacao(SituacaoVeiculo.Vendido).Build();
            _servidor.CarrosApi.ListarAsync(Arg.Any<FiltroVeiculos>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.Sucesso(new ResultadoPaginado<Carro>([carro], 6, 2, 5)));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/carros?pagina=2&tamanhoPagina=5&situacao=Vendido&busca=corolla");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
            corpo.GetProperty("total").GetInt64().ShouldBe(6);
            corpo.GetProperty("itens")[0].GetProperty("situacao").GetString().ShouldBe("Vendido");
            await _servidor.CarrosApi.Received(1).ListarAsync(new FiltroVeiculos(2, 5, SituacaoVeiculo.Vendido, "corolla"), Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve repassar o 404 de Carros com o ProblemDetails original")]
        public async Task GetCarroPorId_WhenCarDoesNotExist_ShouldForwardNotFound()
        {
            // Arrange
            const string problema = """{"title":"Carro não encontrado.","status":404,"codigo":"Carro.NaoEncontrado"}""";
            _servidor.CarrosApi.ObterPorIdAsync("inexistente", Arg.Any<CancellationToken>())
                .Returns(await RespostasApi.ErroHttpAsync<Carro>(HttpStatusCode.NotFound, problema));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/carros/inexistente");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await LerProblemaAsync(resposta)).GetProperty("codigo").GetString().ShouldBe("Carro.NaoEncontrado");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 201 com o endereço do carro no BFF")]
        public async Task PostCarro_WhenCarsServiceCreates_ShouldReturnCreatedWithBffLocation()
        {
            // Arrange
            var carro = CarroDataBuilder.Create().Build();
            var requisicao = new RequisicaoCarro(carro.Marca, carro.Modelo, carro.Ano, carro.Cor, carro.Portas, carro.Preco);
            _servidor.CarrosApi.CriarAsync(Arg.Any<RequisicaoCarro>(), Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(carro));

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/api/carros", requisicao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
            resposta.Headers.Location!.ToString().ShouldBe($"/api/carros/{carro.Id}");
            await _servidor.CarrosApi.Received(1).CriarAsync(requisicao, Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve repassar o 400 de validação com os erros por campo")]
        public async Task PostCarro_WhenCarsServiceRejects_ShouldForwardValidationProblem()
        {
            // Arrange
            const string problema = """{"title":"Um ou mais campos são inválidos.","status":400,"errors":{"Portas":["Quantidade de portas deve estar entre 2 e 5."]}}""";
            _servidor.CarrosApi.CriarAsync(Arg.Any<RequisicaoCarro>(), Arg.Any<CancellationToken>())
                .Returns(await RespostasApi.ErroHttpAsync<Carro>(HttpStatusCode.BadRequest, problema));

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/api/carros", new RequisicaoCarro("Fiat", "Uno", 2020, "Branco", 9, 1m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await LerProblemaAsync(resposta)).GetProperty("errors").TryGetProperty("Portas", out _).ShouldBeTrue();
        }

        [Fact(DisplayName = "SUCESSO - Deve repassar o Id e o corpo da atualização")]
        public async Task PutCarro_WhenCarIsUpdated_ShouldForwardIdAndBody()
        {
            // Arrange
            var carro = CarroDataBuilder.Create().Build();
            var requisicao = new RequisicaoCarro("Honda", "Civic", 2024, "Preto", 4, 150_000m);
            _servidor.CarrosApi.AtualizarAsync(carro.Id, requisicao, Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(carro));

            // Act
            var resposta = await _servidor.Cliente.PutAsJsonAsync($"/api/carros/{carro.Id}", requisicao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString().ShouldBe(carro.Id);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 204 quando a exclusão dá certo")]
        public async Task DeleteCarro_WhenCarIsDeleted_ShouldReturnNoContent()
        {
            // Arrange
            _servidor.CarrosApi.ExcluirAsync("abc", Arg.Any<CancellationToken>()).Returns(RespostasApi.SemConteudo());

            // Act
            var resposta = await _servidor.Cliente.DeleteAsync("/api/carros/abc");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 201 com o endereço da venda na lista de vendas do BFF")]
        public async Task PostVenda_WhenSaleIsRegistered_ShouldReturnCreatedWithSalesLocation()
        {
            // Arrange
            var venda = VendaDataBuilder.Create().Build();
            _servidor.CarrosApi.RegistrarVendaAsync(Arg.Any<RequisicaoVenda>(), Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(venda));

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/api/carros/vendas", new RequisicaoVenda(venda.VeiculoId, "Maria", "12345678909", 99_000m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
            resposta.Headers.Location!.ToString().ShouldBe($"/api/vendas/{venda.Id}");
        }

        [Fact(DisplayName = "ERRO - Deve repassar o 409 da segunda venda do mesmo carro")]
        public async Task PostVenda_WhenCarIsAlreadySold_ShouldForwardConflict()
        {
            // Arrange
            _servidor.CarrosApi.RegistrarVendaAsync(Arg.Any<RequisicaoVenda>(), Arg.Any<CancellationToken>())
                .Returns(await RespostasApi.ErroHttpAsync<Venda>(HttpStatusCode.Conflict, ProblemaConflito));

            // Act
            var resposta = await _servidor.Cliente.PostAsJsonAsync("/api/carros/vendas", new RequisicaoVenda("abc", "Maria", "12345678909", 1m));

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await LerProblemaAsync(resposta)).GetProperty("codigo").GetString().ShouldBe("Carro.JaVendido");
        }

        [Fact(DisplayName = "SUCESSO - Deve repassar o período e devolver a planilha de vendas com o nome enviado por Carros")]
        public async Task GetPlanilhaVendas_WhenCarsServiceSendsFile_ShouldForwardPeriodAndReturnFile()
        {
            // Arrange
            var disposicao = new ContentDispositionHeaderValue("attachment") { FileName = "relatorio-vendas-carros_20260901_20260930.xlsx" };
            _servidor.CarrosApi.GerarPlanilhaVendasAsync(Arg.Any<FiltroPeriodo>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.Arquivo([0x50, 0x4B, 0x03, 0x04], disposicao, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/carros/relatorios/vendas?de=2026-09-01&ate=2026-09-30");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
            resposta.Content.Headers.ContentDisposition!.FileName.ShouldBe("relatorio-vendas-carros_20260901_20260930.xlsx");
            (await resposta.Content.ReadAsByteArrayAsync()).ShouldBe(new byte[] { 0x50, 0x4B, 0x03, 0x04 });
            await _servidor.CarrosApi.Received(1).GerarPlanilhaVendasAsync(
                new FiltroPeriodo(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)), Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve devolver 502 quando Carros falha ao gerar a planilha de estoque (Relatórios fora)")]
        public async Task GetPlanilhaEstoque_WhenCarsServiceFails_ShouldReturnBadGateway()
        {
            // Arrange
            const string problema = """{"title":"Não foi possível gerar a planilha no serviço de relatórios.","status":502,"codigo":"Relatorios.FalhaNaGeracao"}""";
            _servidor.CarrosApi.GerarPlanilhaEstoqueAsync(Arg.Any<CancellationToken>())
                .Returns(await RespostasApi.ErroHttpAsync<Stream>(HttpStatusCode.BadGateway, problema));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/carros/relatorios/estoque");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
            (await LerProblemaAsync(resposta)).GetProperty("codigo").GetString().ShouldBe("Relatorios.FalhaNaGeracao");
        }

        [Fact(DisplayName = "ERRO - Deve devolver 502 com o Id de operação quando a conexão com Carros é recusada")]
        public async Task GetCarros_WhenConnectionIsRefused_ShouldReturnBadGatewayWithOperationId()
        {
            // Arrange
            _servidor.CarrosApi.ListarAsync(Arg.Any<FiltroVeiculos>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<ResultadoPaginado<Carro>>(new HttpRequestException("Conexão recusada")));
            using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/carros");
            requisicao.Headers.Add(ContextoOperacao.Cabecalho, "id-do-front");

            // Act
            var resposta = await _servidor.Cliente.SendAsync(requisicao);

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
            resposta.Headers.GetValues(ContextoOperacao.Cabecalho).ShouldBe(["id-do-front"]);
            (await LerProblemaAsync(resposta)).GetProperty("idOperacao").GetString().ShouldBe("id-do-front");
        }

        [Fact(DisplayName = "ERRO - Deve devolver 504 quando Carros não responde a tempo")]
        public async Task GetCarros_WhenCarsServiceTimesOut_ShouldReturnGatewayTimeout()
        {
            // Arrange
            _servidor.CarrosApi.ListarAsync(Arg.Any<FiltroVeiculos>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<ResultadoPaginado<Carro>>(new TimeoutRejectedException()));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/carros");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        }

        [Fact(DisplayName = "ERRO - Deve devolver 503 quando o circuito para Carros está aberto")]
        public async Task GetCarros_WhenCircuitIsOpen_ShouldReturnServiceUnavailable()
        {
            // Arrange
            _servidor.CarrosApi.ListarAsync(Arg.Any<FiltroVeiculos>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<ResultadoPaginado<Carro>>(new BrokenCircuitException()));

            // Act
            var resposta = await _servidor.Cliente.GetAsync("/api/carros");

            // Assert
            resposta.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }
    }
}
