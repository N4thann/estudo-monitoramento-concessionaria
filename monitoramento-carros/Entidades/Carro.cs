using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Concessionaria.Carros.Entidades;

public class Carro
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Ano { get; set; }
    public string Cor { get; set; } = string.Empty;
    public int Portas { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Preco { get; set; }

    [BsonRepresentation(BsonType.String)]
    public SituacaoVeiculo Situacao { get; set; } = SituacaoVeiculo.Disponivel;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }

    public string ObterDescricao() => $"{Marca} {Modelo} {Ano}";
}
