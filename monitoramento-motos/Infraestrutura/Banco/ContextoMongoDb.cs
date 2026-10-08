using Concessionaria.Motos.Dominio.Entidades;
using MongoDB.Driver;

namespace Concessionaria.Motos.Infraestrutura.Banco;

/// <summary>Banco desta API e suas coleções.</summary>
public class ContextoMongoDb(IMongoDatabase banco)
{
    /// <summary>Banco configurado em <c>MongoDb:NomeBanco</c>.</summary>
    public IMongoDatabase Banco => banco;

    /// <summary>Coleção <c>motos</c>.</summary>
    public IMongoCollection<Moto> Motos => banco.GetCollection<Moto>("motos");

    /// <summary>Coleção <c>vendas</c>.</summary>
    public IMongoCollection<Venda> Vendas => banco.GetCollection<Venda>("vendas");
}
