using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Aplicacao.Features.Painel.Contratos;

/// <summary>Painel da tela inicial: resumo de cada serviço e o total.</summary>
/// <param name="Carros">Resumo devolvido por Carros.</param>
/// <param name="Motos">Resumo devolvido por Motos.</param>
/// <param name="Total">Soma dos dois.</param>
public record RespostaPainel(Resumo Carros, Resumo Motos, ResumoGeral Total);
