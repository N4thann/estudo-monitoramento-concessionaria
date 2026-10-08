using Serilog.Core;
using Serilog.Events;

namespace Concessionaria.Carros.tests.Fakes.Logging
{
    /// <summary>Sink do Serilog que guarda os eventos em memória, para conferir as propriedades de cada log.</summary>
    public sealed class ColetorLogs : ILogEventSink
    {
        /// <summary>Eventos recebidos, na ordem.</summary>
        public List<LogEvent> Eventos { get; } = [];

        /// <summary>Guarda o evento.</summary>
        public void Emit(LogEvent logEvent) => Eventos.Add(logEvent);
    }
}
