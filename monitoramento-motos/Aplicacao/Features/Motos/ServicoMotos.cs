using Concessionaria.Motos.Aplicacao.Comum;
using Concessionaria.Motos.Aplicacao.Features.Motos.Contratos;
using Concessionaria.Motos.Aplicacao.Features.Relatorios;
using Concessionaria.Motos.Comum.Observabilidade;
using Concessionaria.Motos.Dominio.Entidades;
using Concessionaria.Motos.Dominio.Enums;
using Concessionaria.Motos.Dominio.Erros;
using Concessionaria.Motos.Dominio.Interfaces;
using Concessionaria.Motos.Dominio.Modelos;
using ErrorOr;
using MongoDB.Bson;

namespace Concessionaria.Motos.Aplicacao.Features.Motos;

/// <summary>Regras de negócio do cadastro de motos e do resumo do estoque.</summary>
public class ServicoMotos(
    IRepositorioMotos repositorioMotos,
    IRepositorioVendas repositorioVendas,
    ServicoRelatorios servicoRelatorios,
    ILogger<ServicoMotos> logger)
{
    private const int TamanhoMaximoPagina = 100;

    /// <summary>Lista as motos paginadas. Página menor que 1 vira 1; tamanho fica entre 1 e 100 (padrão 10).</summary>
    public async Task<ResultadoPaginado<RespostaMoto>> ListarAsync(FiltroMotos filtro, CancellationToken ct)
    {
        var pagina = Math.Max(filtro.Pagina ?? 1, 1);
        var tamanhoPagina = Math.Clamp(filtro.TamanhoPagina ?? 10, 1, TamanhoMaximoPagina);

        var (itens, total) = await repositorioMotos.ListarPaginadoAsync(pagina, tamanhoPagina, filtro.Situacao, filtro.Busca, ct);
        return new ResultadoPaginado<RespostaMoto>(itens.Select(RespostaMoto.De).ToList(), total, pagina, tamanhoPagina);
    }

    /// <summary>Busca uma moto pelo Id.</summary>
    public async Task<ErrorOr<RespostaMoto>> ObterPorIdAsync(string id, CancellationToken ct)
    {
        var moto = await BuscarAsync(id, ct);
        return moto is null ? ErrosMoto.NaoEncontrado : RespostaMoto.De(moto);
    }

    /// <summary>Valida e cadastra uma moto disponível.</summary>
    public async Task<ErrorOr<RespostaMoto>> CriarAsync(RequisicaoMoto requisicao, CancellationToken ct)
    {
        var erros = requisicao.Validar();
        if (erros.Count > 0)
            return erros;

        var moto = new Moto
        {
            Marca = requisicao.Marca.Trim(),
            Modelo = requisicao.Modelo.Trim(),
            Ano = requisicao.Ano,
            Cor = requisicao.Cor.Trim(),
            Cilindrada = requisicao.Cilindrada,
            Preco = requisicao.Preco
        };

        await repositorioMotos.InserirAsync(moto, ct);
        logger.LogInformation("Moto {MotoId} cadastrada: {Descricao}", moto.Id, moto.ObterDescricao());

        return RespostaMoto.De(moto);
    }

    /// <summary>Valida e atualiza uma moto que ainda não foi vendida.</summary>
    public async Task<ErrorOr<RespostaMoto>> AtualizarAsync(string id, RequisicaoMoto requisicao, CancellationToken ct)
    {
        var erros = requisicao.Validar();
        if (erros.Count > 0)
            return erros;

        var moto = await BuscarAsync(id, ct);
        if (moto is null)
            return ErrosMoto.NaoEncontrado;
        if (moto.Situacao == SituacaoVeiculo.Vendido)
            return ErrosMoto.JaVendido;

        moto.Marca = requisicao.Marca.Trim();
        moto.Modelo = requisicao.Modelo.Trim();
        moto.Ano = requisicao.Ano;
        moto.Cor = requisicao.Cor.Trim();
        moto.Cilindrada = requisicao.Cilindrada;
        moto.Preco = requisicao.Preco;
        moto.AtualizadoEm = DateTime.UtcNow;

        await repositorioMotos.SubstituirAsync(moto, ct);
        logger.LogInformation("Moto {MotoId} atualizada", moto.Id);

        return RespostaMoto.De(moto);
    }

    /// <summary>Exclui uma moto que ainda não foi vendida.</summary>
    public async Task<ErrorOr<Deleted>> ExcluirAsync(string id, CancellationToken ct)
    {
        var moto = await BuscarAsync(id, ct);
        if (moto is null)
            return ErrosMoto.NaoEncontrado;
        if (moto.Situacao == SituacaoVeiculo.Vendido)
            return ErrosMoto.JaVendido;

        await repositorioMotos.ExcluirAsync(id, ct);
        logger.LogInformation("Moto {MotoId} excluída", id);

        return Result.Deleted;
    }

    /// <summary>
    /// Monta o resumo do estoque (Mongo) com os indicadores de vendas (Relatórios). Se Relatórios falhar,
    /// o resumo sai sem indicadores em vez de falhar inteiro.
    /// </summary>
    public async Task<RespostaResumo> ObterResumoAsync(CancellationToken ct)
    {
        // Span próprio separa, no trace, a etapa local (Mongo) da etapa remota (Relatórios).
        Task<IReadOnlyDictionary<SituacaoVeiculo, TotaisEstoque>> totaisTask;
        Task<IReadOnlyList<Venda>> vendasTask;
        using (var etapa = Telemetria.Fonte.StartActivity("Consultar estoque e vendas"))
        {
            totaisTask = repositorioMotos.ObterTotaisPorSituacaoAsync(ct);
            vendasTask = repositorioVendas.ListarPorPeriodoAsync(null, null, ct);
            await Task.WhenAll(totaisTask, vendasTask);
            etapa?.SetTag("concessionaria.quantidade_vendas", vendasTask.Result.Count);
        }

        var totais = totaisTask.Result;
        var disponiveis = totais.GetValueOrDefault(SituacaoVeiculo.Disponivel) ?? new TotaisEstoque(0, 0m);
        var vendidos = totais.GetValueOrDefault(SituacaoVeiculo.Vendido) ?? new TotaisEstoque(0, 0m);

        var indicadores = await servicoRelatorios.CalcularIndicadoresAsync(vendasTask.Result, ct);

        return new RespostaResumo(
            TotalVeiculos: disponiveis.Quantidade + vendidos.Quantidade,
            Disponiveis: disponiveis.Quantidade,
            Vendidos: vendidos.Quantidade,
            ValorEmEstoque: disponiveis.ValorTotal,
            Indicadores: indicadores);
    }

    /// <summary>Busca a moto; Id fora do formato ObjectId é tratado como "não encontrado" em vez de estourar exceção no driver.</summary>
    private Task<Moto?> BuscarAsync(string id, CancellationToken ct) =>
        ObjectId.TryParse(id, out _) ? repositorioMotos.ObterPorIdAsync(id, ct) : Task.FromResult<Moto?>(null);
}
