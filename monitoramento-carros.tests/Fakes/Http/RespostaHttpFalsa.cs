using Microsoft.AspNetCore.Http.Features;

namespace Concessionaria.Carros.tests.Fakes.Http
{
    /// <summary>
    /// Resposta HTTP que guarda os callbacks de OnStarting para o teste disparar: o DefaultHttpContext
    /// não inicia a resposta sozinho, então o cabeçalho escrito no início dela nunca apareceria.
    /// </summary>
    public sealed class RespostaHttpFalsa : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object Estado)> _aoIniciar = [];

        /// <summary>Guarda o callback em vez de agendá-lo.</summary>
        public override void OnStarting(Func<object, Task> callback, object state) => _aoIniciar.Add((callback, state));

        /// <summary>Simula o início da resposta, disparando os callbacks guardados.</summary>
        public async Task IniciarAsync()
        {
            foreach (var (callback, estado) in _aoIniciar)
                await callback(estado);
        }
    }
}
