using Concessionaria.Carros.Dominio.Entidades;
using MongoDB.Driver;

namespace Concessionaria.Carros.Infraestrutura.Banco;

/// <summary>Banco desta API e suas coleções.</summary>
public class ContextoMongoDb(IMongoDatabase banco)
{
    /// <summary>Banco configurado em <c>MongoDb:NomeBanco</c>.</summary>
    public IMongoDatabase Banco => banco;

    /// <summary>Coleção <c>carros</c>.</summary>
    public IMongoCollection<Carro> Carros => banco.GetCollection<Carro>("carros");

    /// <summary>Coleção <c>vendas</c>.</summary>
    public IMongoCollection<Venda> Vendas => banco.GetCollection<Venda>("vendas");
}
