using System.Net;
using Concessionaria.Motos.Aplicacao.Features.Motos;
using Concessionaria.Motos.Aplicacao.Features.Motos.Contratos;
using Concessionaria.Motos.Aplicacao.Features.Relatorios;
using Concessionaria.Motos.Dominio.Entidades;
using Concessionaria.Motos.Dominio.Enums;
using Concessionaria.Motos.Dominio.Interfaces;
using Concessionaria.Motos.Dominio.Modelos;
using Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios;
using Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios.Contratos;
using Concessionaria.Motos.tests.Common.Refit;
using Concessionaria.Motos.tests.DataBuilder;
using ErrorOr;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using NSubstitute;
using Shouldly;

namespace Concessionaria.Motos.tests.Services
{
    /// <summary>CRUD de motos, bloqueio de edição/exclusão de vendidas e resumo do painel.</summary>
    public class ServicoMotosTests
    {
        private readonly IRepositorioMotos _repositorioMotosMock;
        private readonly IRepositorioVendas _repositorioVendasMock;
        private readonly IRelatoriosApi _relatoriosApiMock;
        private readonly ServicoMotos _sut;

        /// <summary>
        /// Monta o serviço com repositórios e cliente de Relatórios substituídos. O <see cref="ServicoRelatorios"/>
        /// é concreto e usado de verdade, porque o resumo depende do comportamento dele quando Relatórios falha.
        /// </summary>
        public ServicoMotosTests()
        {
            _repositorioMotosMock = Substitute.For<IRepositorioMotos>();
            _repositorioVendasMock = Substitute.For<IRepositorioVendas>();
            _relatoriosApiMock = Substitute.For<IRelatoriosApi>();
            var servicoRelatorios = new ServicoRelatorios(_repositorioMotosMock, _repositorioVendasMock, _relatoriosApiMock, Substitute.For<ILogger<ServicoRelatorios>>());
            _sut = new ServicoMotos(_repositorioMotosMock, _repositorioVendasMock, servicoRelatorios, Substitute.For<ILogger<ServicoMotos>>());
        }

        /// <summary>Configura o repositório para devolver a moto pelo Id dele.</summary>
        private Moto Existente(Moto moto)
        {
            _repositorioMotosMock.ObterPorIdAsync(moto.Id, Arg.Any<CancellationToken>()).Returns(moto);
            return moto;
        }

        [Fact(DisplayName = "SUCESSO - Deve listar a primeira página com 10 itens quando a paginação não é informada")]
        public async Task ListarAsync_WhenPaginationIsMissing_ShouldUseDefaults()
        {
            // Arrange
            var motos = MotoDataBuilder.AsList(2);
            _repositorioMotosMock.ListarPaginadoAsync(1, 10, null, null, Arg.Any<CancellationToken>()).Returns((motos, 2L));

            // Act
            var resultado = await _sut.ListarAsync(new FiltroMotos(null, null, null, null), CancellationToken.None);

            // Assert
            resultado.Pagina.ShouldBe(1);
            resultado.TamanhoPagina.ShouldBe(10);
            resultado.Total.ShouldBe(2);
            resultado.Itens.Select(c => c.Id).ShouldBe(motos.Select(c => c.Id));
        }

        [Fact(DisplayName = "SUCESSO - Deve limitar a página a no mínimo 1 e o tamanho a no máximo 100")]
        public async Task ListarAsync_WhenPaginationIsOutOfRange_ShouldClampValues()
        {
            // Arrange
            _repositorioMotosMock.ListarPaginadoAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<SituacaoVeiculo?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns((new List<Moto>(), 0L));

            // Act
            var resultado = await _sut.ListarAsync(new FiltroMotos(-3, 500, SituacaoVeiculo.Vendido, "titan"), CancellationToken.None);

            // Assert
            resultado.Pagina.ShouldBe(1);
            resultado.TamanhoPagina.ShouldBe(100);
            await _repositorioMotosMock.Received(1).ListarPaginadoAsync(1, 100, SituacaoVeiculo.Vendido, "titan", Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver a moto encontrada pelo Id")]
        public async Task ObterPorIdAsync_WhenCarExists_ShouldReturnCar()
        {
            // Arrange
            var moto = Existente(MotoDataBuilder.Create().WithMarca("Fiat"));

            // Act
            var resultado = await _sut.ObterPorIdAsync(moto.Id, CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
            resultado.Value.Id.ShouldBe(moto.Id);
            resultado.Value.Marca.ShouldBe("Fiat");
        }

        [Fact(DisplayName = "ERRO - Deve devolver não encontrado quando a moto não existe")]
        public async Task ObterPorIdAsync_WhenCarDoesNotExist_ShouldReturnNotFound()
        {
            // Act
            var resultado = await _sut.ObterPorIdAsync(ObjectId.GenerateNewId().ToString(), CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeTrue();
            resultado.FirstError.Type.ShouldBe(ErrorType.NotFound);
            resultado.FirstError.Code.ShouldBe("Moto.NaoEncontrado");
        }

        [Fact(DisplayName = "ERRO - Deve tratar Id fora do formato ObjectId como não encontrado, sem consultar o banco")]
        public async Task ObterPorIdAsync_WhenIdIsNotObjectId_ShouldReturnNotFoundWithoutQuery()
        {
            // Act
            var resultado = await _sut.ObterPorIdAsync("id-invalido", CancellationToken.None);

            // Assert
            resultado.FirstError.Code.ShouldBe("Moto.NaoEncontrado");
            await _repositorioMotosMock.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default!, default);
        }

        [Fact(DisplayName = "SUCESSO - Deve cadastrar a moto disponível, sem espaços nas pontas dos textos")]
        public async Task CriarAsync_WhenRequestIsValid_ShouldInsertTrimmedAvailableCar()
        {
            // Arrange
            var requisicao = new RequisicaoMoto("  Toyota ", " Corolla ", 2024, " Prata ", 160, 150_000m);

            // Act
            var resultado = await _sut.CriarAsync(requisicao, CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
            resultado.Value.Situacao.ShouldBe(SituacaoVeiculo.Disponivel);
            await _repositorioMotosMock.Received(1).InserirAsync(
                Arg.Is<Moto>(c => c.Marca == "Toyota" && c.Modelo == "Corolla" && c.Cor == "Prata" && c.Ano == 2024 && c.Cilindrada == 160 && c.Preco == 150_000m && c.Id == resultado.Value.Id),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve devolver os erros de validação sem gravar a moto")]
        public async Task CriarAsync_WhenRequestIsInvalid_ShouldReturnValidationErrorsWithoutInsert()
        {
            // Arrange
            var requisicao = new RequisicaoMoto("", "Corolla", 2024, "Prata", 30, 0m);

            // Act
            var resultado = await _sut.CriarAsync(requisicao, CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeTrue();
            resultado.Errors.Select(e => e.Code).ShouldBe(["Marca", "Cilindrada", "Preco"]);
            await _repositorioMotosMock.DidNotReceiveWithAnyArgs().InserirAsync(default!, default);
        }

        [Fact(DisplayName = "SUCESSO - Deve atualizar os dados da moto disponível e registrar a data de atualização")]
        public async Task AtualizarAsync_WhenCarIsAvailable_ShouldReplaceWithNewData()
        {
            // Arrange
            var moto = Existente(MotoDataBuilder.Create());
            var requisicao = new RequisicaoMoto(" Honda ", "Civic", 2025, "Preto", 160, 180_000m);

            // Act
            var resultado = await _sut.AtualizarAsync(moto.Id, requisicao, CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
            resultado.Value.Marca.ShouldBe("Honda");
            await _repositorioMotosMock.Received(1).SubstituirAsync(
                Arg.Is<Moto>(c => c.Id == moto.Id && c.Marca == "Honda" && c.Modelo == "Civic" && c.Preco == 180_000m && c.AtualizadoEm != null),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve impedir a edição de moto já vendida")]
        public async Task AtualizarAsync_WhenCarIsSold_ShouldReturnConflictWithoutReplace()
        {
            // Arrange
            var moto = Existente(MotoDataBuilder.Create().WithSituacao(SituacaoVeiculo.Vendido));

            // Act
            var resultado = await _sut.AtualizarAsync(moto.Id, new RequisicaoMoto("Honda", "Civic", 2025, "Preto", 160, 180_000m), CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.Conflict);
            resultado.FirstError.Code.ShouldBe("Moto.JaVendido");
            await _repositorioMotosMock.DidNotReceiveWithAnyArgs().SubstituirAsync(default!, default);
        }

        [Fact(DisplayName = "ERRO - Deve devolver não encontrado ao editar moto inexistente")]
        public async Task AtualizarAsync_WhenCarDoesNotExist_ShouldReturnNotFound()
        {
            // Act
            var resultado = await _sut.AtualizarAsync(ObjectId.GenerateNewId().ToString(), new RequisicaoMoto("Honda", "Civic", 2025, "Preto", 160, 180_000m), CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.NotFound);
            await _repositorioMotosMock.DidNotReceiveWithAnyArgs().SubstituirAsync(default!, default);
        }

        [Fact(DisplayName = "ERRO - Deve validar a requisição antes de procurar a moto")]
        public async Task AtualizarAsync_WhenRequestIsInvalid_ShouldReturnValidationErrorsWithoutQuery()
        {
            // Act
            var resultado = await _sut.AtualizarAsync(ObjectId.GenerateNewId().ToString(), new RequisicaoMoto("Honda", "Civic", 1900, "Preto", 160, 180_000m), CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.Validation);
            resultado.FirstError.Code.ShouldBe("Ano");
            await _repositorioMotosMock.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default!, default);
        }

        [Fact(DisplayName = "SUCESSO - Deve excluir a moto disponível")]
        public async Task ExcluirAsync_WhenCarIsAvailable_ShouldDelete()
        {
            // Arrange
            var moto = Existente(MotoDataBuilder.Create());

            // Act
            var resultado = await _sut.ExcluirAsync(moto.Id, CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
            resultado.Value.ShouldBe(Result.Deleted);
            await _repositorioMotosMock.Received(1).ExcluirAsync(moto.Id, Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve impedir a exclusão de moto já vendida")]
        public async Task ExcluirAsync_WhenCarIsSold_ShouldReturnConflictWithoutDelete()
        {
            // Arrange
            var moto = Existente(MotoDataBuilder.Create().WithSituacao(SituacaoVeiculo.Vendido));

            // Act
            var resultado = await _sut.ExcluirAsync(moto.Id, CancellationToken.None);

            // Assert
            resultado.FirstError.Code.ShouldBe("Moto.JaVendido");
            await _repositorioMotosMock.DidNotReceiveWithAnyArgs().ExcluirAsync(default!, default);
        }

        [Fact(DisplayName = "ERRO - Deve devolver não encontrado ao excluir moto inexistente")]
        public async Task ExcluirAsync_WhenCarDoesNotExist_ShouldReturnNotFound()
        {
            // Act
            var resultado = await _sut.ExcluirAsync("abc", CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.NotFound);
            await _repositorioMotosMock.DidNotReceiveWithAnyArgs().ExcluirAsync(default!, default);
        }

        [Fact(DisplayName = "SUCESSO - Deve montar o resumo com totais do estoque e os indicadores calculados por Relatórios")]
        public async Task ObterResumoAsync_WhenReportsServiceResponds_ShouldReturnTotalsAndIndicators()
        {
            // Arrange
            var vendas = VendaDataBuilder.AsList(2);
            var indicadores = new IndicadoresVendas(2, 300_000m, 150_000m, 200_000m, "Fiat");
            _repositorioMotosMock.ObterTotaisPorSituacaoAsync(Arg.Any<CancellationToken>()).Returns(new Dictionary<SituacaoVeiculo, TotaisEstoque>
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

        [Fact(DisplayName = "SUCESSO - Deve zerar os totais de uma situação que não tem nenhuma moto")]
        public async Task ObterResumoAsync_WhenThereAreNoSoldCars_ShouldReturnZeroForSold()
        {
            // Arrange
            _repositorioMotosMock.ObterTotaisPorSituacaoAsync(Arg.Any<CancellationToken>())
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
            _repositorioMotosMock.ObterTotaisPorSituacaoAsync(Arg.Any<CancellationToken>())
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
            _repositorioMotosMock.ObterTotaisPorSituacaoAsync(Arg.Any<CancellationToken>()).Returns(new Dictionary<SituacaoVeiculo, TotaisEstoque>());
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
