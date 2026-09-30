using System.Diagnostics;
using System.Diagnostics.Metrics;
using Concessionaria.Relatorios.Comum.Observabilidade;

namespace Concessionaria.Relatorios.tests.Common.Observabilidade
{
    public record MedicaoColetada(string Instrumento, double Valor, IReadOnlyDictionary<string, object?> Tags);

    /// <summary>
    /// Escuta as métricas e os spans próprios de Relatórios (<see cref="Telemetria"/>) durante um teste.
    /// Os instrumentos são estáticos, então quem usa precisa estar na <see cref="ColecaoTelemetria"/> para não
    /// capturar medições de testes rodando em paralelo.
    /// </summary>
    public sealed class ColetorTelemetria : IDisposable
    {
        private readonly MeterListener _ouvinteMetricas = new();
        private readonly ActivityListener _ouvinteSpans;
        private readonly List<MedicaoColetada> _medicoes = [];
        private readonly List<Activity> _spans = [];

        public ColetorTelemetria()
        {
            _ouvinteMetricas.InstrumentPublished = (instrumento, ouvinte) =>
            {
                if (instrumento.Meter.Name == Telemetria.Nome)
                    ouvinte.EnableMeasurementEvents(instrumento);
            };
            _ouvinteMetricas.SetMeasurementEventCallback<long>((instrumento, valor, tags, _) => Registrar(instrumento, valor, tags));
            _ouvinteMetricas.SetMeasurementEventCallback<double>((instrumento, valor, tags, _) => Registrar(instrumento, valor, tags));
            _ouvinteMetricas.Start();

            _ouvinteSpans = new ActivityListener
            {
                ShouldListenTo = fonte => fonte.Name == Telemetria.Nome,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = span => { lock (_spans) _spans.Add(span); }
            };
            ActivitySource.AddActivityListener(_ouvinteSpans);
        }

        public IReadOnlyList<MedicaoColetada> Medicoes(string instrumento)
        {
            lock (_medicoes) return _medicoes.Where(m => m.Instrumento == instrumento).ToList();
        }

        public IReadOnlyList<Activity> Spans
        {
            get { lock (_spans) return _spans.ToList(); }
        }

        private void Registrar(Instrument instrumento, double valor, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var copiaTags = new Dictionary<string, object?>();
            foreach (var tag in tags) copiaTags[tag.Key] = tag.Value;
            lock (_medicoes) _medicoes.Add(new MedicaoColetada(instrumento.Name, valor, copiaTags));
        }

        public void Dispose()
        {
            _ouvinteMetricas.Dispose();
            _ouvinteSpans.Dispose();
        }
    }

    /// <summary>Testes que medem a telemetria estática rodam isolados, fora da execução paralela.</summary>
    [CollectionDefinition(Nome, DisableParallelization = true)]
    public class ColecaoTelemetria
    {
        public const string Nome = "Telemetria";
    }
}
