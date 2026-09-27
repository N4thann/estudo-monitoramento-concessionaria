using Concessionaria.Motos.Entidades;
using MongoDB.Driver;

namespace Concessionaria.Motos.Dados;

public class ContextoMongoDb(IMongoDatabase banco)
{
    public IMongoDatabase Banco => banco;
    public IMongoCollection<Moto> Motos => banco.GetCollection<Moto>("motos");
    public IMongoCollection<Venda> Vendas => banco.GetCollection<Venda>("vendas");
}
