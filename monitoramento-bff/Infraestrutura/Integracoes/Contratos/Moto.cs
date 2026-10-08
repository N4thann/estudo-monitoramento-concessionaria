using Concessionaria.Bff.Dominio.Enums;

namespace Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

/// <summary>Moto devolvida pela API de Motos.</summary>
public record Moto(string Id, string Marca, string Modelo, int Ano, string Cor, int Cilindrada, decimal Preco, SituacaoVeiculo Situacao, DateTime CriadoEm);
