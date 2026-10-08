namespace Concessionaria.Carros.tests.Common.Observabilidade
{
    /// <summary>Testes que medem a telemetria estática rodam isolados, fora da execução paralela.</summary>
    [CollectionDefinition(Nome, DisableParallelization = true)]
    public class ColecaoTelemetria
    {
        /// <summary>Nome da coleção usado em <c>[Collection]</c>.</summary>
        public const string Nome = "Telemetria";
    }
}
