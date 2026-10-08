using Carter;
using Concessionaria.Relatorios.Aplicacao;
using Concessionaria.Relatorios.Comum.Observabilidade;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace Concessionaria.Relatorios.tests.Common.Hospedagem
{
    /// <summary>
    /// Servidor em memória com todos os endpoints Carter de Relatórios, os serviços reais e o pipeline de Id de
    /// operação e ProblemDetails do Program.cs, mas sem Serilog/OpenTelemetry, que exportariam para o Collector.
    /// </summary>
    public sealed class ServidorTeste : IAsyncDisposable
    {
        private readonly WebApplication _app;

        /// <summary>Cliente HTTP que fala com o servidor em memória.</summary>
        public HttpClient Cliente { get; private set; } = null!;

        /// <summary>Monta a aplicação com os serviços e todos os endpoints.</summary>
        private ServidorTeste()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AdicionarProblemDetails();
            builder.Services.AdicionarAplicacao();

            _app = builder.Build();
            _app.UseMiddleware<MiddlewareIdOperacao>();
            MapearEndpoints(_app);
        }

        /// <summary>
        /// Mapeia todos os <see cref="ICarterModule"/> da API, como o <c>MapCarter</c> do Program.cs, sem precisar
        /// do <c>AddCarter</c> (que também procuraria validadores e módulos no assembly de testes).
        /// </summary>
        private static void MapearEndpoints(WebApplication app)
        {
            var endpoints = typeof(ExtensoesAplicacao).Assembly.GetTypes()
                .Where(t => typeof(ICarterModule).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

            foreach (var endpoint in endpoints)
                ((ICarterModule)Activator.CreateInstance(endpoint)!).AddRoutes(app);
        }

        /// <summary>Sobe o servidor e cria o <see cref="Cliente"/>.</summary>
        public static async Task<ServidorTeste> IniciarAsync()
        {
            var servidor = new ServidorTeste();
            await servidor._app.StartAsync();
            servidor.Cliente = servidor._app.GetTestClient();
            return servidor;
        }

        /// <summary>Derruba o servidor e libera o cliente.</summary>
        public async ValueTask DisposeAsync()
        {
            Cliente.Dispose();
            await _app.DisposeAsync();
        }
    }
}
