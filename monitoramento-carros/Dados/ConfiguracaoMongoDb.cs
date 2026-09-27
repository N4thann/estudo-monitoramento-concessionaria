namespace Concessionaria.Carros.Dados;

public class ConfiguracaoMongoDb
{
    public const string Secao = "MongoDb";

    public string StringConexao { get; set; } = string.Empty;
    public string NomeBanco { get; set; } = string.Empty;
}
