using ErrorOr;

namespace Concessionaria.Carros.Aplicacao.Features.Carros.Contratos;

/// <summary>Dados para cadastrar ou atualizar um carro.</summary>
/// <param name="Marca">Fabricante (até 60 caracteres).</param>
/// <param name="Modelo">Modelo (até 60 caracteres).</param>
/// <param name="Ano">Ano do modelo, de 1950 até o ano que vem.</param>
/// <param name="Cor">Cor (até 30 caracteres).</param>
/// <param name="Portas">Quantidade de portas, de 2 a 5.</param>
/// <param name="Preco">Preço, maior que zero.</param>
public record RequisicaoCarro(string Marca, string Modelo, int Ano, string Cor, int Portas, decimal Preco)
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
        if (Portas < 2 || Portas > 5)
            erros.Add(Error.Validation(nameof(Portas), "Quantidade de portas deve estar entre 2 e 5."));
        if (Preco <= 0)
            erros.Add(Error.Validation(nameof(Preco), "Preço deve ser maior que zero."));

        return erros;
    }
}
