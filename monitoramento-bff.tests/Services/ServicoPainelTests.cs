using System.Net;
using Concessionaria.Bff.Aplicacao.Features.Painel;
using Concessionaria.Bff.Aplicacao.Features.Painel.Contratos;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;
using Concessionaria.Bff.tests.Common.Observabilidade;
using Concessionaria.Bff.tests.Common.Refit;
using Concessionaria.Bff.tests.DataBuilder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Refit;
using Shouldly;

namespace Concessionaria.Bff.tests.Services
{
    /// <summary>
    /// Agregação do painel: soma dos resumos, degradação sem indicadores (Relatórios fora) e falha de Carros ou Motos.
    /// Fica na coleção de telemetria porque mede o contador estático de painel degradado.
    /// </summary>
    [Collection(ColecaoTelemetria.Nome)]
    public class ServicoPainelTests
    {
        private readonly ICarrosApi _carrosApiMock;
        private readonly IMotosApi _motosApiMock;
        private readonly ServicoPainel _sut;

        /// <summary>Monta o serviço com os clientes de Carros e Motos substituídos.</summary>
        public ServicoPainelTests()
        {
            _carrosApiMock = Substitute.For<ICarrosApi>();
            _motosApiMock = Substitute.For<IMotosApi>();
            _sut = new ServicoPainel(_carrosApiMock, _motosApiMock);
        }

        /// <summary>Configura as respostas do resumo de Carros e de Motos.</summary>
        private void Resumos(IApiResponse<Resumo> carros, IApiResponse<Resumo> motos)
        {
            _carrosApiMock.ObterResumoAsync(Arg.Any<CancellationToken>()).Returns(carros);
            _motosApiMock.ObterResumoAsync(Arg.Any<CancellationToken>()).Returns(motos);
        }

        [Fact(DisplayName = "SUCESSO - Deve somar estoque, vendas e receita de Carros e Motos no total")]
        public async Task ObterAsync_WhenBothServicesRespond_ShouldSumSummaries()
        {
            // Arrange
            var carros = ResumoDataBuilder.Create().WithEstoque(disponiveis: 5, vendidos: 3, valorEmEstoque: 500_000m).WithIndicadores(3, 300_000m).Build();
            var motos = ResumoDataBuilder.Create().WithEstoque(disponiveis: 7, vendidos: 2, valorEmEstoque: 200_000m).WithIndicadores(2, 50_000m).Build();
            Resumos(RespostasApi.Sucesso(carros), RespostasApi.Sucesso(motos));

            // Act
            var resultado = await _sut.ObterAsync(CancellationToken.None);

            // Assert
            var painel = resultado.ShouldBeOfType<Ok<RespostaPainel>>().Value!;
            painel.Carros.ShouldBe(carros);
            painel.Motos.ShouldBe(motos);
            painel.Total.ShouldBe(new ResumoGeral(TotalVeiculos: 17, Disponiveis: 12, Vendidos: 5, ValorEmEstoque: 700_000m, QuantidadeVendas: 5, Receita: 350_000m));
        }

        [Fact(DisplayName = "SUCESSO - Não deve contar painel degradado quando os dois resumos têm indicadores")]
        public async Task ObterAsync_WhenBothHaveIndicators_ShouldNotCountDegradedPanel()
        {
            // Arrange
            using var coletor = new ColetorTelemetria();
            Resumos(RespostasApi.Sucesso(ResumoDataBuilder.Create().Build()), RespostasApi.Sucesso(ResumoDataBuilder.Create().Build()));

            // Act
            await _sut.ObterAsync(CancellationToken.None);

            // Assert
            coletor.Medicoes("concessionaria.painel.degradado").ShouldBeEmpty();
        }

        [Fact(DisplayName = "SUCESSO - Deve entregar o painel sem vendas e receita no total quando um resumo vem sem indicadores")]
        public async Task ObterAsync_WhenOneSummaryHasNoIndicators_ShouldReturnTotalWithoutSales()
        {
            // Arrange
            var carros = ResumoDataBuilder.Create().SemIndicadores().Build();
            var motos = ResumoDataBuilder.Create().Build();
            Resumos(RespostasApi.Sucesso(carros), RespostasApi.Sucesso(motos));

            // Act
            var resultado = await _sut.ObterAsync(CancellationToken.None);

            // Assert
            var total = resultado.ShouldBeOfType<Ok<RespostaPainel>>().Value!.Total;
            total.QuantidadeVendas.ShouldBeNull();
            total.Receita.ShouldBeNull();
            total.TotalVeiculos.ShouldBe(carros.TotalVeiculos + motos.TotalVeiculos);
        }

        [Fact(DisplayName = "SUCESSO - Deve contar o painel degradado quando Relatórios está fora")]
        public async Task ObterAsync_WhenIndicatorsAreMissing_ShouldCountDegradedPanel()
        {
            // Arrange
            using var coletor = new ColetorTelemetria();
            Resumos(RespostasApi.Sucesso(ResumoDataBuilder.Create().Build()), RespostasApi.Sucesso(ResumoDataBuilder.Create().SemIndicadores().Build()));

            // Act
            await _sut.ObterAsync(CancellationToken.None);

            // Assert
            coletor.Medicoes("concessionaria.painel.degradado").ShouldHaveSingleItem().Valor.ShouldBe(1);
        }

        [Fact(DisplayName = "ERRO - Deve devolver a falha de Carros como 502 quando o resumo dele dá 5xx")]
        public async Task ObterAsync_WhenCarsServiceFails_ShouldReturnBadGateway()
        {
            // Arrange
            Resumos(await RespostasApi.ErroHttpAsync<Resumo>(HttpStatusCode.InternalServerError), RespostasApi.Sucesso(ResumoDataBuilder.Create().Build()));

            // Act
            var resultado = await _sut.ObterAsync(CancellationToken.None);

            // Assert
            resultado.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
        }

        [Fact(DisplayName = "ERRO - Deve devolver a falha de Motos quando só Motos falha")]
        public async Task ObterAsync_WhenMotorcyclesServiceFails_ShouldReturnItsFailure()
        {
            // Arrange
            Resumos(RespostasApi.Sucesso(ResumoDataBuilder.Create().Build()), await RespostasApi.ErroHttpAsync<Resumo>(HttpStatusCode.ServiceUnavailable));

            // Act
            var resultado = await _sut.ObterAsync(CancellationToken.None);

            // Assert
            resultado.ShouldBeOfType<ProblemHttpResult>().ProblemDetails.Title.ShouldBe("Serviço dependente respondeu com erro 503.");
        }

        [Fact(DisplayName = "ERRO - Deve relançar a falha de rede para virar 502/503/504 no manipulador de integração")]
        public async Task ObterAsync_WhenServiceIsUnreachable_ShouldRethrow()
        {
            // Arrange
            Resumos(RespostasApi.FalhaRede<Resumo>(new HttpRequestException("Conexão recusada")), RespostasApi.Sucesso(ResumoDataBuilder.Create().Build()));

            // Act & Assert
            await Should.ThrowAsync<ApiRequestException>(() => _sut.ObterAsync(CancellationToken.None));
        }

        [Fact(DisplayName = "SUCESSO - Deve pedir os dois resumos ao mesmo tempo, antes de esperar qualquer um")]
        public async Task ObterAsync_WhenCalled_ShouldRequestBothSummariesConcurrently()
        {
            // Arrange
            var carrosPendente = new TaskCompletionSource<IApiResponse<Resumo>>();
            _carrosApiMock.ObterResumoAsync(Arg.Any<CancellationToken>()).Returns(carrosPendente.Task);
            _motosApiMock.ObterResumoAsync(Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(ResumoDataBuilder.Create().Build()));

            // Act
            var execucao = _sut.ObterAsync(CancellationToken.None);

            // Assert
            await _motosApiMock.Received(1).ObterResumoAsync(Arg.Any<CancellationToken>());
            carrosPendente.SetResult(RespostasApi.Sucesso(ResumoDataBuilder.Create().Build()));
            (await execucao).ShouldBeOfType<Ok<RespostaPainel>>();
        }
    }
}
