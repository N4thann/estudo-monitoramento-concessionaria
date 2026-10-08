using Bogus;
using Concessionaria.Motos.Dominio.Entidades;
using Concessionaria.Motos.Dominio.Enums;
using MongoDB.Bson;

namespace Concessionaria.Motos.tests.DataBuilder
{
    /// <summary>Gera motos (Bogus) disponíveis e válidos para as regras de cadastro.</summary>
    public class MotoDataBuilder
    {
        private static readonly string[] Marcas = ["Honda", "Yamaha", "Kawasaki", "BMW", "Triumph", "Royal Enfield"];

        private readonly Moto _instance;

        /// <summary>Cria uma moto disponível com dados aleatórios.</summary>
        public MotoDataBuilder()
        {
            var faker = new Faker<Moto>("pt_BR")
                .RuleFor(c => c.Id, _ => ObjectId.GenerateNewId().ToString())
                .RuleFor(c => c.Marca, f => f.PickRandom(Marcas))
                .RuleFor(c => c.Modelo, f => f.Vehicle.Model())
                .RuleFor(c => c.Ano, f => f.Random.Int(2010, 2026))
                .RuleFor(c => c.Cor, f => f.Commerce.Color())
                .RuleFor(c => c.Cilindrada, f => f.Random.Int(50, 2500))
                .RuleFor(c => c.Preco, f => Math.Round(f.Finance.Amount(20_000, 300_000), 2))
                .RuleFor(c => c.Situacao, SituacaoVeiculo.Disponivel)
                .RuleFor(c => c.CriadoEm, f => DateTime.SpecifyKind(f.Date.Past(1), DateTimeKind.Utc));

            _instance = faker.Generate();
        }

        /// <summary>Ponto de entrada fluente.</summary>
        public static MotoDataBuilder Create() => new();

        /// <summary>Devolve a moto montado.</summary>
        public Moto Build() => _instance;

        /// <summary>Permite usar o builder onde se espera um <see cref="Moto"/>.</summary>
        public static implicit operator Moto(MotoDataBuilder builder) => builder.Build();

        /// <summary>Gera <paramref name="count"/> motos independentes.</summary>
        public static List<Moto> AsList(int count)
        {
            var list = new List<Moto>();
            for (int i = 0; i < count; i++) list.Add(Create().Build());
            return list;
        }

        /// <summary>Define o Id.</summary>
        public MotoDataBuilder WithId(string id) { _instance.Id = id; return this; }

        /// <summary>Define a marca.</summary>
        public MotoDataBuilder WithMarca(string marca) { _instance.Marca = marca; return this; }

        /// <summary>Define o modelo.</summary>
        public MotoDataBuilder WithModelo(string modelo) { _instance.Modelo = modelo; return this; }

        /// <summary>Define o ano.</summary>
        public MotoDataBuilder WithAno(int ano) { _instance.Ano = ano; return this; }

        /// <summary>Define a cor.</summary>
        public MotoDataBuilder WithCor(string cor) { _instance.Cor = cor; return this; }

        /// <summary>Define a cilindrada.</summary>
        public MotoDataBuilder WithCilindrada(int cilindrada) { _instance.Cilindrada = cilindrada; return this; }

        /// <summary>Define o preço.</summary>
        public MotoDataBuilder WithPreco(decimal preco) { _instance.Preco = preco; return this; }

        /// <summary>Define a situação (disponível ou vendido).</summary>
        public MotoDataBuilder WithSituacao(SituacaoVeiculo situacao) { _instance.Situacao = situacao; return this; }

        /// <summary>Define a data de cadastro.</summary>
        public MotoDataBuilder WithCriadoEm(DateTime criadoEm) { _instance.CriadoEm = criadoEm; return this; }
    }
}
