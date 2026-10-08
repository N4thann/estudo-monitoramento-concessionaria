using Concessionaria.Motos.Dominio.Entidades;
using Concessionaria.Motos.Dominio.Enums;

namespace Concessionaria.Motos.Aplicacao.Features.Motos.Contratos;

/// <summary>Moto devolvida pela API.</summary>
public record RespostaMoto(
    string Id,
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    int Cilindrada,
    decimal Preco,
    SituacaoVeiculo Situacao,
    DateTime CriadoEm)
{
    /// <summary>Converte a entidade na resposta da API.</summary>
    public static RespostaMoto De(Moto moto) =>
        new(moto.Id, moto.Marca, moto.Modelo, moto.Ano, moto.Cor, moto.Cilindrada, moto.Preco, moto.Situacao, moto.CriadoEm);
}
