using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Concessionaria.Carros.Entidades;

public class Venda
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string VeiculoId { get; set; } = string.Empty;

    // Retrato do veículo no momento da venda: o relatório não depende do cadastro atual do carro.
    public string DescricaoVeiculo { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;

    public string NomeCliente { get; set; } = string.Empty;
    public string DocumentoCliente { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Valor { get; set; }

    public DateTime VendidaEm { get; set; } = DateTime.UtcNow;
}
