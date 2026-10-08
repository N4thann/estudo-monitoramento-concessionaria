using System.Net;
using Concessionaria.Bff.Comum.Observabilidade;
using Concessionaria.Bff.Infraestrutura;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;
using Concessionaria.Bff.tests.Fakes.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly.CircuitBreaker;
using Shouldly;

namespace Concessionaria.Bff.tests.Infraestrutura
{
    /// <summary>
    /// Resiliência dos clientes de Carros e Motos com o registro real (<see cref="ExtensoesInfraestrutura.AdicionarIntegracoes"/>),
    /// trocando só a rede por <see cref="HandlerRespostasProgramadas"/>: retry e circuit breaker só por falha de transporte,
    /// sem retry em POST, e o Id de operação propagado.
    /// </summary>
    public class ExtensoesInfraestruturaTests
    {
        /// <summary>Monta os clientes como no Program.cs, com a rede de Carros substituída e retry sem espera entre tentativas.</summary>
        private static ServiceProvider MontarProvedor(HandlerRespostasProgramadas redeCarros)
        {
            var configuracao = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Integracoes:CarrosApi"] = "http://carros/",
                    ["Integracoes:MotosApi"] = "http://motos/"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMetrics();
            services.AdicionarIntegracoes(configuracao);
            services.AddHttpClient("carros").ConfigurePrimaryHttpMessageHandler(() => redeCarros);
            // Só o intervalo entre tentativas muda (o padrão é 2 s com backoff); quantidade e regras ficam as de produção.
            services.Configure<HttpStandardResilienceOptions>("carros-standard", opcoes => opcoes.Retry.Delay = TimeSpan.Zero);

            return services.BuildServiceProvider();
        }

        [Fact(DisplayName = "SUCESSO - Deve repetir até 3 vezes um GET que falha por conexão recusada")]
        public async Task Get_WhenConnectionIsRefused_ShouldRetryThreeTimes()
        {
            // Arrange
            var rede = new HandlerRespostasProgramadas(_ => throw new HttpRequestException("Conexão recusada"));
            await using var provedor = MontarProvedor(rede);
            var api = provedor.GetRequiredService<ICarrosApi>();

            // Act
            var resposta = await api.ObterResumoAsync(CancellationToken.None);

            // Assert
            rede.Chamadas.ShouldBe(4);
            resposta.StatusCode.ShouldBeNull();
            resposta.Error!.InnerException.ShouldBeOfType<HttpRequestException>();
        }

        [Fact(DisplayName = "SUCESSO - Não deve repetir quando Carros responde 5xx, para não multiplicar tentativas em cascata")]
        public async Task Get_WhenServiceReturnsServerError_ShouldNotRetry()
        {
            // Arrange
            var rede = new HandlerRespostasProgramadas(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
            await using var provedor = MontarProvedor(rede);
            var api = provedor.GetRequiredService<ICarrosApi>();

            // Act
            var resposta = await api.GerarPlanilhaEstoqueAsync(CancellationToken.None);

            // Assert
            rede.Chamadas.ShouldBe(1);
            resposta.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        }

        [Fact(DisplayName = "SUCESSO - Não deve repetir POST de venda, mesmo com falha de transporte, para não vender duas vezes")]
        public async Task Post_WhenConnectionFails_ShouldNotRetry()
        {
            // Arrange
            var rede = new HandlerRespostasProgramadas(_ => throw new HttpRequestException("Conexão recusada"));
            await using var provedor = MontarProvedor(rede);
            var api = provedor.GetRequiredService<ICarrosApi>();

            // Act
            var resposta = await api.RegistrarVendaAsync(new RequisicaoVenda("abc", "Maria", "12345678909", 1m), CancellationToken.None);

            // Assert
            rede.Chamadas.ShouldBe(1);
            resposta.StatusCode.ShouldBeNull();
        }

        [Fact(DisplayName = "SUCESSO - Não deve abrir o circuito por 5xx: rotas saudáveis continuam passando com Relatórios fora")]
        public async Task CircuitBreaker_WhenServiceReturnsManyServerErrors_ShouldStayClosed()
        {
            // Arrange
            var rede = new HandlerRespostasProgramadas(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
            await using var provedor = MontarProvedor(rede);
            var api = provedor.GetRequiredService<ICarrosApi>();
            const int quantidade = 120;

            // Act
            for (var i = 0; i < quantidade; i++)
                await api.GerarPlanilhaEstoqueAsync(CancellationToken.None);

            // Assert
            rede.Chamadas.ShouldBe(quantidade);
        }

        [Fact(DisplayName = "ERRO - Deve abrir o circuito quando Carros fica inalcançável e parar de chamar a rede")]
        public async Task CircuitBreaker_WhenServiceIsUnreachable_ShouldOpen()
        {
            // Arrange
            var rede = new HandlerRespostasProgramadas(_ => throw new HttpRequestException("Conexão recusada"));
            await using var provedor = MontarProvedor(rede);
            var api = provedor.GetRequiredService<ICarrosApi>();
            const int quantidade = 40;

            // Act
            Refit.IApiResponse<Resumo>? ultima = null;
            for (var i = 0; i < quantidade; i++)
                ultima = await api.ObterResumoAsync(CancellationToken.None);

            // Assert
            ultima!.Error!.InnerException.ShouldBeOfType<BrokenCircuitException>();
            rede.Chamadas.ShouldBeLessThan(quantidade * 4);
        }

        [Fact(DisplayName = "SUCESSO - Deve propagar o Id de operação atual para Carros")]
        public async Task Get_WhenThereIsCurrentOperationId_ShouldSendHeader()
        {
            // Arrange
            var rede = new HandlerRespostasProgramadas(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
            await using var provedor = MontarProvedor(rede);
            var api = provedor.GetRequiredService<ICarrosApi>();
            ContextoOperacao.IdAtual = "id-do-front";

            // Act
            await api.ExcluirAsync("abc", CancellationToken.None);

            // Assert
            rede.Recebidas.ShouldHaveSingleItem().Headers.GetValues(ContextoOperacao.Cabecalho).ShouldBe(["id-do-front"]);
        }
    }
}
