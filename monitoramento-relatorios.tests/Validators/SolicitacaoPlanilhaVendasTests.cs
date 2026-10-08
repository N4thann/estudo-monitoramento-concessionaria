using Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Contratos;
using Concessionaria.Relatorios.tests.DataBuilder;
using Shouldly;

namespace Concessionaria.Relatorios.tests.Validators
{
    public class SolicitacaoPlanilhaVendasTests
    {
        private static readonly DateOnly De = new(2026, 9, 1);
        private static readonly DateOnly Ate = new(2026, 9, 30);

        [Fact(DisplayName = "SUCESSO - Deve aceitar a solicitação quando todos os campos são válidos")]
        public void Validar_WhenRequestIsValid_ShouldReturnNoErrors()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", De, Ate, VendaRelatorioDataBuilder.AsList(3));

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.ShouldBeEmpty();
        }

        [Fact(DisplayName = "SUCESSO - Deve aceitar lista de vendas vazia")]
        public void Validar_WhenSalesListIsEmpty_ShouldReturnNoErrors()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Motos", De, Ate, []);

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.ShouldBeEmpty();
        }

        [Fact(DisplayName = "SUCESSO - Deve aceitar período de um único dia")]
        public void Validar_WhenStartDateEqualsEndDate_ShouldReturnNoErrors()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", De, De, []);

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.ShouldBeEmpty();
        }

        [Theory(DisplayName = "ERRO - Deve exigir o tipo de veículo")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Validar_WhenVehicleTypeIsBlank_ShouldReturnVehicleTypeError(string? tipoVeiculo)
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas(tipoVeiculo!, De, Ate, []);

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.Keys.ShouldBe([nameof(SolicitacaoPlanilhaVendas.TipoVeiculo)]);
            erros[nameof(SolicitacaoPlanilhaVendas.TipoVeiculo)].ShouldBe(["Tipo de veículo é obrigatório."]);
        }

        [Fact(DisplayName = "ERRO - Deve recusar período com data inicial posterior à final")]
        public void Validar_WhenStartDateIsAfterEndDate_ShouldReturnPeriodError()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", Ate, De, []);

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.Keys.ShouldBe(["Periodo"]);
            erros["Periodo"].ShouldBe(["A data inicial deve ser anterior ou igual à final."]);
        }

        [Fact(DisplayName = "ERRO - Deve exigir a lista de vendas")]
        public void Validar_WhenSalesListIsNull_ShouldReturnSalesError()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", De, Ate, null);

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.Keys.ShouldBe([nameof(SolicitacaoPlanilhaVendas.Vendas)]);
            erros[nameof(SolicitacaoPlanilhaVendas.Vendas)].ShouldBe(["A lista de vendas é obrigatória (pode ser vazia)."]);
        }

        [Fact(DisplayName = "ERRO - Deve acumular todos os erros quando vários campos são inválidos")]
        public void Validar_WhenAllFieldsAreInvalid_ShouldReturnEveryError()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("", Ate, De, null);

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.Keys.ShouldBe([nameof(SolicitacaoPlanilhaVendas.TipoVeiculo), "Periodo", nameof(SolicitacaoPlanilhaVendas.Vendas)], ignoreOrder: true);
        }
    }
}
