using System.Net;
using Concessionaria.Bff.Aplicacao.Features.Vendas;
using Concessionaria.Bff.Aplicacao.Features.Vendas.Contratos;
using Concessionaria.Bff.Dominio.Enums;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;
using Concessionaria.Bff.tests.Common.Refit;
using Concessionaria.Bff.tests.DataBuilder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Refit;
using Shouldly;

namespace Concessionaria.Bff.tests.Services
{
    /// <summary>Lista unificada de vendas: junção, tipo de origem, ordem por data, repasse do período e falhas.</summary>
    public class ServicoVendasTests
    {
        private static readonly FiltroPeriodo Periodo = new(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        private readonly ICarrosApi _carrosApiMock;
        private readonly IMotosApi _motosApiMock;
        private readonly ServicoVendas _sut;

        /// <summary>Monta o serviço com os clientes de Carros e Motos substituídos.</summary>
        public ServicoVendasTests()
        {
            _carrosApiMock = Substitute.For<ICarrosApi>();
            _motosApiMock = Substitute.For<IMotosApi>();
            _sut = new ServicoVendas(_carrosApiMock, _motosApiMock);
        }

        /// <summary>Configura as vendas devolvidas por Carros e por Motos.</summary>
        private void Vendas(IApiResponse<List<Venda>> carros, IApiResponse<List<Venda>> motos)
        {
            _carrosApiMock.ListarVendasAsync(Arg.Any<FiltroPeriodo>(), Arg.Any<CancellationToken>()).Returns(carros);
            _motosApiMock.ListarVendasAsync(Arg.Any<FiltroPeriodo>(), Arg.Any<CancellationToken>()).Returns(motos);
        }

        [Fact(DisplayName = "SUCESSO - Deve juntar as vendas dos dois serviços, da mais recente para a mais antiga")]
        public async Task ListarAsync_WhenBothServicesRespond_ShouldMergeOrderedByDateDescending()
        {
            // Arrange
            var vendaCarroAntiga = VendaDataBuilder.Create().WithVendidaEm(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc)).Build();
            var vendaCarroRecente = VendaDataBuilder.Create().WithVendidaEm(new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc)).Build();
            var vendaMoto = VendaDataBuilder.Create().WithVendidaEm(new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc)).Build();
            Vendas(RespostasApi.Sucesso(new List<Venda> { vendaCarroAntiga, vendaCarroRecente }), RespostasApi.Sucesso(new List<Venda> { vendaMoto }));

            // Act
            var resultado = await _sut.ListarAsync(Periodo, CancellationToken.None);

            // Assert
            var vendas = resultado.ShouldBeOfType<Ok<List<VendaComTipo>>>().Value!;
            vendas.Select(v => v.Id).ShouldBe([vendaCarroRecente.Id, vendaMoto.Id, vendaCarroAntiga.Id]);
        }

        [Fact(DisplayName = "SUCESSO - Deve marcar cada venda com o tipo de veículo do serviço de origem")]
        public async Task ListarAsync_WhenBothServicesRespond_ShouldTagVehicleType()
        {
            // Arrange
            var vendaCarro = VendaDataBuilder.Create().Build();
            var vendaMoto = VendaDataBuilder.Create().Build();
            Vendas(RespostasApi.Sucesso(new List<Venda> { vendaCarro }), RespostasApi.Sucesso(new List<Venda> { vendaMoto }));

            // Act
            var resultado = await _sut.ListarAsync(Periodo, CancellationToken.None);

            // Assert
            var vendas = resultado.ShouldBeOfType<Ok<List<VendaComTipo>>>().Value!;
            vendas.Single(v => v.Id == vendaCarro.Id).TipoVeiculo.ShouldBe(TipoVeiculo.Carro);
            vendas.Single(v => v.Id == vendaMoto.Id).TipoVeiculo.ShouldBe(TipoVeiculo.Moto);
        }

        [Fact(DisplayName = "SUCESSO - Deve copiar todos os dados da venda original")]
        public async Task ListarAsync_WhenSaleIsReturned_ShouldKeepAllFields()
        {
            // Arrange
            var venda = VendaDataBuilder.Create().Build();
            Vendas(RespostasApi.Sucesso(new List<Venda> { venda }), RespostasApi.Sucesso(new List<Venda>()));

            // Act
            var resultado = await _sut.ListarAsync(Periodo, CancellationToken.None);

            // Assert
            resultado.ShouldBeOfType<Ok<List<VendaComTipo>>>().Value!.ShouldHaveSingleItem().ShouldBe(VendaComTipo.De(venda, TipoVeiculo.Carro));
        }

        [Fact(DisplayName = "SUCESSO - Deve repassar o mesmo período para Carros e Motos")]
        public async Task ListarAsync_WhenPeriodIsInformed_ShouldForwardItToBothServices()
        {
            // Arrange
            Vendas(RespostasApi.Sucesso(new List<Venda>()), RespostasApi.Sucesso(new List<Venda>()));

            // Act
            await _sut.ListarAsync(Periodo, CancellationToken.None);

            // Assert
            await _carrosApiMock.Received(1).ListarVendasAsync(Periodo, Arg.Any<CancellationToken>());
            await _motosApiMock.Received(1).ListarVendasAsync(Periodo, Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve repassar o erro de Motos quando só Motos falha")]
        public async Task ListarAsync_WhenMotorcyclesServiceFails_ShouldReturnItsFailure()
        {
            // Arrange
            Vendas(RespostasApi.Sucesso(VendaDataBuilder.AsList(2)), await RespostasApi.ErroHttpAsync<List<Venda>>(HttpStatusCode.InternalServerError));

            // Act
            var resultado = await _sut.ListarAsync(Periodo, CancellationToken.None);

            // Assert
            resultado.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
        }

        [Fact(DisplayName = "ERRO - Deve relançar a falha de rede de Carros para o manipulador de integração")]
        public async Task ListarAsync_WhenCarsServiceIsUnreachable_ShouldRethrow()
        {
            // Arrange
            Vendas(RespostasApi.FalhaRede<List<Venda>>(new HttpRequestException("Conexão recusada")), RespostasApi.Sucesso(new List<Venda>()));

            // Act & Assert
            await Should.ThrowAsync<ApiRequestException>(() => _sut.ListarAsync(Periodo, CancellationToken.None));
        }
    }
}
