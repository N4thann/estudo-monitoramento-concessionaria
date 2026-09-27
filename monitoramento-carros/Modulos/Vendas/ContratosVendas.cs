using Concessionaria.Carros.Entidades;
using ErrorOr;

namespace Concessionaria.Carros.Modulos.Vendas;

public record RequisicaoVenda(string VeiculoId, string NomeCliente, string DocumentoCliente, decimal Valor)
{
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

public record RespostaVenda(
    string Id,
    string VeiculoId,
    string DescricaoVeiculo,
    string Marca,
    string NomeCliente,
    string DocumentoCliente,
    decimal Valor,
    DateTime VendidaEm)
{
    public static RespostaVenda De(Venda venda) =>
        new(venda.Id, venda.VeiculoId, venda.DescricaoVeiculo, venda.Marca, venda.NomeCliente, venda.DocumentoCliente, venda.Valor, venda.VendidaEm);
}
