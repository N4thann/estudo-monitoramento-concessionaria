using ErrorOr;

namespace Concessionaria.Motos.Aplicacao.Features.Vendas.Contratos;

/// <summary>Dados para registrar a venda de uma moto.</summary>
/// <param name="VeiculoId">Id da moto vendida.</param>
/// <param name="NomeCliente">Nome do comprador (até 120 caracteres).</param>
/// <param name="DocumentoCliente">CPF (11 dígitos) ou CNPJ (14 dígitos), com ou sem máscara.</param>
/// <param name="Valor">Valor negociado, maior que zero.</param>
public record RequisicaoVenda(string VeiculoId, string NomeCliente, string DocumentoCliente, decimal Valor)
{
    /// <summary>Valida os campos; lista vazia significa requisição válida. O código de cada erro é o nome do campo.</summary>
    public List<Error> Validar()
    {
        var erros = new List<Error>();
        var digitosDocumento = new string((DocumentoCliente ?? string.Empty).Where(char.IsDigit).ToArray());

        if (string.IsNullOrWhiteSpace(VeiculoId))
            erros.Add(Error.Validation(nameof(VeiculoId), "Veículo é obrigatório."));
        if (string.IsNullOrWhiteSpace(NomeCliente) || NomeCliente.Length > 120)
            erros.Add(Error.Validation(nameof(NomeCliente), "Nome do cliente é obrigatório e deve ter até 120 caracteres."));
        if (digitosDocumento.Length is not (11 or 14))
            erros.Add(Error.Validation(nameof(DocumentoCliente), "Documento do cliente deve ser um CPF (11 dígitos) ou CNPJ (14 dígitos)."));
        if (Valor <= 0)
            erros.Add(Error.Validation(nameof(Valor), "Valor da venda deve ser maior que zero."));

        return erros;
    }
}
