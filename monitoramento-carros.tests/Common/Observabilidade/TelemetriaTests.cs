using Concessionaria.Carros.Comum.Observabilidade;
using Shouldly;

namespace Concessionaria.Carros.tests.Common.Observabilidade
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

        [Fact(DisplayName = "SUCESSO - Tag de tipo de veículo deve ser 'carro', no singular, como usam o dashboard e os alertas")]
        public void TagTipoVeiculo_WhenRead_ShouldBeSingularCarro()
        {
            // Arrange & Act
            var tag = Telemetria.TagTipoVeiculo;

            // Assert
            tag.Key.ShouldBe("tipo_veiculo");
            tag.Value.ShouldBe("carro");
        }
    }
}
