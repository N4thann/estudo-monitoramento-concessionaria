using Bogus;
using Concessionaria.Bff.Dominio.Enums;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.tests.DataBuilder
{
    /// <summary>Gera motos (Bogus) disponíveis, como a API de Motos devolveria.</summary>
    public class MotoDataBuilder
    {
        private static readonly string[] Marcas = ["Honda", "Yamaha", "Kawasaki", "BMW", "Triumph", "Royal Enfield"];

        private Moto _instance;

        /// <summary>Cria uma moto disponível com dados aleatórios.</summary>
        public MotoDataBuilder()
        {
            var faker = new Faker<Moto>("pt_BR").CustomInstantiator(f => new Moto(
                Id: f.Random.Hexadecimal(24, prefix: string.Empty),
                Marca: f.PickRandom(Marcas),
                Modelo: f.Vehicle.Model(),
                Ano: f.Random.Int(2010, 2026),
                Cor: f.Commerce.Color(),
                Cilindrada: f.Random.Int(50, 2500),
                Preco: Math.Round(f.Finance.Amount(10_000, 120_000), 2),
                Situacao: SituacaoVeiculo.Disponivel,
                CriadoEm: DateTime.SpecifyKind(f.Date.Past(1), DateTimeKind.Utc)));

            _instance = faker.Generate();
        }

        /// <summary>Ponto de entrada fluente.</summary>
        public static MotoDataBuilder Create() => new();

        /// <summary>Devolve a moto montada.</summary>
        public Moto Build() => _instance;

        /// <summary>Permite usar o builder onde se espera uma <see cref="Moto"/>.</summary>
        public static implicit operator Moto(MotoDataBuilder builder) => builder.Build();

        /// <summary>Gera <paramref name="count"/> motos independentes.</summary>
        public static List<Moto> AsList(int count)
        {
            var list = new List<Moto>();
            for (int i = 0; i < count; i++) list.Add(Create().Build());
            return list;
        }

        /// <summary>Define o Id.</summary>
        public MotoDataBuilder WithId(string id) { _instance = _instance with { Id = id }; return this; }

        /// <summary>Define a cilindrada.</summary>
        public MotoDataBuilder WithCilindrada(int cilindrada) { _instance = _instance with { Cilindrada = cilindrada }; return this; }
    }
}
