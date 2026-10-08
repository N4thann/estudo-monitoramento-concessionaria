using ErrorOr;

namespace Concessionaria.Motos.Aplicacao.Features.Motos.Contratos;

/// <summary>Dados para cadastrar ou atualizar uma moto.</summary>
/// <param name="Marca">Fabricante (até 60 caracteres).</param>
/// <param name="Modelo">Modelo (até 60 caracteres).</param>
/// <param name="Ano">Ano do modelo, de 1950 até o ano que vem.</param>
/// <param name="Cor">Cor (até 30 caracteres).</param>
/// <param name="Cilindrada">Cilindrada em cc, de 50 a 2500.</param>
/// <param name="Preco">Preço, maior que zero.</param>
public record RequisicaoMoto(string Marca, string Modelo, int Ano, string Cor, int Cilindrada, decimal Preco)
{
    /// <summary>Valida os campos; lista vazia significa requisição válida. O código de cada erro é o nome do campo.</summary>
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
