using System.Diagnostics;
using System.Diagnostics.Metrics;
using Concessionaria.Relatorios.Comum.Observabilidade;

namespace Concessionaria.Relatorios.tests.Common.Observabilidade
{
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

        /// <summary>Começa a escutar o medidor e a fonte de spans de <see cref="Telemetria"/>.</summary>
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

        /// <summary>Medições já capturadas de um instrumento, pelo nome.</summary>
        public IReadOnlyList<MedicaoColetada> Medicoes(string instrumento)
        {
            lock (_medicoes) return _medicoes.Where(m => m.Instrumento == instrumento).ToList();
        }

        /// <summary>Spans de <see cref="Telemetria.Fonte"/> já encerrados.</summary>
        public IReadOnlyList<Activity> Spans
        {
            get { lock (_spans) return _spans.ToList(); }
        }

        /// <summary>Lê os instrumentos observáveis (gauges), como o Prometheus faria a cada scrape.</summary>
        public void ObservarInstrumentos() => _ouvinteMetricas.RecordObservableInstruments();

        /// <summary>Guarda uma medição com cópia das tags, que só valem durante o callback.</summary>
        private void Registrar(Instrument instrumento, double valor, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var copiaTags = new Dictionary<string, object?>();
            foreach (var tag in tags) copiaTags[tag.Key] = tag.Value;
            lock (_medicoes) _medicoes.Add(new MedicaoColetada(instrumento.Name, valor, copiaTags));
        }

        /// <summary>Para de escutar métricas e spans.</summary>
        public void Dispose()
        {
            _ouvinteMetricas.Dispose();
            _ouvinteSpans.Dispose();
        }
    }
}
