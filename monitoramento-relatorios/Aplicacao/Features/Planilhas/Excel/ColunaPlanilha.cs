namespace Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Excel;

/// <summary>Coluna de uma aba da planilha.</summary>
/// <param name="Titulo">Texto do cabeçalho.</param>
/// <param name="Formato">Formato numérico do Excel aplicado às células (ex.: moeda); nenhum se nulo.</param>
public record ColunaPlanilha(string Titulo, string? Formato = null);
