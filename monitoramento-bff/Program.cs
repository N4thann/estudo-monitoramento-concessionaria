using System.Text.Json.Serialization;
using Carter;
using Concessionaria.Bff.Aplicacao;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Comum.Observabilidade;
using Concessionaria.Bff.Infraestrutura;

const string PoliticaCorsFrontend = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarSerilog("bff");      // logs: console + arquivo + OTLP
builder.AdicionarTelemetria("bff");   // traces + métricas (OpenTelemetry)

builder.Services.AddOpenApi();
builder.Services.AdicionarProblemDetails();                           // erros padronizados com idOperacao
builder.Services.AddExceptionHandler<ManipuladorFalhaIntegracao>();   // falha de outro serviço → 502/503/504
builder.Services.ConfigureHttpJsonOptions(opcoes =>
    opcoes.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// CORS só no BFF: é a única API acessada pelo navegador; Carros, Motos e Relatórios são internas.
var origensPermitidas = builder.Configuration.GetSection("Cors:OrigensPermitidas").Get<string[]>() ?? [];
builder.Services.AddCors(opcoes => opcoes.AddPolicy(PoliticaCorsFrontend, politica => politica
    .WithOrigins(origensPermitidas)   // http://localhost:5173
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Content-Disposition", ContextoOperacao.Cabecalho)));

builder.Services.AdicionarIntegracoes(builder.Configuration);   // Refit + resiliência + propagação
builder.Services.AdicionarAplicacao();                          // serviços de negócio (um por feature)

builder.Services.AddHealthChecks();
builder.Services.AddCarter();

var app = builder.Build();

app.UsarObservabilidade();   // 1º middleware: Id de operação + log de requisição
app.UseExceptionHandler();   // captura exceções não tratadas
app.UseStatusCodePages();
app.UseCors(PoliticaCorsFrontend);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks(ExtensoesTelemetria.RotaSaude);   // GET /saude
app.MapearMetricas();                                 // GET /metrics (Prometheus)
app.MapCarter();                                      // endpoints de negócio

app.Run();
