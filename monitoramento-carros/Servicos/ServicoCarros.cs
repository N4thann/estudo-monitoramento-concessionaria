using Concessionaria.Carros.Comum;
using Concessionaria.Carros.Comum.Observabilidade;
using Concessionaria.Carros.Dados;
using Concessionaria.Carros.Entidades;
using Concessionaria.Carros.Modulos.Carros;
using ErrorOr;
using MongoDB.Bson;

namespace Concessionaria.Carros.Servicos;

public class ServicoCarros(
    IRepositorioCarros repositorioCarros,
    IRepositorioVendas repositorioVendas,
    ServicoRelatorios servicoRelatorios,
    ILogger<ServicoCarros> logger)
{
    private const int TamanhoMaximoPagina = 100;

    public async Task<ResultadoPaginado<RespostaCarro>> ListarAsync(FiltroCarros filtro, CancellationToken ct)
    {
        var pagina = Math.Max(filtro.Pagina ?? 1, 1);
        var tamanhoPagina = Math.Clamp(filtro.TamanhoPagina ?? 10, 1, TamanhoMaximoPagina);

        var (itens, total) = await repositorioCarros.ListarPaginadoAsync(pagina, tamanhoPagina, filtro.Situacao, filtro.Busca, ct);
        return new ResultadoPaginado<RespostaCarro>(itens.Select(RespostaCarro.De).ToList(), total, pagina, tamanhoPagina);
    }

    public async Task<ErrorOr<RespostaCarro>> ObterPorIdAsync(string id, CancellationToken ct)
    {
        var carro = await BuscarAsync(id, ct);
        return carro is null ? ErrosCarro.NaoEncontrado : RespostaCarro.De(carro);
    }

    public async Task<ErrorOr<RespostaCarro>> CriarAsync(RequisicaoCarro requisicao, CancellationToken ct)
    {
        var erros = requisicao.Validar();
        if (erros.Count > 0)
            return erros;

        var carro = new Carro
        {
            Marca = requisicao.Marca.Trim(),
            Modelo = requisicao.Modelo.Trim(),
            Ano = requisicao.Ano,
            Cor = requisicao.Cor.Trim(),
            Portas = requisicao.Portas,
            Preco = requisicao.Preco
        };

        await repositorioCarros.InserirAsync(carro, ct);
        logger.LogInformation("Carro {CarroId} cadastrado: {Descricao}", carro.Id, carro.ObterDescricao());

        return RespostaCarro.De(carro);
    }

    public async Task<ErrorOr<RespostaCarro>> AtualizarAsync(string id, RequisicaoCarro requisicao, CancellationToken ct)
    {
        var erros = requisicao.Validar();
        if (erros.Count > 0)
            return erros;

        var carro = await BuscarAsync(id, ct);
        if (carro is null)
            return ErrosCarro.NaoEncontrado;
        if (carro.Situacao == SituacaoVeiculo.Vendido)
            return ErrosCarro.JaVendido;

        carro.Marca = requisicao.Marca.Trim();
        carro.Modelo = requisicao.Modelo.Trim();
        carro.Ano = requisicao.Ano;
        carro.Cor = requisicao.Cor.Trim();
        carro.Portas = requisicao.Portas;
        carro.Preco = requisicao.Preco;
        carro.AtualizadoEm = DateTime.UtcNow;

        await repositorioCarros.SubstituirAsync(carro, ct);
        logger.LogInformation("Carro {CarroId} atualizado", carro.Id);

        return RespostaCarro.De(carro);
    }

    public async Task<ErrorOr<Deleted>> ExcluirAsync(string id, CancellationToken ct)
    {
        var carro = await BuscarAsync(id, ct);
        if (carro is null)
            return ErrosCarro.NaoEncontrado;
        if (carro.Situacao == SituacaoVeiculo.Vendido)
            return ErrosCarro.JaVendido;

        await repositorioCarros.ExcluirAsync(id, ct);
        logger.LogInformation("Carro {CarroId} excluído", id);

        return Result.Deleted;
    }

    public async Task<RespostaResumo> ObterResumoAsync(CancellationToken ct)
    {
        // Span próprio separa, no trace, a etapa local (Mongo) da etapa remota (Relatórios).
        Task<IReadOnlyDictionary<SituacaoVeiculo, TotaisEstoque>> totaisTask;
        Task<IReadOnlyList<Venda>> vendasTask;
        using (var etapa = Telemetria.Fonte.StartActivity("Consultar estoque e vendas"))
        {
            totaisTask = repositorioCarros.ObterTotaisPorSituacaoAsync(ct);
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

    // Id fora do formato ObjectId é tratado como "não encontrado" em vez de estourar exceção no driver.
    private Task<Carro?> BuscarAsync(string id, CancellationToken ct) =>
        ObjectId.TryParse(id, out _) ? repositorioCarros.ObterPorIdAsync(id, ct) : Task.FromResult<Carro?>(null);
}
