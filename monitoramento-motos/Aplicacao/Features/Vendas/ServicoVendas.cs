using System.Diagnostics;
using Concessionaria.Motos.Aplicacao.Comum;
using Concessionaria.Motos.Aplicacao.Features.Vendas.Contratos;
using Concessionaria.Motos.Comum.Observabilidade;
using Concessionaria.Motos.Dominio.Entidades;
using Concessionaria.Motos.Dominio.Erros;
using Concessionaria.Motos.Dominio.Interfaces;
using ErrorOr;
using MongoDB.Bson;

namespace Concessionaria.Motos.Aplicacao.Features.Vendas;

/// <summary>Regras de negócio das vendas de motos.</summary>
public class ServicoVendas(IRepositorioMotos repositorioMotos, IRepositorioVendas repositorioVendas, ILogger<ServicoVendas> logger)
{
    /// <summary>Lista as vendas do período, da mais recente para a mais antiga.</summary>
    public async Task<IReadOnlyList<RespostaVenda>> ListarAsync(FiltroPeriodo filtro, CancellationToken ct)
    {
        var vendas = await repositorioVendas.ListarPorPeriodoAsync(filtro.ObterInicioUtc(), filtro.ObterFimExclusivoUtc(), ct);
        return vendas.Select(RespostaVenda.De).ToList();
    }

    /// <summary>
    /// Registra a venda: valida, marca a moto como vendida de forma atômica e grava a venda.
    /// Cada recusa é contada na métrica de vendas recusadas, com o motivo.
    /// </summary>
    public async Task<ErrorOr<RespostaVenda>> RegistrarAsync(RequisicaoVenda requisicao, CancellationToken ct)
    {
        var erros = requisicao.Validar();
        if (erros.Count > 0)
            return Recusar("validacao", erros);

        var moto = ObjectId.TryParse(requisicao.VeiculoId, out _)
            ? await repositorioMotos.ObterPorIdAsync(requisicao.VeiculoId, ct)
            : null;

        if (moto is null)
            return Recusar("nao_encontrado", [ErrosMoto.NaoEncontrado]);

        if (!await repositorioMotos.TentarMarcarComoVendidoAsync(moto.Id, ct))
        {
            logger.LogWarning("Tentativa de venda da moto {MotoId}, que já está vendida", moto.Id);
            return Recusar("ja_vendido", [ErrosMoto.JaVendido]);
        }

        var venda = new Venda
        {
            VeiculoId = moto.Id,
            DescricaoVeiculo = moto.ObterDescricao(),
            Marca = moto.Marca,
            NomeCliente = requisicao.NomeCliente.Trim(),
            DocumentoCliente = new string(requisicao.DocumentoCliente.Where(char.IsDigit).ToArray()),
            Valor = requisicao.Valor
        };

        await repositorioVendas.InserirAsync(venda, ct);
        logger.LogInformation("Venda {VendaId} registrada para a moto {MotoId} no valor de {Valor}", venda.Id, moto.Id, venda.Valor);

        Telemetria.VendasRegistradas.Add(1, Telemetria.TagTipoVeiculo);
        Telemetria.ValorVendido.Add((double)venda.Valor, Telemetria.TagTipoVeiculo);
        Activity.Current?.SetTag("concessionaria.venda_id", venda.Id);

        return RespostaVenda.De(venda);
    }

    /// <summary>Conta a venda recusada com o motivo e devolve os erros.</summary>
    private static List<Error> Recusar(string motivo, List<Error> erros)
    {
        Telemetria.VendasRecusadas.Add(1, Telemetria.TagTipoVeiculo, new KeyValuePair<string, object?>("motivo", motivo));
        return erros;
    }
}
