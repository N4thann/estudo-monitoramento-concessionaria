using Carter;
using Concessionaria.Relatorios.Aplicacao;
using Concessionaria.Relatorios.Comum.Observabilidade;

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarSerilog("relatorios");      // logs: console + arquivo + OTLP
builder.AdicionarTelemetria("relatorios");   // traces + métricas (OpenTelemetry)

builder.Services.AddOpenApi();
builder.Services.AdicionarProblemDetails();   // erros padronizados com idOperacao

builder.Services.AdicionarAplicacao();   // serviços de negócio (um por feature)

builder.Services.AddHealthChecks();
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
