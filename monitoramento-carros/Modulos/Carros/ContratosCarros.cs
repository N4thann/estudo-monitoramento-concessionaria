using Concessionaria.Carros.Entidades;
using Concessionaria.Carros.Integracoes.Relatorios;
using ErrorOr;

namespace Concessionaria.Carros.Modulos.Carros;

public record FiltroCarros(int? Pagina, int? TamanhoPagina, SituacaoVeiculo? Situacao, string? Busca);

public record RequisicaoCarro(string Marca, string Modelo, int Ano, string Cor, int Portas, decimal Preco)
{
    public List<Error> Validar()
    {
        var erros = new List<Error>();
        var anoMaximo = DateTime.UtcNow.Year + 1;

        if (string.IsNullOrWhiteSpace(Marca) || Marca.Length > 60)
            erros.Add(Error.Validation(nameof(Marca), "Marca é obrigatória e deve ter até 60 caracteres."));
        if (string.IsNullOrWhiteSpace(Modelo) || Modelo.Length > 60)
            erros.Add(Error.Validation(nameof(Modelo), "Modelo é obrigatório e deve ter até 60 caracteres."));
        if (string.IsNullOrWhiteSpace(Cor) || Cor.Length > 30)
            erros.Add(Error.Validation(nameof(Cor), "Cor é obrigatória e deve ter até 30 caracteres."));
        if (Ano < 1950 || Ano > anoMaximo)
            erros.Add(Error.Validation(nameof(Ano), $"Ano deve estar entre 1950 e {anoMaximo}."));
        if (Portas < 2 || Portas > 5)
            erros.Add(Error.Validation(nameof(Portas), "Quantidade de portas deve estar entre 2 e 5."));
        if (Preco <= 0)
            erros.Add(Error.Validation(nameof(Preco), "Preço deve ser maior que zero."));

        return erros;
    }
}

public record RespostaCarro(
    string Id,
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    int Portas,
    decimal Preco,
    SituacaoVeiculo Situacao,
    DateTime CriadoEm)
{
    public static RespostaCarro De(Carro carro) =>
        new(carro.Id, carro.Marca, carro.Modelo, carro.Ano, carro.Cor, carro.Portas, carro.Preco, carro.Situacao, carro.CriadoEm);
}

// Indicadores nulos = serviço de Relatórios indisponível; o restante do resumo continua válido.
public record RespostaResumo(long TotalVeiculos, long Disponiveis, long Vendidos, decimal ValorEmEstoque, IndicadoresVendas? Indicadores);
