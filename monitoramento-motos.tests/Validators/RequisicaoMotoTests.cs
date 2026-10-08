using Concessionaria.Motos.Aplicacao.Features.Motos.Contratos;
using ErrorOr;
using Shouldly;

namespace Concessionaria.Motos.tests.Validators
{
    /// <summary>Validação de borda do cadastro e da edição de motos.</summary>
    public class RequisicaoMotoTests
    {
        private static readonly RequisicaoMoto Valida = new("Toyota", "Corolla", 2024, "Prata", 160, 150_000m);

        [Fact(DisplayName = "SUCESSO - Deve aceitar a requisição quando todos os campos são válidos")]
        public void Validar_WhenRequestIsValid_ShouldReturnNoErrors()
        {
            // Act
            var erros = Valida.Validar();

            // Assert
            erros.ShouldBeEmpty();
        }

        [Fact(DisplayName = "SUCESSO - Deve aceitar os limites de ano, cilindrada e tamanho de texto")]
        public void Validar_WhenValuesAreAtTheLimits_ShouldReturnNoErrors()
        {
            // Arrange
            var requisicao = new RequisicaoMoto(new string('a', 60), new string('b', 60), DateTime.UtcNow.Year + 1, new string('c', 30), 2500, 0.01m);

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.ShouldBeEmpty();
            (Valida with { Ano = 1950, Cilindrada = 50 }).Validar().ShouldBeEmpty();
        }

        [Theory(DisplayName = "ERRO - Deve exigir marca, modelo e cor preenchidos")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Validar_WhenTextFieldsAreBlank_ShouldReturnErrorForEachField(string? texto)
        {
            // Arrange
            var requisicao = Valida with { Marca = texto!, Modelo = texto!, Cor = texto! };

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.Select(e => e.Code).ShouldBe(["Marca", "Modelo", "Cor"]);
            erros.ShouldAllBe(e => e.Type == ErrorType.Validation);
        }

        [Fact(DisplayName = "ERRO - Deve recusar marca, modelo e cor acima do tamanho máximo")]
        public void Validar_WhenTextFieldsAreTooLong_ShouldReturnErrorForEachField()
        {
            // Arrange
            var requisicao = Valida with { Marca = new string('a', 61), Modelo = new string('b', 61), Cor = new string('c', 31) };

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.Select(e => e.Code).ShouldBe(["Marca", "Modelo", "Cor"]);
        }

        [Theory(DisplayName = "ERRO - Deve recusar ano fora do intervalo de 1950 até o próximo ano")]
        [InlineData(1949)]
        [InlineData(0)]
        public void Validar_WhenYearIsTooOld_ShouldReturnYearError(int ano)
        {
            // Arrange
            var requisicao = Valida with { Ano = ano };

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.ShouldHaveSingleItem().Code.ShouldBe("Ano");
            erros[0].Description.ShouldBe($"Ano deve estar entre 1950 e {DateTime.UtcNow.Year + 1}.");
        }

        [Fact(DisplayName = "ERRO - Deve recusar ano posterior ao próximo ano")]
        public void Validar_WhenYearIsInTheFuture_ShouldReturnYearError()
        {
            // Arrange
            var requisicao = Valida with { Ano = DateTime.UtcNow.Year + 2 };

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.ShouldHaveSingleItem().Code.ShouldBe("Ano");
        }

        [Theory(DisplayName = "ERRO - Deve recusar cilindrada fora de 50 a 2500 cc")]
        [InlineData(49)]
        [InlineData(2501)]
        public void Validar_WhenDisplacementIsOutOfRange_ShouldReturnDisplacementError(int cilindrada)
        {
            // Arrange
            var requisicao = Valida with { Cilindrada = cilindrada };

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.ShouldHaveSingleItem().Code.ShouldBe("Cilindrada");
        }

        [Theory(DisplayName = "ERRO - Deve recusar preço zero ou negativo")]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validar_WhenPriceIsNotPositive_ShouldReturnPriceError(decimal preco)
        {
            // Arrange
            var requisicao = Valida with { Preco = preco };

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.ShouldHaveSingleItem().Code.ShouldBe("Preco");
            erros[0].Description.ShouldBe("Preço deve ser maior que zero.");
        }
    }
}
