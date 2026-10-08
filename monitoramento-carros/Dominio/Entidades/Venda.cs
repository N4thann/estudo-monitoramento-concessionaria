using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Concessionaria.Carros.Dominio.Entidades;

/// <summary>
/// Venda de um carro (coleção <c>vendas</c>). Guarda um retrato do veículo no momento da venda, para que
/// o relatório não dependa do cadastro atual do carro.
/// </summary>
public class Venda
{
    /// <summary>Identificador (ObjectId do Mongo, em texto).</summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>Id do carro vendido; tem índice único, então um carro só pode ser vendido uma vez.</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string VeiculoId { get; set; } = string.Empty;

    /// <summary>Descrição do carro na hora da venda ("Marca Modelo Ano").</summary>
    public string DescricaoVeiculo { get; set; } = string.Empty;

    /// <summary>Marca do carro na hora da venda.</summary>
    public string Marca { get; set; } = string.Empty;

    /// <summary>Nome do comprador.</summary>
    public string NomeCliente { get; set; } = string.Empty;

    /// <summary>CPF ou CNPJ do comprador, só com dígitos.</summary>
    public string DocumentoCliente { get; set; } = string.Empty;

    /// <summary>Valor negociado.</summary>
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Valor { get; set; }

    /// <summary>Data da venda (UTC).</summary>
    public DateTime VendidaEm { get; set; } = DateTime.UtcNow;
}
