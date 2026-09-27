using System.Diagnostics;
using Concessionaria.Carros.Comum;
using Concessionaria.Carros.Comum.Observabilidade;
using Concessionaria.Carros.Dados;
using Concessionaria.Carros.Entidades;
using Concessionaria.Carros.Modulos.Vendas;
using ErrorOr;
using MongoDB.Bson;

namespace Concessionaria.Carros.Servicos;

public class ServicoVendas(IRepositorioCarros repositorioCarros, IRepositorioVendas repositorioVendas, ILogger<ServicoVendas> logger)
{
    public async Task<IReadOnlyList<RespostaVenda>> ListarAsync(FiltroPeriodo filtro, CancellationToken ct)
    {
        var vendas = await repositorioVendas.ListarPorPeriodoAsync(filtro.ObterInicioUtc(), filtro.ObterFimExclusivoUtc(), ct);
        return vendas.Select(RespostaVenda.De).ToList();
    }

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

        Telemetria.VendasRegistradas.Add(1, Telemetria.TagTipoVeiculo);
        Telemetria.ValorVendido.Add((double)venda.Valor, Telemetria.TagTipoVeiculo);
        Activity.Current?.SetTag("concessionaria.venda_id", venda.Id);

        return RespostaVenda.De(venda);
    }

    private static List<Error> Recusar(string motivo, List<Error> erros)
    {
        Telemetria.VendasRecusadas.Add(1, Telemetria.TagTipoVeiculo, new KeyValuePair<string, object?>("motivo", motivo));
        return erros;
    }
}
