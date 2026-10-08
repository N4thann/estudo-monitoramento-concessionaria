using Bogus;
using Concessionaria.Relatorios.Aplicacao.Comum.Contratos;

namespace Concessionaria.Relatorios.tests.DataBuilder
{
    /// <summary>Gera vendas (Bogus) como Carros/Motos enviam para Relatórios.</summary>
    public class VendaRelatorioDataBuilder
    {
        private static readonly string[] Marcas = ["Toyota", "Honda", "Volkswagen", "Fiat", "Chevrolet", "Yamaha"];

        private VendaRelatorio _instance;

        public VendaRelatorioDataBuilder()
        {
            var faker = new Faker<VendaRelatorio>("pt_BR")
                .CustomInstantiator(f => new VendaRelatorio(
                    Veiculo: f.Vehicle.Model(),
                    Marca: f.PickRandom(Marcas),
                    Cliente: f.Name.FullName(),
                    Documento: f.Random.ReplaceNumbers("###########"),
                    Valor: Math.Round(f.Finance.Amount(20_000, 300_000), 2),
                    VendidaEm: DateTime.SpecifyKind(f.Date.Past(1), DateTimeKind.Utc)));

            _instance = faker.Generate();
        }

        public static VendaRelatorioDataBuilder Create() => new();
        public VendaRelatorio Build() => _instance;
        public static implicit operator VendaRelatorio(VendaRelatorioDataBuilder builder) => builder.Build();

        public static List<VendaRelatorio> AsList(int count)
        {
            var list = new List<VendaRelatorio>();
            for (int i = 0; i < count; i++) list.Add(Create().Build());
            return list;
        }

        public VendaRelatorioDataBuilder WithVeiculo(string veiculo) { _instance = _instance with { Veiculo = veiculo }; return this; }
        public VendaRelatorioDataBuilder WithMarca(string marca) { _instance = _instance with { Marca = marca }; return this; }
        public VendaRelatorioDataBuilder WithCliente(string cliente) { _instance = _instance with { Cliente = cliente }; return this; }
        public VendaRelatorioDataBuilder WithDocumento(string documento) { _instance = _instance with { Documento = documento }; return this; }
        public VendaRelatorioDataBuilder WithValor(decimal valor) { _instance = _instance with { Valor = valor }; return this; }
        public VendaRelatorioDataBuilder WithVendidaEm(DateTime vendidaEm) { _instance = _instance with { VendidaEm = vendidaEm }; return this; }
    }
}
