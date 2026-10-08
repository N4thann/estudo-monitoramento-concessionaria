using System.Text.Json.Serialization;
using Carter;
using Concessionaria.Motos.Aplicacao;
using Concessionaria.Motos.Comum;
using Concessionaria.Motos.Comum.Observabilidade;
using Concessionaria.Motos.Dominio.Interfaces;
using Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Concessionaria.Motos.tests.Common.Hospedagem
{
    /// <summary>
    /// Servidor em memória com todos os endpoints Carter de Motos e o mesmo pipeline de erros do Program.cs
    /// (Id de operação, ProblemDetails, manipulador de falha de integração), mas sem Serilog/OpenTelemetry,
    /// que exportariam para o Collector. Repositórios e cliente de Relatórios são substitutos.
    /// </summary>
    public sealed class ServidorTeste : IAsyncDisposable
    {
        private readonly WebApplication _app;

        /// <summary>Substituto do repositório de motos usado pelos serviços.</summary>
        public IRepositorioMotos RepositorioMotos { get; } = Substitute.For<IRepositorioMotos>();

        /// <summary>Substituto do repositório de vendas usado pelos serviços.</summary>
        public IRepositorioVendas RepositorioVendas { get; } = Substitute.For<IRepositorioVendas>();

        /// <summary>Substituto do cliente Refit da API de Relatórios.</summary>
        public IRelatoriosApi RelatoriosApi { get; } = Substitute.For<IRelatoriosApi>();

        /// <summary>Cliente HTTP que fala com o servidor em memória.</summary>
        public HttpClient Cliente { get; private set; } = null!;

        /// <summary>Monta a aplicação com os serviços reais, as dependências externas substituídas e todos os endpoints.</summary>
        private ServidorTeste()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AdicionarProblemDetails();
            builder.Services.AddExceptionHandler<ManipuladorFalhaIntegracao>();
            builder.Services.ConfigureHttpJsonOptions(opcoes =>
                opcoes.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.AddSingleton(RepositorioMotos);
            builder.Services.AddSingleton(RepositorioVendas);
            builder.Services.AddSingleton(RelatoriosApi);
            builder.Services.AdicionarAplicacao();

            _app = builder.Build();
            _app.UseMiddleware<MiddlewareIdOperacao>();
            _app.UseExceptionHandler();
            _app.UseStatusCodePages();
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
