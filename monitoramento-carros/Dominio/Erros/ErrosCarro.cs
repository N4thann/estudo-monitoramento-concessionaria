using ErrorOr;

namespace Concessionaria.Carros.Dominio.Erros;

/// <summary>Erros de negócio de carros.</summary>
public static class ErrosCarro
{
    /// <summary>Carro inexistente ou Id fora do formato ObjectId (404).</summary>
    public static readonly Error NaoEncontrado = Error.NotFound("Carro.NaoEncontrado", "Carro não encontrado.");

    /// <summary>Carro vendido não pode ser editado, excluído nem revendido (409).</summary>
    public static readonly Error JaVendido = Error.Conflict("Carro.JaVendido", "Carro já foi vendido.");
}
