using Bogus;
using Concessionaria.Relatorios.Contratos;

namespace Concessionaria.Relatorios.tests.DataBuilder
{
    /// <summary>Gera veículos em estoque (Bogus) como Carros/Motos enviam para Relatórios.</summary>
    public class VeiculoRelatorioDataBuilder
    {
        private static readonly string[] Marcas = ["Toyota", "Honda", "Volkswagen", "Fiat", "Chevrolet", "Yamaha"];

        private VeiculoRelatorio _instance;

        public VeiculoRelatorioDataBuilder()
        {
            var faker = new Faker<VeiculoRelatorio>("pt_BR")
                .CustomInstantiator(f => new VeiculoRelatorio(
                    Marca: f.PickRandom(Marcas),
                    Modelo: f.Vehicle.Model(),
                    Ano: f.Random.Int(2010, 2026),
                    Cor: f.Commerce.Color(),
                    Detalhe: f.PickRandom("2", "4"),
                    Preco: Math.Round(f.Finance.Amount(20_000, 300_000), 2),
                    CadastradoEm: DateTime.SpecifyKind(f.Date.Past(1), DateTimeKind.Utc)));

            _instance = faker.Generate();
        }

        public static VeiculoRelatorioDataBuilder Create() => new();
        public VeiculoRelatorio Build() => _instance;
        public static implicit operator VeiculoRelatorio(VeiculoRelatorioDataBuilder builder) => builder.Build();

        public static List<VeiculoRelatorio> AsList(int count)
        {
            var list = new List<VeiculoRelatorio>();
            for (int i = 0; i < count; i++) list.Add(Create().Build());
            return list;
        }

        public VeiculoRelatorioDataBuilder WithMarca(string marca) { _instance = _instance with { Marca = marca }; return this; }
        public VeiculoRelatorioDataBuilder WithModelo(string modelo) { _instance = _instance with { Modelo = modelo }; return this; }
        public VeiculoRelatorioDataBuilder WithAno(int ano) { _instance = _instance with { Ano = ano }; return this; }
        public VeiculoRelatorioDataBuilder WithCor(string cor) { _instance = _instance with { Cor = cor }; return this; }
        public VeiculoRelatorioDataBuilder WithDetalhe(string detalhe) { _instance = _instance with { Detalhe = detalhe }; return this; }
        public VeiculoRelatorioDataBuilder WithPreco(decimal preco) { _instance = _instance with { Preco = preco }; return this; }
        public VeiculoRelatorioDataBuilder WithCadastradoEm(DateTime cadastradoEm) { _instance = _instance with { CadastradoEm = cadastradoEm }; return this; }
    }
}
