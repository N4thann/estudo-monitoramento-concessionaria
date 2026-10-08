using Carter;
using Concessionaria.Relatorios.Aplicacao;
using Concessionaria.Relatorios.Comum.Observabilidade;

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarSerilog("relatorios");
builder.AdicionarTelemetria("relatorios");

builder.Services.AddOpenApi();
builder.Services.AdicionarProblemDetails();

builder.Services.AdicionarAplicacao();

builder.Services.AddHealthChecks();
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
