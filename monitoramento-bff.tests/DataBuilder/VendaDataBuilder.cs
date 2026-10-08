using Bogus;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.tests.DataBuilder
{
    /// <summary>Gera vendas (Bogus), como Carros ou Motos devolveriam.</summary>
    public class VendaDataBuilder
    {
        private Venda _instance;

        /// <summary>Cria uma venda com dados aleatórios e documento só com dígitos.</summary>
        public VendaDataBuilder()
        {
            var faker = new Faker<Venda>("pt_BR").CustomInstantiator(f => new Venda(
                Id: f.Random.Hexadecimal(24, prefix: string.Empty),
                VeiculoId: f.Random.Hexadecimal(24, prefix: string.Empty),
                DescricaoVeiculo: $"{f.Vehicle.Manufacturer()} {f.Vehicle.Model()} {f.Random.Int(2010, 2026)}",
                Marca: f.Vehicle.Manufacturer(),
                NomeCliente: f.Person.FullName,
                DocumentoCliente: f.Random.ReplaceNumbers("###########"),
                Valor: Math.Round(f.Finance.Amount(10_000, 300_000), 2),
                VendidaEm: DateTime.SpecifyKind(f.Date.Past(1), DateTimeKind.Utc)));

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

        /// <summary>Define o Id.</summary>
        public VendaDataBuilder WithId(string id) { _instance = _instance with { Id = id }; return this; }

        /// <summary>Define a data da venda.</summary>
        public VendaDataBuilder WithVendidaEm(DateTime vendidaEm) { _instance = _instance with { VendidaEm = vendidaEm }; return this; }
    }
}
