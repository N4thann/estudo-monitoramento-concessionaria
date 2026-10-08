using Bogus;
using Concessionaria.Carros.Dominio.Entidades;
using Concessionaria.Carros.Dominio.Enums;
using MongoDB.Bson;

namespace Concessionaria.Carros.tests.DataBuilder
{
    /// <summary>Gera carros (Bogus) disponíveis e válidos para as regras de cadastro.</summary>
    public class CarroDataBuilder
    {
        private static readonly string[] Marcas = ["Toyota", "Honda", "Volkswagen", "Fiat", "Chevrolet", "Hyundai"];

        private readonly Carro _instance;

        /// <summary>Cria um carro disponível com dados aleatórios.</summary>
        public CarroDataBuilder()
        {
            var faker = new Faker<Carro>("pt_BR")
                .RuleFor(c => c.Id, _ => ObjectId.GenerateNewId().ToString())
                .RuleFor(c => c.Marca, f => f.PickRandom(Marcas))
                .RuleFor(c => c.Modelo, f => f.Vehicle.Model())
                .RuleFor(c => c.Ano, f => f.Random.Int(2010, 2026))
                .RuleFor(c => c.Cor, f => f.Commerce.Color())
                .RuleFor(c => c.Portas, f => f.PickRandom(2, 4))
                .RuleFor(c => c.Preco, f => Math.Round(f.Finance.Amount(20_000, 300_000), 2))
                .RuleFor(c => c.Situacao, SituacaoVeiculo.Disponivel)
                .RuleFor(c => c.CriadoEm, f => DateTime.SpecifyKind(f.Date.Past(1), DateTimeKind.Utc));

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
        public CarroDataBuilder WithId(string id) { _instance.Id = id; return this; }

        /// <summary>Define a marca.</summary>
        public CarroDataBuilder WithMarca(string marca) { _instance.Marca = marca; return this; }

        /// <summary>Define o modelo.</summary>
        public CarroDataBuilder WithModelo(string modelo) { _instance.Modelo = modelo; return this; }

        /// <summary>Define o ano.</summary>
        public CarroDataBuilder WithAno(int ano) { _instance.Ano = ano; return this; }

        /// <summary>Define a cor.</summary>
        public CarroDataBuilder WithCor(string cor) { _instance.Cor = cor; return this; }

        /// <summary>Define a quantidade de portas.</summary>
        public CarroDataBuilder WithPortas(int portas) { _instance.Portas = portas; return this; }

        /// <summary>Define o preço.</summary>
        public CarroDataBuilder WithPreco(decimal preco) { _instance.Preco = preco; return this; }

        /// <summary>Define a situação (disponível ou vendido).</summary>
        public CarroDataBuilder WithSituacao(SituacaoVeiculo situacao) { _instance.Situacao = situacao; return this; }

        /// <summary>Define a data de cadastro.</summary>
        public CarroDataBuilder WithCriadoEm(DateTime criadoEm) { _instance.CriadoEm = criadoEm; return this; }
    }
}
