using ErrorOr;

namespace Concessionaria.Motos.Dominio.Erros;

/// <summary>Erros de negócio de motos.</summary>
public static class ErrosMoto
{
    /// <summary>Moto inexistente ou Id fora do formato ObjectId (404).</summary>
    public static readonly Error NaoEncontrado = Error.NotFound("Moto.NaoEncontrado", "Moto não encontrada.");

    /// <summary>Moto vendida não pode ser editada, excluída nem revendida (409).</summary>
    public static readonly Error JaVendido = Error.Conflict("Moto.JaVendido", "Moto já foi vendida.");
}
