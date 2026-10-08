using System.Text.Json.Serialization;
using Carter;
using Concessionaria.Motos.Aplicacao;
using Concessionaria.Motos.Comum;
using Concessionaria.Motos.Comum.Observabilidade;
using Concessionaria.Motos.Infraestrutura;
using Concessionaria.Motos.Infraestrutura.Banco;

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarSerilog("motos");
builder.AdicionarTelemetria("motos");

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
