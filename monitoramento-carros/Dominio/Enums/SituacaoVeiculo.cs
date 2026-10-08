namespace Concessionaria.Carros.Dominio.Enums;

/// <summary>Situação comercial de um veículo do estoque.</summary>
public enum SituacaoVeiculo
{
    /// <summary>À venda: pode ser editado, excluído e vendido.</summary>
    Disponivel,

    /// <summary>Já vendido: não pode mais ser editado, excluído nem revendido.</summary>
    Vendido
}
