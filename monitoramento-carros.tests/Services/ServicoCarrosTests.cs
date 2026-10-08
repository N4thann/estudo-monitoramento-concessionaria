using System.Net;
using Concessionaria.Carros.Aplicacao.Features.Carros;
using Concessionaria.Carros.Aplicacao.Features.Carros.Contratos;
using Concessionaria.Carros.Aplicacao.Features.Relatorios;
using Concessionaria.Carros.Dominio.Entidades;
using Concessionaria.Carros.Dominio.Enums;
using Concessionaria.Carros.Dominio.Interfaces;
using Concessionaria.Carros.Dominio.Modelos;
using Concessionaria.Carros.Infraestrutura.Integracoes.Relatorios;
using Concessionaria.Carros.Infraestrutura.Integracoes.Relatorios.Contratos;
using Concessionaria.Carros.tests.Common.Refit;
using Concessionaria.Carros.tests.DataBuilder;
using ErrorOr;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using NSubstitute;
using Shouldly;

namespace Concessionaria.Carros.tests.Services
{
    /// <summary>CRUD de carros, bloqueio de edição/exclusão de vendidos e resumo do painel.</summary>
    public class ServicoCarrosTests
    {
        private readonly IRepositorioCarros _repositorioCarrosMock;
        private readonly IRepositorioVendas _repositorioVendasMock;
        private readonly IRelatoriosApi _relatoriosApiMock;
        private readonly ServicoCarros _sut;

        /// <summary>
        /// Monta o serviço com repositórios e cliente de Relatórios substituídos. O <see cref="ServicoRelatorios"/>
        /// é concreto e usado de verdade, porque o resumo depende do comportamento dele quando Relatórios falha.
        /// </summary>
        public ServicoCarrosTests()
        {
            _repositorioCarrosMock = Substitute.For<IRepositorioCarros>();
            _repositorioVendasMock = Substitute.For<IRepositorioVendas>();
            _relatoriosApiMock = Substitute.For<IRelatoriosApi>();
            var servicoRelatorios = new ServicoRelatorios(_repositorioCarrosMock, _repositorioVendasMock, _relatoriosApiMock, Substitute.For<ILogger<ServicoRelatorios>>());
            _sut = new ServicoCarros(_repositorioCarrosMock, _repositorioVendasMock, servicoRelatorios, Substitute.For<ILogger<ServicoCarros>>());
        }

        /// <summary>Configura o repositório para devolver o carro pelo Id dele.</summary>
        private Carro Existente(Carro carro)
        {
            _repositorioCarrosMock.ObterPorIdAsync(carro.Id, Arg.Any<CancellationToken>()).Returns(carro);
            return carro;
        }

        [Fact(DisplayName = "SUCESSO - Deve listar a primeira página com 10 itens quando a paginação não é informada")]
        public async Task ListarAsync_WhenPaginationIsMissing_ShouldUseDefaults()
        {
            // Arrange
            var carros = CarroDataBuilder.AsList(2);
            _repositorioCarrosMock.ListarPaginadoAsync(1, 10, null, null, Arg.Any<CancellationToken>()).Returns((carros, 2L));

            // Act
            var resultado = await _sut.ListarAsync(new FiltroCarros(null, null, null, null), CancellationToken.None);

            // Assert
            resultado.Pagina.ShouldBe(1);
            resultado.TamanhoPagina.ShouldBe(10);
            resultado.Total.ShouldBe(2);
            resultado.Itens.Select(c => c.Id).ShouldBe(carros.Select(c => c.Id));
        }

        [Fact(DisplayName = "SUCESSO - Deve limitar a página a no mínimo 1 e o tamanho a no máximo 100")]
        public async Task ListarAsync_WhenPaginationIsOutOfRange_ShouldClampValues()
        {
            // Arrange
            _repositorioCarrosMock.ListarPaginadoAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<SituacaoVeiculo?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns((new List<Carro>(), 0L));

            // Act
            var resultado = await _sut.ListarAsync(new FiltroCarros(-3, 500, SituacaoVeiculo.Vendido, "corolla"), CancellationToken.None);

            // Assert
            resultado.Pagina.ShouldBe(1);
            resultado.TamanhoPagina.ShouldBe(100);
            await _repositorioCarrosMock.Received(1).ListarPaginadoAsync(1, 100, SituacaoVeiculo.Vendido, "corolla", Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver o carro encontrado pelo Id")]
        public async Task ObterPorIdAsync_WhenCarExists_ShouldReturnCar()
        {
            // Arrange
            var carro = Existente(CarroDataBuilder.Create().WithMarca("Fiat"));

            // Act
            var resultado = await _sut.ObterPorIdAsync(carro.Id, CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
            resultado.Value.Id.ShouldBe(carro.Id);
            resultado.Value.Marca.ShouldBe("Fiat");
        }

        [Fact(DisplayName = "ERRO - Deve devolver não encontrado quando o carro não existe")]
        public async Task ObterPorIdAsync_WhenCarDoesNotExist_ShouldReturnNotFound()
        {
            // Act
            var resultado = await _sut.ObterPorIdAsync(ObjectId.GenerateNewId().ToString(), CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeTrue();
            resultado.FirstError.Type.ShouldBe(ErrorType.NotFound);
            resultado.FirstError.Code.ShouldBe("Carro.NaoEncontrado");
        }

        [Fact(DisplayName = "ERRO - Deve tratar Id fora do formato ObjectId como não encontrado, sem consultar o banco")]
        public async Task ObterPorIdAsync_WhenIdIsNotObjectId_ShouldReturnNotFoundWithoutQuery()
        {
            // Act
            var resultado = await _sut.ObterPorIdAsync("id-invalido", CancellationToken.None);

            // Assert
            resultado.FirstError.Code.ShouldBe("Carro.NaoEncontrado");
            await _repositorioCarrosMock.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default!, default);
        }

        [Fact(DisplayName = "SUCESSO - Deve cadastrar o carro disponível, sem espaços nas pontas dos textos")]
        public async Task CriarAsync_WhenRequestIsValid_ShouldInsertTrimmedAvailableCar()
        {
            // Arrange
            var requisicao = new RequisicaoCarro("  Toyota ", " Corolla ", 2024, " Prata ", 4, 150_000m);

            // Act
            var resultado = await _sut.CriarAsync(requisicao, CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
            resultado.Value.Situacao.ShouldBe(SituacaoVeiculo.Disponivel);
            await _repositorioCarrosMock.Received(1).InserirAsync(
                Arg.Is<Carro>(c => c.Marca == "Toyota" && c.Modelo == "Corolla" && c.Cor == "Prata" && c.Ano == 2024 && c.Portas == 4 && c.Preco == 150_000m && c.Id == resultado.Value.Id),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve devolver os erros de validação sem gravar o carro")]
        public async Task CriarAsync_WhenRequestIsInvalid_ShouldReturnValidationErrorsWithoutInsert()
        {
            // Arrange
            var requisicao = new RequisicaoCarro("", "Corolla", 2024, "Prata", 9, 0m);

            // Act
            var resultado = await _sut.CriarAsync(requisicao, CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeTrue();
            resultado.Errors.Select(e => e.Code).ShouldBe(["Marca", "Portas", "Preco"]);
            await _repositorioCarrosMock.DidNotReceiveWithAnyArgs().InserirAsync(default!, default);
        }

        [Fact(DisplayName = "SUCESSO - Deve atualizar os dados do carro disponível e registrar a data de atualização")]
        public async Task AtualizarAsync_WhenCarIsAvailable_ShouldReplaceWithNewData()
        {
            // Arrange
            var carro = Existente(CarroDataBuilder.Create());
            var requisicao = new RequisicaoCarro(" Honda ", "Civic", 2025, "Preto", 4, 180_000m);

            // Act
            var resultado = await _sut.AtualizarAsync(carro.Id, requisicao, CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
            resultado.Value.Marca.ShouldBe("Honda");
            await _repositorioCarrosMock.Received(1).SubstituirAsync(
                Arg.Is<Carro>(c => c.Id == carro.Id && c.Marca == "Honda" && c.Modelo == "Civic" && c.Preco == 180_000m && c.AtualizadoEm != null),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve impedir a edição de carro já vendido")]
        public async Task AtualizarAsync_WhenCarIsSold_ShouldReturnConflictWithoutReplace()
        {
            // Arrange
            var carro = Existente(CarroDataBuilder.Create().WithSituacao(SituacaoVeiculo.Vendido));

            // Act
            var resultado = await _sut.AtualizarAsync(carro.Id, new RequisicaoCarro("Honda", "Civic", 2025, "Preto", 4, 180_000m), CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.Conflict);
            resultado.FirstError.Code.ShouldBe("Carro.JaVendido");
            await _repositorioCarrosMock.DidNotReceiveWithAnyArgs().SubstituirAsync(default!, default);
        }

        [Fact(DisplayName = "ERRO - Deve devolver não encontrado ao editar carro inexistente")]
        public async Task AtualizarAsync_WhenCarDoesNotExist_ShouldReturnNotFound()
        {
            // Act
            var resultado = await _sut.AtualizarAsync(ObjectId.GenerateNewId().ToString(), new RequisicaoCarro("Honda", "Civic", 2025, "Preto", 4, 180_000m), CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.NotFound);
            await _repositorioCarrosMock.DidNotReceiveWithAnyArgs().SubstituirAsync(default!, default);
        }

        [Fact(DisplayName = "ERRO - Deve validar a requisição antes de procurar o carro")]
        public async Task AtualizarAsync_WhenRequestIsInvalid_ShouldReturnValidationErrorsWithoutQuery()
        {
            // Act
            var resultado = await _sut.AtualizarAsync(ObjectId.GenerateNewId().ToString(), new RequisicaoCarro("Honda", "Civic", 1900, "Preto", 4, 180_000m), CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.Validation);
            resultado.FirstError.Code.ShouldBe("Ano");
            await _repositorioCarrosMock.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default!, default);
        }

        [Fact(DisplayName = "SUCESSO - Deve excluir o carro disponível")]
        public async Task ExcluirAsync_WhenCarIsAvailable_ShouldDelete()
        {
            // Arrange
            var carro = Existente(CarroDataBuilder.Create());

            // Act
            var resultado = await _sut.ExcluirAsync(carro.Id, CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
            resultado.Value.ShouldBe(Result.Deleted);
            await _repositorioCarrosMock.Received(1).ExcluirAsync(carro.Id, Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve impedir a exclusão de carro já vendido")]
        public async Task ExcluirAsync_WhenCarIsSold_ShouldReturnConflictWithoutDelete()
        {
            // Arrange
            var carro = Existente(CarroDataBuilder.Create().WithSituacao(SituacaoVeiculo.Vendido));

            // Act
            var resultado = await _sut.ExcluirAsync(carro.Id, CancellationToken.None);

            // Assert
            resultado.FirstError.Code.ShouldBe("Carro.JaVendido");
            await _repositorioCarrosMock.DidNotReceiveWithAnyArgs().ExcluirAsync(default!, default);
        }

        [Fact(DisplayName = "ERRO - Deve devolver não encontrado ao excluir carro inexistente")]
        public async Task ExcluirAsync_WhenCarDoesNotExist_ShouldReturnNotFound()
        {
            // Act
            var resultado = await _sut.ExcluirAsync("abc", CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.NotFound);
            await _repositorioCarrosMock.DidNotReceiveWithAnyArgs().ExcluirAsync(default!, default);
        }

        [Fact(DisplayName = "SUCESSO - Deve montar o resumo com totais do estoque e os indicadores calculados por Relatórios")]
        public async Task ObterResumoAsync_WhenReportsServiceResponds_ShouldReturnTotalsAndIndicators()
        {
            // Arrange
            var vendas = VendaDataBuilder.AsList(2);
            var indicadores = new IndicadoresVendas(2, 300_000m, 150_000m, 200_000m, "Fiat");
            _repositorioCarrosMock.ObterTotaisPorSituacaoAsync(Arg.Any<CancellationToken>()).Returns(new Dictionary<SituacaoVeiculo, TotaisEstoque>
            {
                [SituacaoVeiculo.Disponivel] = new(5, 750_000m),
                [SituacaoVeiculo.Vendido] = new(2, 300_000m)
            });
            _repositorioVendasMock.ListarPorPeriodoAsync(null, null, Arg.Any<CancellationToken>()).Returns(vendas);
            _relatoriosApiMock.CalcularIndicadoresVendasAsync(Arg.Any<SolicitacaoIndicadoresVendas>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.Sucesso(indicadores));

            // Act
            var resumo = await _sut.ObterResumoAsync(CancellationToken.None);

            // Assert
            resumo.ShouldBe(new RespostaResumo(7, 5, 2, 750_000m, indicadores));
            await _relatoriosApiMock.Received(1).CalcularIndicadoresVendasAsync(
                Arg.Is<SolicitacaoIndicadoresVendas>(s => s.Vendas.Count == 2 && s.Vendas[0].Veiculo == vendas[0].DescricaoVeiculo),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "SUCESSO - Deve zerar os totais de uma situação que não tem nenhum carro")]
        public async Task ObterResumoAsync_WhenThereAreNoSoldCars_ShouldReturnZeroForSold()
        {
            // Arrange
            _repositorioCarrosMock.ObterTotaisPorSituacaoAsync(Arg.Any<CancellationToken>())
                .Returns(new Dictionary<SituacaoVeiculo, TotaisEstoque> { [SituacaoVeiculo.Disponivel] = new(3, 90_000m) });
            _repositorioVendasMock.ListarPorPeriodoAsync(null, null, Arg.Any<CancellationToken>()).Returns(new List<Venda>());
            _relatoriosApiMock.CalcularIndicadoresVendasAsync(Arg.Any<SolicitacaoIndicadoresVendas>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.Sucesso(new IndicadoresVendas(0, 0m, 0m, 0m, null)));

            // Act
            var resumo = await _sut.ObterResumoAsync(CancellationToken.None);

            // Assert
            resumo.TotalVeiculos.ShouldBe(3);
            resumo.Vendidos.ShouldBe(0);
            resumo.ValorEmEstoque.ShouldBe(90_000m);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver o resumo sem indicadores quando Relatórios está fora (degradação)")]
        public async Task ObterResumoAsync_WhenReportsServiceIsDown_ShouldReturnTotalsWithoutIndicators()
        {
            // Arrange
            _repositorioCarrosMock.ObterTotaisPorSituacaoAsync(Arg.Any<CancellationToken>())
                .Returns(new Dictionary<SituacaoVeiculo, TotaisEstoque> { [SituacaoVeiculo.Disponivel] = new(1, 50_000m) });
            _repositorioVendasMock.ListarPorPeriodoAsync(null, null, Arg.Any<CancellationToken>()).Returns(new List<Venda>());
            _relatoriosApiMock.CalcularIndicadoresVendasAsync(Arg.Any<SolicitacaoIndicadoresVendas>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<IndicadoresVendas>(new HttpRequestException("Conexão recusada")));

            // Act
            var resumo = await _sut.ObterResumoAsync(CancellationToken.None);

            // Assert
            resumo.Indicadores.ShouldBeNull();
            resumo.Disponiveis.ShouldBe(1);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver o resumo sem indicadores quando Relatórios responde 500")]
        public async Task ObterResumoAsync_WhenReportsServiceReturnsServerError_ShouldReturnTotalsWithoutIndicators()
        {
            // Arrange
            _repositorioCarrosMock.ObterTotaisPorSituacaoAsync(Arg.Any<CancellationToken>()).Returns(new Dictionary<SituacaoVeiculo, TotaisEstoque>());
            _repositorioVendasMock.ListarPorPeriodoAsync(null, null, Arg.Any<CancellationToken>()).Returns(new List<Venda>());
            _relatoriosApiMock.CalcularIndicadoresVendasAsync(Arg.Any<SolicitacaoIndicadoresVendas>(), Arg.Any<CancellationToken>())
                .Returns(await RespostasApi.ErroHttpAsync<IndicadoresVendas>(HttpStatusCode.InternalServerError));

            // Act
            var resumo = await _sut.ObterResumoAsync(CancellationToken.None);

            // Assert
            resumo.ShouldBe(new RespostaResumo(0, 0, 0, 0m, null));
        }
    }
}
