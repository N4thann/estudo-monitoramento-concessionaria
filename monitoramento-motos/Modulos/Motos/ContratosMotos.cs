using Concessionaria.Motos.Entidades;
using Concessionaria.Motos.Integracoes.Relatorios;
using ErrorOr;

namespace Concessionaria.Motos.Modulos.Motos;

public record FiltroMotos(int? Pagina, int? TamanhoPagina, SituacaoVeiculo? Situacao, string? Busca);

public record RequisicaoMoto(string Marca, string Modelo, int Ano, string Cor, int Cilindrada, decimal Preco)
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
        if (Cilindrada < 50 || Cilindrada > 2500)
            erros.Add(Error.Validation(nameof(Cilindrada), "Cilindrada deve estar entre 50 e 2500 cc."));
        if (Preco <= 0)
            erros.Add(Error.Validation(nameof(Preco), "Preço deve ser maior que zero."));

        return erros;
    }
}

public record RespostaMoto(
    string Id,
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    int Cilindrada,
    decimal Preco,
    SituacaoVeiculo Situacao,
    DateTime CriadoEm)
{
    public static RespostaMoto De(Moto moto) =>
        new(moto.Id, moto.Marca, moto.Modelo, moto.Ano, moto.Cor, moto.Cilindrada, moto.Preco, moto.Situacao, moto.CriadoEm);
}

// Indicadores nulos = serviço de Relatórios indisponível; o restante do resumo continua válido.
public record RespostaResumo(long TotalVeiculos, long Disponiveis, long Vendidos, decimal ValorEmEstoque, IndicadoresVendas? Indicadores);
