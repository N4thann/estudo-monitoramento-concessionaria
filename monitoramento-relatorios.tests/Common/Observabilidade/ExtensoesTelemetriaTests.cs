using Concessionaria.Relatorios.Comum.Observabilidade;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace Concessionaria.Relatorios.tests.Common.Observabilidade
{
    public class ExtensoesTelemetriaTests
    {
        [Theory(DisplayName = "SUCESSO - Deve reconhecer métricas e saúde como rotas de infraestrutura")]
        [InlineData("/metrics")]
        [InlineData("/saude")]
        [InlineData("/metrics/qualquer-coisa")]
        [InlineData("/SAUDE")]
        public void EhRotaInfraestrutura_WhenPathIsMetricsOrHealth_ShouldReturnTrue(string caminho)
        {
            // Act
            var resultado = ExtensoesTelemetria.EhRotaInfraestrutura(new PathString(caminho));

            // Assert
            resultado.ShouldBeTrue();
        }

        [Theory(DisplayName = "SUCESSO - Deve tratar rotas de negócio como uso real, não infraestrutura")]
        [InlineData("/relatorios/indicadores/vendas")]
        [InlineData("/relatorios/planilhas/estoque")]
        [InlineData("/metricsx")]
        [InlineData("/saudeecoisa")]
        [InlineData("/")]
        public void EhRotaInfraestrutura_WhenPathIsBusinessRoute_ShouldReturnFalse(string caminho)
        {
            // Act
            var resultado = ExtensoesTelemetria.EhRotaInfraestrutura(new PathString(caminho));

            // Assert
            resultado.ShouldBeFalse();
        }

        [Fact(DisplayName = "SUCESSO - Deve usar os valores padrão quando a seção Telemetria não existe")]
        public void ObterConfiguracaoTelemetria_WhenSectionIsMissing_ShouldReturnDefaults()
        {
            // Arrange
            var configuracao = new ConfigurationBuilder().Build();

            // Act
            var telemetria = configuracao.ObterConfiguracaoTelemetria();

            // Assert
            telemetria.EndpointOtlp.ShouldBe("http://127.0.0.1:4317");
            telemetria.ExportarTracesNoConsole.ShouldBeFalse();
        }

        [Fact(DisplayName = "SUCESSO - Deve ler endpoint OTLP e exportação no console da seção Telemetria")]
        public void ObterConfiguracaoTelemetria_WhenSectionIsPresent_ShouldReadValues()
        {
            // Arrange
            var configuracao = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telemetria:EndpointOtlp"] = "http://collector:4317",
                    ["Telemetria:ExportarTracesNoConsole"] = "true"
                })
                .Build();

            // Act
            var telemetria = configuracao.ObterConfiguracaoTelemetria();

            // Assert
            telemetria.EndpointOtlp.ShouldBe("http://collector:4317");
            telemetria.ExportarTracesNoConsole.ShouldBeTrue();
        }
    }
}
