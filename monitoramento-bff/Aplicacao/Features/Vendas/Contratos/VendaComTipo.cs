using Concessionaria.Bff.Dominio.Enums;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Aplicacao.Features.Vendas.Contratos;

/// <summary>Venda da lista unificada, com o tipo de veículo que identifica o serviço de origem.</summary>
public record VendaComTipo(
    string Id,
    TipoVeiculo TipoVeiculo,
    string VeiculoId,
    string DescricaoVeiculo,
    string Marca,
    string NomeCliente,
    string DocumentoCliente,
    decimal Valor,
    DateTime VendidaEm)
{
    /// <summary>Copia a venda de Carros ou Motos acrescentando o tipo.</summary>
    public static VendaComTipo De(Venda venda, TipoVeiculo tipo) =>
        new(venda.Id, tipo, venda.VeiculoId, venda.DescricaoVeiculo, venda.Marca, venda.NomeCliente, venda.DocumentoCliente, venda.Valor, venda.VendidaEm);
}
