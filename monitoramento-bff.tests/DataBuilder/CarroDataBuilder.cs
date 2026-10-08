using Bogus;
using Concessionaria.Bff.Dominio.Enums;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.tests.DataBuilder
{
    /// <summary>Gera carros (Bogus) disponíveis, como a API de Carros devolveria.</summary>
    public class CarroDataBuilder
    {
        private static readonly string[] Marcas = ["Toyota", "Honda", "Volkswagen", "Fiat", "Chevrolet", "Hyundai"];

        private Carro _instance;

        /// <summary>Cria um carro disponível com dados aleatórios.</summary>
        public CarroDataBuilder()
        {
            var faker = new Faker<Carro>("pt_BR").CustomInstantiator(f => new Carro(
                Id: f.Random.Hexadecimal(24, prefix: string.Empty),
                Marca: f.PickRandom(Marcas),
                Modelo: f.Vehicle.Model(),
                Ano: f.Random.Int(2010, 2026),
                Cor: f.Commerce.Color(),
                Portas: f.PickRandom(2, 4),
                Preco: Math.Round(f.Finance.Amount(20_000, 300_000), 2),
                Situacao: SituacaoVeiculo.Disponivel,
                CriadoEm: DateTime.SpecifyKind(f.Date.Past(1), DateTimeKind.Utc)));

            _instance = faker.Generate();
        }

        /// <summary>Ponto de entrada fluente.</summary>
        public static CarroDataBuilder Create() => new();

        /// <summary>Devolve o carro montado.</summary>
        public Carro Build() => _instance;

        /// <summary>Permite usar o builder onde se espera um <see cref="Carro"/>.</summary>
        public static implicit operator Carro(CarroDataBuilder builder) => builder.Build();

        /// <summary>Gera <paramref name="count"/> carros independentes.</summary>
        public static List<Carro> AsList(int count)
        {
            var list = new List<Carro>();
            for (int i = 0; i < count; i++) list.Add(Create().Build());
            return list;
        }

        /// <summary>Define o Id.</summary>
        public CarroDataBuilder WithId(string id) { _instance = _instance with { Id = id }; return this; }

        /// <summary>Define a situação (disponível ou vendido).</summary>
        public CarroDataBuilder WithSituacao(SituacaoVeiculo situacao) { _instance = _instance with { Situacao = situacao }; return this; }
    }
}
