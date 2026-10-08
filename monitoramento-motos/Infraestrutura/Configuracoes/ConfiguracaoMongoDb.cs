namespace Concessionaria.Motos.Infraestrutura.Configuracoes;

/// <summary>Seção <c>MongoDb</c> do appsettings.</summary>
public class ConfiguracaoMongoDb
{
    /// <summary>Nome da seção no appsettings.</summary>
    public const string Secao = "MongoDb";

    /// <summary>String de conexão do Mongo.</summary>
    public string StringConexao { get; set; } = string.Empty;

    /// <summary>Nome do banco desta API.</summary>
    public string NomeBanco { get; set; } = string.Empty;
}
