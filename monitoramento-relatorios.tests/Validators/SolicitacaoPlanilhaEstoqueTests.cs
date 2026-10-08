using Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Contratos;
using Concessionaria.Relatorios.tests.DataBuilder;
using Shouldly;

namespace Concessionaria.Relatorios.tests.Validators
{
    public class SolicitacaoPlanilhaEstoqueTests
    {
        [Fact(DisplayName = "SUCESSO - Deve aceitar a solicitação quando todos os campos são válidos")]
        public void Validar_WhenRequestIsValid_ShouldReturnNoErrors()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Carros", "Portas", VeiculoRelatorioDataBuilder.AsList(3));

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.ShouldBeEmpty();
        }

        [Fact(DisplayName = "SUCESSO - Deve aceitar lista de veículos vazia")]
        public void Validar_WhenVehicleListIsEmpty_ShouldReturnNoErrors()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Motos", "Cilindrada", []);

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
            var solicitacao = new SolicitacaoPlanilhaEstoque(tipoVeiculo!, "Portas", []);

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.Keys.ShouldBe([nameof(SolicitacaoPlanilhaEstoque.TipoVeiculo)]);
            erros[nameof(SolicitacaoPlanilhaEstoque.TipoVeiculo)].ShouldBe(["Tipo de veículo é obrigatório."]);
        }

        [Theory(DisplayName = "ERRO - Deve exigir o rótulo do detalhe do veículo")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Validar_WhenDetailLabelIsBlank_ShouldReturnDetailLabelError(string? rotuloDetalhe)
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Carros", rotuloDetalhe!, []);

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.Keys.ShouldBe([nameof(SolicitacaoPlanilhaEstoque.RotuloDetalhe)]);
            erros[nameof(SolicitacaoPlanilhaEstoque.RotuloDetalhe)].ShouldBe(["Rótulo do detalhe do veículo é obrigatório."]);
        }

        [Fact(DisplayName = "ERRO - Deve exigir a lista de veículos")]
        public void Validar_WhenVehicleListIsNull_ShouldReturnVehiclesError()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Carros", "Portas", null);

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.Keys.ShouldBe([nameof(SolicitacaoPlanilhaEstoque.Veiculos)]);
            erros[nameof(SolicitacaoPlanilhaEstoque.Veiculos)].ShouldBe(["A lista de veículos é obrigatória (pode ser vazia)."]);
        }

        [Fact(DisplayName = "ERRO - Deve acumular todos os erros quando vários campos são inválidos")]
        public void Validar_WhenAllFieldsAreInvalid_ShouldReturnEveryError()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("", " ", null);

            // Act
            var erros = solicitacao.Validar();

            // Assert
            erros.Keys.ShouldBe(
                [nameof(SolicitacaoPlanilhaEstoque.TipoVeiculo), nameof(SolicitacaoPlanilhaEstoque.RotuloDetalhe), nameof(SolicitacaoPlanilhaEstoque.Veiculos)],
                ignoreOrder: true);
        }
    }
}
