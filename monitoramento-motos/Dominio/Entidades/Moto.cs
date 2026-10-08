using Concessionaria.Motos.Dominio.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Concessionaria.Motos.Dominio.Entidades;

/// <summary>Moto do estoque da concessionária (coleção <c>motos</c>).</summary>
public class Moto
{
    /// <summary>Identificador (ObjectId do Mongo, em texto).</summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>Fabricante.</summary>
    public string Marca { get; set; } = string.Empty;

    /// <summary>Modelo.</summary>
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Ano do modelo.</summary>
    public int Ano { get; set; }

    /// <summary>Cor.</summary>
    public string Cor { get; set; } = string.Empty;

    /// <summary>Cilindrada da motor, em cc.</summary>
    public int Cilindrada { get; set; }

    /// <summary>Preço de venda anunciado.</summary>
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Preco { get; set; }

    /// <summary>Situação comercial, gravada como texto no banco.</summary>
    [BsonRepresentation(BsonType.String)]
    public SituacaoVeiculo Situacao { get; set; } = SituacaoVeiculo.Disponivel;

    /// <summary>Data de cadastro (UTC).</summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>Data da última alteração (UTC); nula se nunca foi alterado.</summary>
    public DateTime? AtualizadoEm { get; set; }

    /// <summary>Descrição curta usada em logs e na venda: "Marca Modelo Ano".</summary>
    public string ObterDescricao() => $"{Marca} {Modelo} {Ano}";
}
