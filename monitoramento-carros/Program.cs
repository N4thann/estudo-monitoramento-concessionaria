using System.Text.Json.Serialization;
using Carter;
using Concessionaria.Carros.Aplicacao;
using Concessionaria.Carros.Comum;
using Concessionaria.Carros.Comum.Observabilidade;
using Concessionaria.Carros.Infraestrutura;
using Concessionaria.Carros.Infraestrutura.Banco;

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarSerilog("carros");
builder.AdicionarTelemetria("carros");

builder.Services.AddOpenApi();
builder.Services.AdicionarProblemDetails();
builder.Services.AddExceptionHandler<ManipuladorFalhaIntegracao>();
builder.Services.ConfigureHttpJsonOptions(opcoes =>
    opcoes.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AdicionarMongoDb(builder.Configuration);
builder.Services.AdicionarIntegracoes(builder.Configuration);
builder.Services.AdicionarAplicacao();

builder.Services.AddHealthChecks().AddCheck<VerificacaoSaudeMongoDb>("mongodb");
builder.Services.AddCarter();

var app = builder.Build();

app.UsarObservabilidade();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks(ExtensoesTelemetria.RotaSaude);
app.MapearMetricas();
app.MapCarter();

app.Run();
