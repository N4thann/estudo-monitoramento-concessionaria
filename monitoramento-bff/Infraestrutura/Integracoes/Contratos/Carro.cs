using Concessionaria.Bff.Dominio.Enums;

namespace Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

/// <summary>Carro devolvido pela API de Carros.</summary>
public record Carro(string Id, string Marca, string Modelo, int Ano, string Cor, int Portas, decimal Preco, SituacaoVeiculo Situacao, DateTime CriadoEm);
