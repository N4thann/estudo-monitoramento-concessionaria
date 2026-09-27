using Concessionaria.Carros.Entidades;
using MongoDB.Driver;

namespace Concessionaria.Carros.Dados;

public class ContextoMongoDb(IMongoDatabase banco)
{
    public IMongoDatabase Banco => banco;
    public IMongoCollection<Carro> Carros => banco.GetCollection<Carro>("carros");
    public IMongoCollection<Venda> Vendas => banco.GetCollection<Venda>("vendas");
}
