using Concessionaria.Motos.Aplicacao.Features.Vendas.Contratos;
using Shouldly;

namespace Concessionaria.Motos.tests.Validators
{
    /// <summary>Validação de borda do registro de venda: veículo, nome, CPF/CNPJ e valor.</summary>
    public class RequisicaoVendaTests
    {
        private static readonly RequisicaoVenda Valida = new("66f0a1b2c3d4e5f6a7b8c9d0", "Maria Souza", "123.456.789-09", 120_000m);

        [Fact(DisplayName = "SUCESSO - Deve aceitar a venda com CPF formatado")]
        public void Validar_WhenRequestIsValidWithFormattedCpf_ShouldReturnNoErrors()
        {
            // Act
            var erros = Valida.Validar();

            // Assert
            erros.ShouldBeEmpty();
        }

        [Fact(DisplayName = "SUCESSO - Deve aceitar CNPJ com 14 dígitos")]
        public void Validar_WhenDocumentIsCnpj_ShouldReturnNoErrors()
        {
            // Arrange
            var requisicao = Valida with { DocumentoCliente = "12.345.678/0001-95" };

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.ShouldBeEmpty();
        }

        [Theory(DisplayName = "ERRO - Deve recusar documento que não tem 11 nem 14 dígitos")]
        [InlineData("1234567890")]
        [InlineData("123456789012")]
        [InlineData("abc.def.ghi-jk")]
        [InlineData("")]
        [InlineData(null)]
        public void Validar_WhenDocumentIsNotCpfOrCnpj_ShouldReturnDocumentError(string? documento)
        {
            // Arrange
            var requisicao = Valida with { DocumentoCliente = documento! };

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.ShouldHaveSingleItem().Code.ShouldBe("DocumentoCliente");
            erros[0].Description.ShouldBe("Documento do cliente deve ser um CPF (11 dígitos) ou CNPJ (14 dígitos).");
        }

        [Theory(DisplayName = "ERRO - Deve exigir o veículo")]
        [InlineData("")]
        [InlineData("  ")]
        public void Validar_WhenVehicleIsBlank_ShouldReturnVehicleError(string veiculoId)
        {
            // Arrange
            var requisicao = Valida with { VeiculoId = veiculoId };

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.ShouldHaveSingleItem().Code.ShouldBe("VeiculoId");
        }

        [Fact(DisplayName = "ERRO - Deve recusar nome do cliente vazio ou com mais de 120 caracteres")]
        public void Validar_WhenCustomerNameIsBlankOrTooLong_ShouldReturnNameError()
        {
            // Act
            var vazio = (Valida with { NomeCliente = " " }).Validar();
            var longo = (Valida with { NomeCliente = new string('a', 121) }).Validar();

            // Assert
            vazio.ShouldHaveSingleItem().Code.ShouldBe("NomeCliente");
            longo.ShouldHaveSingleItem().Code.ShouldBe("NomeCliente");
            (Valida with { NomeCliente = new string('a', 120) }).Validar().ShouldBeEmpty();
        }

        [Theory(DisplayName = "ERRO - Deve recusar valor de venda zero ou negativo")]
        [InlineData(0)]
        [InlineData(-10)]
        public void Validar_WhenValueIsNotPositive_ShouldReturnValueError(decimal valor)
        {
            // Arrange
            var requisicao = Valida with { Valor = valor };

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.ShouldHaveSingleItem().Code.ShouldBe("Valor");
        }

        [Fact(DisplayName = "ERRO - Deve acumular todos os erros quando todos os campos são inválidos")]
        public void Validar_WhenAllFieldsAreInvalid_ShouldReturnEveryError()
        {
            // Arrange
            var requisicao = new RequisicaoVenda("", "", "123", 0m);

            // Act
            var erros = requisicao.Validar();

            // Assert
            erros.Select(e => e.Code).ShouldBe(["VeiculoId", "NomeCliente", "DocumentoCliente", "Valor"]);
        }
    }
}
