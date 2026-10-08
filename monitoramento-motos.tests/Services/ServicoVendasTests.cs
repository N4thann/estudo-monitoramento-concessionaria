using Concessionaria.Motos.Aplicacao.Comum;
using Concessionaria.Motos.Aplicacao.Features.Vendas;
using Concessionaria.Motos.Aplicacao.Features.Vendas.Contratos;
using Concessionaria.Motos.Dominio.Entidades;
using Concessionaria.Motos.Dominio.Interfaces;
using Concessionaria.Motos.tests.DataBuilder;
using ErrorOr;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using NSubstitute;
using Shouldly;

namespace Concessionaria.Motos.tests.Services
{
    /// <summary>Registro de venda: validação, moto inexistente, venda dupla e retrato do veículo na venda.</summary>
    public class ServicoVendasTests
    {
        private readonly IRepositorioMotos _repositorioMotosMock;
        private readonly IRepositorioVendas _repositorioVendasMock;
        private readonly ServicoVendas _sut;

        /// <summary>Monta o serviço com os dois repositórios substituídos.</summary>
        public ServicoVendasTests()
        {
            _repositorioMotosMock = Substitute.For<IRepositorioMotos>();
            _repositorioVendasMock = Substitute.For<IRepositorioVendas>();
            _sut = new ServicoVendas(_repositorioMotosMock, _repositorioVendasMock, Substitute.For<ILogger<ServicoVendas>>());
        }

        /// <summary>Requisição válida para a moto informado.</summary>
        private static RequisicaoVenda RequisicaoPara(Moto moto) => new(moto.Id, " Maria Souza ", "123.456.789-09", 120_000m);

        /// <summary>Configura a moto como existente e ainda disponível para venda.</summary>
        private Moto Disponivel(Moto moto)
        {
            _repositorioMotosMock.ObterPorIdAsync(moto.Id, Arg.Any<CancellationToken>()).Returns(moto);
            _repositorioMotosMock.TentarMarcarComoVendidoAsync(moto.Id, Arg.Any<CancellationToken>()).Returns(true);
            return moto;
        }

        [Fact(DisplayName = "SUCESSO - Deve registrar a venda com o retrato do veículo, nome sem espaços e documento só com dígitos")]
        public async Task RegistrarAsync_WhenCarIsAvailable_ShouldInsertSale()
        {
            // Arrange
            var moto = Disponivel(MotoDataBuilder.Create().WithMarca("Toyota").WithModelo("Corolla").WithAno(2024));

            // Act
            var resultado = await _sut.RegistrarAsync(RequisicaoPara(moto), CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
            resultado.Value.DescricaoVeiculo.ShouldBe("Toyota Corolla 2024");
            resultado.Value.NomeCliente.ShouldBe("Maria Souza");
            resultado.Value.DocumentoCliente.ShouldBe("12345678909");
            await _repositorioVendasMock.Received(1).InserirAsync(
                Arg.Is<Venda>(v => v.VeiculoId == moto.Id && v.Marca == "Toyota" && v.Valor == 120_000m && v.Id == resultado.Value.Id),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve devolver os erros de validação sem consultar nem gravar nada")]
        public async Task RegistrarAsync_WhenRequestIsInvalid_ShouldReturnValidationErrorsWithoutQuery()
        {
            // Arrange
            var requisicao = new RequisicaoVenda(ObjectId.GenerateNewId().ToString(), "Maria", "123", 0m);

            // Act
            var resultado = await _sut.RegistrarAsync(requisicao, CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeTrue();
            resultado.Errors.Select(e => e.Code).ShouldBe(["DocumentoCliente", "Valor"]);
            await _repositorioMotosMock.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default!, default);
            await _repositorioVendasMock.DidNotReceiveWithAnyArgs().InserirAsync(default!, default);
        }

        [Fact(DisplayName = "ERRO - Deve devolver não encontrado quando a moto não existe")]
        public async Task RegistrarAsync_WhenCarDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var requisicao = new RequisicaoVenda(ObjectId.GenerateNewId().ToString(), "Maria", "12345678909", 10m);

            // Act
            var resultado = await _sut.RegistrarAsync(requisicao, CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.NotFound);
            resultado.FirstError.Code.ShouldBe("Moto.NaoEncontrado");
            await _repositorioMotosMock.DidNotReceiveWithAnyArgs().TentarMarcarComoVendidoAsync(default!, default);
        }

        [Fact(DisplayName = "ERRO - Deve tratar Id de veículo fora do formato ObjectId como não encontrado, sem consultar o banco")]
        public async Task RegistrarAsync_WhenVehicleIdIsNotObjectId_ShouldReturnNotFoundWithoutQuery()
        {
            // Arrange
            var requisicao = new RequisicaoVenda("nao-e-objectid", "Maria", "12345678909", 10m);

            // Act
            var resultado = await _sut.RegistrarAsync(requisicao, CancellationToken.None);

            // Assert
            resultado.FirstError.Code.ShouldBe("Moto.NaoEncontrado");
            await _repositorioMotosMock.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default!, default);
        }

        [Fact(DisplayName = "ERRO - Deve recusar a segunda venda do mesma moto sem gravar outra venda")]
        public async Task RegistrarAsync_WhenCarIsAlreadySold_ShouldReturnConflictWithoutInsert()
        {
            // Arrange
            var moto = MotoDataBuilder.Create().Build();
            _repositorioMotosMock.ObterPorIdAsync(moto.Id, Arg.Any<CancellationToken>()).Returns(moto);
            _repositorioMotosMock.TentarMarcarComoVendidoAsync(moto.Id, Arg.Any<CancellationToken>()).Returns(false);

            // Act
            var resultado = await _sut.RegistrarAsync(RequisicaoPara(moto), CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.Conflict);
            resultado.FirstError.Code.ShouldBe("Moto.JaVendido");
            await _repositorioVendasMock.DidNotReceiveWithAnyArgs().InserirAsync(default!, default);
        }

        [Fact(DisplayName = "SUCESSO - Deve listar as vendas do período, incluindo o dia final inteiro")]
        public async Task ListarAsync_WhenPeriodIsInformed_ShouldQueryUtcBoundaries()
        {
            // Arrange
            var vendas = VendaDataBuilder.AsList(3);
            _repositorioVendasMock.ListarPorPeriodoAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns(vendas);

            // Act
            var resultado = await _sut.ListarAsync(new FiltroPeriodo(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)), CancellationToken.None);

            // Assert
            resultado.Select(v => v.Id).ShouldBe(vendas.Select(v => v.Id));
            await _repositorioVendasMock.Received(1).ListarPorPeriodoAsync(
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                Arg.Any<CancellationToken>());
        }
    }
}
