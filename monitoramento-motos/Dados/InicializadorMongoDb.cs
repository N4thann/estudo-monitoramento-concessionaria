using Concessionaria.Motos.Entidades;
using MongoDB.Driver;

namespace Concessionaria.Motos.Dados;

// Cria os índices na subida da aplicação e, em Development, popula algumas motos para facilitar os testes.
public class InicializadorMongoDb(ContextoMongoDb contexto, IHostEnvironment ambiente, ILogger<InicializadorMongoDb> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await contexto.Motos.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Moto>(Builders<Moto>.IndexKeys.Descending(c => c.CriadoEm)),
            new CreateIndexModel<Moto>(Builders<Moto>.IndexKeys.Ascending(c => c.Situacao).Descending(c => c.CriadoEm))
        ], cancellationToken);

        await contexto.Vendas.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Venda>(Builders<Venda>.IndexKeys.Descending(v => v.VendidaEm)),
            new CreateIndexModel<Venda>(Builders<Venda>.IndexKeys.Ascending(v => v.VeiculoId), new CreateIndexOptions { Unique = true })
        ], cancellationToken);

        if (ambiente.IsDevelopment() && await contexto.Motos.EstimatedDocumentCountAsync(cancellationToken: cancellationToken) == 0)
        {
            await contexto.Motos.InsertManyAsync(MotosIniciais(), cancellationToken: cancellationToken);
            logger.LogInformation("Carga inicial de motos inserida no banco {Banco}", contexto.Banco.DatabaseNamespace.DatabaseName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IEnumerable<Moto> MotosIniciais() =>
    [
        new() { Marca = "Honda", Modelo = "CG 160 Titan", Ano = 2024, Cor = "Vermelha", Cilindrada = 160, Preco = 17_990m },
        new() { Marca = "Honda", Modelo = "CB 500F", Ano = 2023, Cor = "Preta", Cilindrada = 471, Preco = 38_500m },
        new() { Marca = "Yamaha", Modelo = "Fazer FZ25", Ano = 2023, Cor = "Azul", Cilindrada = 249, Preco = 23_800m },
        new() { Marca = "Yamaha", Modelo = "MT-07", Ano = 2022, Cor = "Cinza", Cilindrada = 689, Preco = 52_900m },
        new() { Marca = "Kawasaki", Modelo = "Ninja 400", Ano = 2024, Cor = "Verde", Cilindrada = 399, Preco = 36_990m },
        new() { Marca = "BMW", Modelo = "G 310 GS", Ano = 2023, Cor = "Branca", Cilindrada = 313, Preco = 34_500m },
        new() { Marca = "Triumph", Modelo = "Tiger 900", Ano = 2022, Cor = "Preta", Cilindrada = 888, Preco = 79_900m },
        new() { Marca = "Royal Enfield", Modelo = "Meteor 350", Ano = 2024, Cor = "Prata", Cilindrada = 349, Preco = 22_990m }
    ];
}
