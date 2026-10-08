using Concessionaria.Motos.Aplicacao.Comum;
using Shouldly;

namespace Concessionaria.Motos.tests.Common
{
    /// <summary>Conversão do período em datas (DateOnly) para o intervalo UTC consultado no Mongo.</summary>
    public class FiltroPeriodoTests
    {
        [Fact(DisplayName = "SUCESSO - Deve começar o período à meia-noite UTC da data inicial")]
        public void ObterInicioUtc_WhenStartDateIsInformed_ShouldReturnMidnightUtc()
        {
            // Arrange
            var filtro = new FiltroPeriodo(new DateOnly(2026, 9, 1), null);

            // Act
            var inicio = filtro.ObterInicioUtc();

            // Assert
            inicio.ShouldBe(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
            inicio!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        }

        [Fact(DisplayName = "SUCESSO - Deve incluir o dia final inteiro, terminando na meia-noite do dia seguinte")]
        public void ObterFimExclusivoUtc_WhenEndDateIsInformed_ShouldReturnNextDayMidnightUtc()
        {
            // Arrange
            var filtro = new FiltroPeriodo(null, new DateOnly(2026, 9, 30));

            // Act
            var fim = filtro.ObterFimExclusivoUtc();

            // Assert
            fim.ShouldBe(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
            fim!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        }

        [Fact(DisplayName = "SUCESSO - Deve deixar o período aberto quando as datas não são informadas")]
        public void ObterLimites_WhenDatesAreMissing_ShouldReturnNull()
        {
            // Arrange
            var filtro = new FiltroPeriodo(null, null);

            // Act & Assert
            filtro.ObterInicioUtc().ShouldBeNull();
            filtro.ObterFimExclusivoUtc().ShouldBeNull();
        }
    }
}
