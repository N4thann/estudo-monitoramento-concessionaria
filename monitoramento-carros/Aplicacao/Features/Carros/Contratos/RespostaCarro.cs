using Concessionaria.Carros.Dominio.Entidades;
using Concessionaria.Carros.Dominio.Enums;

namespace Concessionaria.Carros.Aplicacao.Features.Carros.Contratos;

/// <summary>Carro devolvido pela API.</summary>
public record RespostaCarro(
    string Id,
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    int Portas,
    decimal Preco,
    SituacaoVeiculo Situacao,
    DateTime CriadoEm)
{
    /// <summary>Converte a entidade na resposta da API.</summary>
    public static RespostaCarro De(Carro carro) =>
        new(carro.Id, carro.Marca, carro.Modelo, carro.Ano, carro.Cor, carro.Portas, carro.Preco, carro.Situacao, carro.CriadoEm);
}
