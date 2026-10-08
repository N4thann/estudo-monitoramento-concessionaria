using Concessionaria.Carros.Dominio.Entidades;

namespace Concessionaria.Carros.Aplicacao.Features.Vendas.Contratos;

/// <summary>Venda devolvida pela API.</summary>
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
    /// <summary>Converte a entidade na resposta da API.</summary>
    public static RespostaVenda De(Venda venda) =>
        new(venda.Id, venda.VeiculoId, venda.DescricaoVeiculo, venda.Marca, venda.NomeCliente, venda.DocumentoCliente, venda.Valor, venda.VendidaEm);
}
