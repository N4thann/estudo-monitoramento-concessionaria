using Concessionaria.Bff.Comum.Observabilidade;
using Shouldly;

namespace Concessionaria.Bff.tests.Common.Observabilidade
{
    /// <summary>Instrumentos de <see cref="Telemetria"/> que não dependem de um serviço para medir.</summary>
    [Collection(ColecaoTelemetria.Nome)]
    public class TelemetriaTests
    {
        [Fact(DisplayName = "SUCESSO - Deve publicar o limite de memória do processo, base do alerta MemoriaAlta")]
        public void CriarMedidorLimiteMemoria_WhenObserved_ShouldReportPositiveLimit()
        {
            // Arrange
            using var coletor = new ColetorTelemetria();
            Telemetria.CriarMedidorLimiteMemoria();

            // Act
            coletor.ObservarInstrumentos();

            // Assert
            var medicoes = coletor.Medicoes("concessionaria.processo.memoria_limite");
            medicoes.ShouldNotBeEmpty();
            medicoes.ShouldAllBe(m => m.Valor > 0);
        }

        [Fact(DisplayName = "SUCESSO - Contador de painel degradado deve ter o nome usado pelo alerta PainelDegradado")]
        public void PainelDegradado_WhenIncremented_ShouldUseNameExpectedByAlert()
        {
            // Arrange
            using var coletor = new ColetorTelemetria();

            // Act
            Telemetria.PainelDegradado.Add(1);

            // Assert
            Telemetria.PainelDegradado.Name.ShouldBe("concessionaria.painel.degradado");
            coletor.Medicoes("concessionaria.painel.degradado").ShouldHaveSingleItem().Valor.ShouldBe(1);
        }
    }
}
