using Concessionaria.Relatorios.Comum.Observabilidade;
using Shouldly;

namespace Concessionaria.Relatorios.tests.Common.Observabilidade
{
    public class TelemetriaTests
    {
        [Fact(DisplayName = "SUCESSO - Duração da planilha deve ter baldes em segundos, com resolução abaixo de 1 s")]
        public void DuracaoGeracaoPlanilha_WhenCreated_ShouldHaveBucketsInSeconds()
        {
            // Arrange & Act
            var limites = Telemetria.DuracaoGeracaoPlanilha.Advice?.HistogramBucketBoundaries;

            // Assert
            limites.ShouldNotBeNull();
            limites.ShouldContain(0.005);
            limites.Count(limite => limite < 1).ShouldBeGreaterThanOrEqualTo(5);
            limites.Max().ShouldBe(10);
        }

        [Fact(DisplayName = "SUCESSO - Linhas por planilha deve ter baldes que separam planilhas vazias e pequenas")]
        public void LinhasPorPlanilha_WhenCreated_ShouldHaveBucketsForSmallCounts()
        {
            // Arrange & Act
            var limites = Telemetria.LinhasPorPlanilha.Advice?.HistogramBucketBoundaries;

            // Assert
            limites.ShouldNotBeNull();
            limites.ShouldContain(0);
            limites.ShouldContain(1);
            limites.ShouldContain(10);
        }
    }
}
