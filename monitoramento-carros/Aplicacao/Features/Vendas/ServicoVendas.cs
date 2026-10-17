using System.Diagnostics;
using Concessionaria.Carros.Aplicacao.Comum;
using Concessionaria.Carros.Aplicacao.Features.Vendas.Contratos;
using Concessionaria.Carros.Comum.Observabilidade;
using Concessionaria.Carros.Dominio.Entidades;
using Concessionaria.Carros.Dominio.Erros;
using Concessionaria.Carros.Dominio.Interfaces;
using ErrorOr;
using MongoDB.Bson;

namespace Concessionaria.Carros.Aplicacao.Features.Vendas;

/// <summary>Regras de negócio das vendas de carros.</summary>
public class ServicoVendas(IRepositorioCarros repositorioCarros, IRepositorioVendas repositorioVendas, ILogger<ServicoVendas> logger)
{
    /// <summary>Lista as vendas do período, da mais recente para a mais antiga.</summary>
    public async Task<IReadOnlyList<RespostaVenda>> ListarAsync(FiltroPeriodo filtro, CancellationToken ct)
    {
        var vendas = await repositorioVendas.ListarPorPeriodoAsync(filtro.ObterInicioUtc(), filtro.ObterFimExclusivoUtc(), ct);
        return vendas.Select(RespostaVenda.De).ToList();
    }

    /// <summary>
    /// Registra a venda: valida, marca o carro como vendido de forma atômica e grava a venda.
    /// Cada recusa é contada na métrica de vendas recusadas, com o motivo.
    /// </summary>
    public async Task<ErrorOr<RespostaVenda>> RegistrarAsync(RequisicaoVenda requisicao, CancellationToken ct)
    {
        var erros = requisicao.Validar();
        if (erros.Count > 0)
            return Recusar("validacao", erros);

        var carro = ObjectId.TryParse(requisicao.VeiculoId, out _)
            ? await repositorioCarros.ObterPorIdAsync(requisicao.VeiculoId, ct)
            : null;

        if (carro is null)
            return Recusar("nao_encontrado", [ErrosCarro.NaoEncontrado]);

        if (!await repositorioCarros.TentarMarcarComoVendidoAsync(carro.Id, ct))
        {
            logger.LogWarning("Tentativa de venda do carro {CarroId}, que já está vendido", carro.Id);
            return Recusar("ja_vendido", [ErrosCarro.JaVendido]);
        }

        var venda = new Venda
        {
            VeiculoId = carro.Id,
            DescricaoVeiculo = carro.ObterDescricao(),
            Marca = carro.Marca,
            NomeCliente = requisicao.NomeCliente.Trim(),
            DocumentoCliente = new string(requisicao.DocumentoCliente.Where(char.IsDigit).ToArray()),
            Valor = requisicao.Valor
        };

        await repositorioVendas.InserirAsync(venda, ct);
        logger.LogInformation("Venda {VendaId} registrada para o carro {CarroId} no valor de {Valor}", venda.Id, carro.Id, venda.Valor);

        Telemetria.VendasRegistradas.Add(1, Telemetria.TagTipoVeiculo);                // +1, rótulo tipo_veiculo="carro"
        Telemetria.ValorVendido.Add((double)venda.Valor, Telemetria.TagTipoVeiculo);   // soma o valor
        Activity.Current?.SetTag("concessionaria.venda_id", venda.Id);                 // e marca o span com o id da venda

        return RespostaVenda.De(venda);
    }

    /// <summary>Conta a venda recusada com o motivo e devolve os erros.</summary>
    private static List<Error> Recusar(string motivo, List<Error> erros)
    {
        Telemetria.VendasRecusadas.Add(1, Telemetria.TagTipoVeiculo, new KeyValuePair<string, object?>("motivo", motivo));
        return erros;
    }
}
