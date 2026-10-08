using Bogus;
using Concessionaria.Carros.Dominio.Entidades;
using MongoDB.Bson;

namespace Concessionaria.Carros.tests.DataBuilder
{
    /// <summary>Gera vendas de carro (Bogus) como ficam gravadas no Mongo.</summary>
    public class VendaDataBuilder
    {
        private static readonly string[] Marcas = ["Toyota", "Honda", "Volkswagen", "Fiat", "Chevrolet", "Hyundai"];

        private readonly Venda _instance;

        /// <summary>Cria uma venda com dados aleatórios e documento só com dígitos.</summary>
        public VendaDataBuilder()
        {
            var faker = new Faker<Venda>("pt_BR")
                .RuleFor(v => v.Id, _ => ObjectId.GenerateNewId().ToString())
                .RuleFor(v => v.VeiculoId, _ => ObjectId.GenerateNewId().ToString())
                .RuleFor(v => v.Marca, f => f.PickRandom(Marcas))
                .RuleFor(v => v.DescricaoVeiculo, (f, v) => $"{v.Marca} {f.Vehicle.Model()} {f.Random.Int(2010, 2026)}")
                .RuleFor(v => v.NomeCliente, f => f.Name.FullName())
                .RuleFor(v => v.DocumentoCliente, f => f.Random.ReplaceNumbers("###########"))
                .RuleFor(v => v.Valor, f => Math.Round(f.Finance.Amount(20_000, 300_000), 2))
                .RuleFor(v => v.VendidaEm, f => DateTime.SpecifyKind(f.Date.Past(1), DateTimeKind.Utc));

            _instance = faker.Generate();
        }

        /// <summary>Ponto de entrada fluente.</summary>
        public static VendaDataBuilder Create() => new();

        /// <summary>Devolve a venda montada.</summary>
        public Venda Build() => _instance;

        /// <summary>Permite usar o builder onde se espera uma <see cref="Venda"/>.</summary>
        public static implicit operator Venda(VendaDataBuilder builder) => builder.Build();

        /// <summary>Gera <paramref name="count"/> vendas independentes.</summary>
        public static List<Venda> AsList(int count)
        {
            var list = new List<Venda>();
            for (int i = 0; i < count; i++) list.Add(Create().Build());
            return list;
        }

        /// <summary>Define a descrição do veículo no momento da venda.</summary>
        public VendaDataBuilder WithDescricaoVeiculo(string descricao) { _instance.DescricaoVeiculo = descricao; return this; }

        /// <summary>Define a marca.</summary>
        public VendaDataBuilder WithMarca(string marca) { _instance.Marca = marca; return this; }

        /// <summary>Define o nome do cliente.</summary>
        public VendaDataBuilder WithNomeCliente(string nome) { _instance.NomeCliente = nome; return this; }

        /// <summary>Define o documento do cliente.</summary>
        public VendaDataBuilder WithDocumentoCliente(string documento) { _instance.DocumentoCliente = documento; return this; }

        /// <summary>Define o valor.</summary>
        public VendaDataBuilder WithValor(decimal valor) { _instance.Valor = valor; return this; }

        /// <summary>Define o momento da venda.</summary>
        public VendaDataBuilder WithVendidaEm(DateTime vendidaEm) { _instance.VendidaEm = vendidaEm; return this; }
    }
}
