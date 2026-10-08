using Concessionaria.Motos.Comum.Observabilidade;

namespace Concessionaria.Motos.tests.Common.Observabilidade
{
    /// <summary>Uma medição capturada de um instrumento de <see cref="Telemetria"/>, com as tags copiadas.</summary>
    /// <param name="Instrumento">Nome do instrumento (ex.: concessionaria.vendas.registradas).</param>
    /// <param name="Valor">Valor medido.</param>
    /// <param name="Tags">Rótulos da medição.</param>
    public record MedicaoColetada(string Instrumento, double Valor, IReadOnlyDictionary<string, object?> Tags);
}
