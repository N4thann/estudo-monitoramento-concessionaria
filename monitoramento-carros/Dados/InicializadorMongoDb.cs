using Concessionaria.Carros.Entidades;
using MongoDB.Driver;

namespace Concessionaria.Carros.Dados;

// Cria os índices na subida da aplicação e, em Development, popula alguns carros para facilitar os testes.
public class InicializadorMongoDb(ContextoMongoDb contexto, IHostEnvironment ambiente, ILogger<InicializadorMongoDb> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await contexto.Carros.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Carro>(Builders<Carro>.IndexKeys.Descending(c => c.CriadoEm)),
            new CreateIndexModel<Carro>(Builders<Carro>.IndexKeys.Ascending(c => c.Situacao).Descending(c => c.CriadoEm))
        ], cancellationToken);

        await contexto.Vendas.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Venda>(Builders<Venda>.IndexKeys.Descending(v => v.VendidaEm)),
            new CreateIndexModel<Venda>(Builders<Venda>.IndexKeys.Ascending(v => v.VeiculoId), new CreateIndexOptions { Unique = true })
        ], cancellationToken);

        if (ambiente.IsDevelopment() && await contexto.Carros.EstimatedDocumentCountAsync(cancellationToken: cancellationToken) == 0)
        {
            await contexto.Carros.InsertManyAsync(CarrosIniciais(), cancellationToken: cancellationToken);
            logger.LogInformation("Carga inicial de carros inserida no banco {Banco}", contexto.Banco.DatabaseNamespace.DatabaseName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IEnumerable<Carro> CarrosIniciais() =>
    [
        new() { Marca = "Toyota", Modelo = "Corolla", Ano = 2023, Cor = "Prata", Portas = 4, Preco = 139_900m },
        new() { Marca = "Honda", Modelo = "Civic", Ano = 2022, Cor = "Preto", Portas = 4, Preco = 129_500m },
        new() { Marca = "Volkswagen", Modelo = "Polo", Ano = 2024, Cor = "Branco", Portas = 4, Preco = 94_990m },
        new() { Marca = "Chevrolet", Modelo = "Onix", Ano = 2023, Cor = "Vermelho", Portas = 4, Preco = 82_300m },
        new() { Marca = "Fiat", Modelo = "Pulse", Ano = 2024, Cor = "Cinza", Portas = 4, Preco = 105_000m },
        new() { Marca = "Jeep", Modelo = "Compass", Ano = 2022, Cor = "Azul", Portas = 4, Preco = 168_700m },
        new() { Marca = "Hyundai", Modelo = "HB20", Ano = 2021, Cor = "Branco", Portas = 4, Preco = 69_900m },
        new() { Marca = "Fiat", Modelo = "Mobi", Ano = 2023, Cor = "Laranja", Portas = 2, Preco = 59_990m }
    ];
}
