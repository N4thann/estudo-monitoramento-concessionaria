using System.Text.Json.Serialization;
using Carter;
using Concessionaria.Bff.Aplicacao;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Comum.Observabilidade;
using Concessionaria.Bff.Infraestrutura;

const string PoliticaCorsFrontend = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarSerilog("bff");
builder.AdicionarTelemetria("bff");

builder.Services.AddOpenApi();
builder.Services.AdicionarProblemDetails();
builder.Services.AddExceptionHandler<ManipuladorFalhaIntegracao>();
builder.Services.ConfigureHttpJsonOptions(opcoes =>
    opcoes.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// CORS só no BFF: é a única API acessada pelo navegador; Carros, Motos e Relatórios são internas.
var origensPermitidas = builder.Configuration.GetSection("Cors:OrigensPermitidas").Get<string[]>() ?? [];
builder.Services.AddCors(opcoes => opcoes.AddPolicy(PoliticaCorsFrontend, politica => politica
    .WithOrigins(origensPermitidas)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Content-Disposition", ContextoOperacao.Cabecalho)));

builder.Services.AdicionarIntegracoes(builder.Configuration);
builder.Services.AdicionarAplicacao();

builder.Services.AddHealthChecks();
builder.Services.AddCarter();

var app = builder.Build();

app.UsarObservabilidade();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(PoliticaCorsFrontend);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks(ExtensoesTelemetria.RotaSaude);
app.MapearMetricas();
app.MapCarter();

app.Run();
