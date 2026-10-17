using System.Text.Json.Serialization;
using Carter;
using Concessionaria.Carros.Aplicacao;
using Concessionaria.Carros.Comum;
using Concessionaria.Carros.Comum.Observabilidade;
using Concessionaria.Carros.Infraestrutura;
using Concessionaria.Carros.Infraestrutura.Banco;

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarSerilog("carros");      // logs: console + arquivo + OTLP
builder.AdicionarTelemetria("carros");   // traces + métricas (OpenTelemetry)

builder.Services.AddOpenApi();
builder.Services.AdicionarProblemDetails();                           // erros padronizados com idOperacao
builder.Services.AddExceptionHandler<ManipuladorFalhaIntegracao>();   // falha de outro serviço → 502/503/504
builder.Services.ConfigureHttpJsonOptions(opcoes =>
    opcoes.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AdicionarMongoDb(builder.Configuration);       // cliente Mongo com tracing
builder.Services.AdicionarIntegracoes(builder.Configuration);   // Refit + resiliência + propagação
builder.Services.AdicionarAplicacao();                          // serviços de negócio (um por feature)

builder.Services.AddHealthChecks().AddCheck<VerificacaoSaudeMongoDb>("mongodb");
builder.Services.AddCarter();

var app = builder.Build();

app.UsarObservabilidade();   // 1º middleware: Id de operação + log de requisição
app.UseExceptionHandler();   // captura exceções não tratadas
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks(ExtensoesTelemetria.RotaSaude);   // GET /saude
app.MapearMetricas();                                 // GET /metrics (Prometheus)
app.MapCarter();                                      // endpoints de negócio

app.Run();
