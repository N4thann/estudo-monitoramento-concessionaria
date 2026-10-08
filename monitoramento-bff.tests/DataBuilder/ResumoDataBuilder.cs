using Bogus;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.tests.DataBuilder
{
    /// <summary>Gera resumos (Bogus) de Carros ou Motos, com indicadores de vendas preenchidos.</summary>
    public class ResumoDataBuilder
    {
        private Resumo _instance;

        /// <summary>Cria um resumo coerente (total = disponíveis + vendidos) com indicadores.</summary>
        public ResumoDataBuilder()
        {
            var faker = new Faker<Resumo>("pt_BR").CustomInstantiator(f =>
            {
                var disponiveis = f.Random.Long(0, 50);
                var vendidos = f.Random.Long(0, 50);
                var quantidadeVendas = (int)vendidos;
                var receita = Math.Round(f.Finance.Amount(0, 1_000_000), 2);
                return new Resumo(
                    TotalVeiculos: disponiveis + vendidos,
                    Disponiveis: disponiveis,
                    Vendidos: vendidos,
                    ValorEmEstoque: Math.Round(f.Finance.Amount(0, 5_000_000), 2),
                    Indicadores: new IndicadoresVendas(
                        QuantidadeVendas: quantidadeVendas,
                        Receita: receita,
                        TicketMedio: quantidadeVendas == 0 ? 0m : Math.Round(receita / quantidadeVendas, 2),
                        MaiorVenda: receita,
                        MarcaMaisVendida: f.Vehicle.Manufacturer()));
            });

            _instance = faker.Generate();
        }

        /// <summary>Ponto de entrada fluente.</summary>
        public static ResumoDataBuilder Create() => new();

        /// <summary>Devolve o resumo montado.</summary>
        public Resumo Build() => _instance;

        /// <summary>Permite usar o builder onde se espera um <see cref="Resumo"/>.</summary>
        public static implicit operator Resumo(ResumoDataBuilder builder) => builder.Build();

        /// <summary>Gera <paramref name="count"/> resumos independentes.</summary>
        public static List<Resumo> AsList(int count)
        {
            var list = new List<Resumo>();
            for (int i = 0; i < count; i++) list.Add(Create().Build());
            return list;
        }

        /// <summary>Define os totais do estoque.</summary>
        public ResumoDataBuilder WithEstoque(long disponiveis, long vendidos, decimal valorEmEstoque)
        {
            _instance = _instance with { TotalVeiculos = disponiveis + vendidos, Disponiveis = disponiveis, Vendidos = vendidos, ValorEmEstoque = valorEmEstoque };
            return this;
        }

        /// <summary>Define os indicadores de vendas.</summary>
        public ResumoDataBuilder WithIndicadores(int quantidadeVendas, decimal receita)
        {
            _instance = _instance with { Indicadores = new IndicadoresVendas(quantidadeVendas, receita, 0m, 0m, null) };
            return this;
        }

        /// <summary>Remove os indicadores, como quando Relatórios está fora.</summary>
        public ResumoDataBuilder SemIndicadores() { _instance = _instance with { Indicadores = null }; return this; }
    }
}
