using Concessionaria.Carros.Aplicacao.Comum;
using Concessionaria.Carros.Aplicacao.Features.Carros.Contratos;
using Concessionaria.Carros.Aplicacao.Features.Relatorios;
using Concessionaria.Carros.Comum.Observabilidade;
using Concessionaria.Carros.Dominio.Entidades;
using Concessionaria.Carros.Dominio.Enums;
using Concessionaria.Carros.Dominio.Erros;
using Concessionaria.Carros.Dominio.Interfaces;
using Concessionaria.Carros.Dominio.Modelos;
using ErrorOr;
using MongoDB.Bson;

namespace Concessionaria.Carros.Aplicacao.Features.Carros;

/// <summary>Regras de negócio do cadastro de carros e do resumo do estoque.</summary>
public class ServicoCarros(
    IRepositorioCarros repositorioCarros,
    IRepositorioVendas repositorioVendas,
    ServicoRelatorios servicoRelatorios,
    ILogger<ServicoCarros> logger)
{
    private const int TamanhoMaximoPagina = 100;

    /// <summary>Lista os carros paginados. Página menor que 1 vira 1; tamanho fica entre 1 e 100 (padrão 10).</summary>
    public async Task<ResultadoPaginado<RespostaCarro>> ListarAsync(FiltroCarros filtro, CancellationToken ct)
    {
        var pagina = Math.Max(filtro.Pagina ?? 1, 1);
        var tamanhoPagina = Math.Clamp(filtro.TamanhoPagina ?? 10, 1, TamanhoMaximoPagina);

        var (itens, total) = await repositorioCarros.ListarPaginadoAsync(pagina, tamanhoPagina, filtro.Situacao, filtro.Busca, ct);
        return new ResultadoPaginado<RespostaCarro>(itens.Select(RespostaCarro.De).ToList(), total, pagina, tamanhoPagina);
    }

    /// <summary>Busca um carro pelo Id.</summary>
    public async Task<ErrorOr<RespostaCarro>> ObterPorIdAsync(string id, CancellationToken ct)
    {
        var carro = await BuscarAsync(id, ct);
        return carro is null ? ErrosCarro.NaoEncontrado : RespostaCarro.De(carro);
    }

    /// <summary>Valida e cadastra um carro disponível.</summary>
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

    /// <summary>Valida e atualiza um carro que ainda não foi vendido.</summary>
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

    /// <summary>Exclui um carro que ainda não foi vendido.</summary>
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

    /// <summary>
    /// Monta o resumo do estoque (Mongo) com os indicadores de vendas (Relatórios). Se Relatórios falhar,
    /// o resumo sai sem indicadores em vez de falhar inteiro.
    /// </summary>
    public async Task<RespostaResumo> ObterResumoAsync(CancellationToken ct)
    {
        // Span próprio separa, no trace, a etapa local (Mongo) da etapa remota (Relatórios).
        Task<IReadOnlyDictionary<SituacaoVeiculo, TotaisEstoque>> totaisTask;
        Task<IReadOnlyList<Venda>> vendasTask;
        using (var etapa = Telemetria.Fonte.StartActivity("Consultar estoque e vendas"))   // abre span Internal
        {
            totaisTask = repositorioCarros.ObterTotaisPorSituacaoAsync(ct);               // aggregate em "carros"
            vendasTask = repositorioVendas.ListarPorPeriodoAsync(null, null, ct);         // find em "vendas"
            await Task.WhenAll(totaisTask, vendasTask);
            etapa?.SetTag("concessionaria.quantidade_vendas", vendasTask.Result.Count);   // atributo de negócio
        }                                                                                 // fecha o span aqui

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

    /// <summary>Busca o carro; Id fora do formato ObjectId é tratado como "não encontrado" em vez de estourar exceção no driver.</summary>
    private Task<Carro?> BuscarAsync(string id, CancellationToken ct) =>
        ObjectId.TryParse(id, out _) ? repositorioCarros.ObterPorIdAsync(id, ct) : Task.FromResult<Carro?>(null);
}
