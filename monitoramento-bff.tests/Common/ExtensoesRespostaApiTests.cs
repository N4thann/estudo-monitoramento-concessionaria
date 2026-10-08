using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;
using Concessionaria.Bff.tests.Common.Refit;
using Concessionaria.Bff.tests.DataBuilder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Refit;
using Shouldly;

namespace Concessionaria.Bff.tests.Common
{
    /// <summary>
    /// Conversão da resposta de Carros/Motos em resposta do BFF: sucesso repassado, 4xx como veio, 5xx vira 502
    /// mantendo o corpo, e falta de resposta relançada para o manipulador de falha de integração.
    /// </summary>
    public class ExtensoesRespostaApiTests
    {
        private const string ProblemaConflito = """{"title":"Carro já foi vendido.","status":409,"codigo":"Carro.JaVendido"}""";

        [Fact(DisplayName = "SUCESSO - Deve devolver 200 com o conteúdo quando o serviço responde com sucesso")]
        public void ParaResultado_WhenResponseIsSuccessful_ShouldReturnOkWithContent()
        {
            // Arrange
            var carro = CarroDataBuilder.Create().Build();

            // Act
            var resultado = RespostasApi.Sucesso(carro).ParaResultado();

            // Assert
            resultado.ShouldBeOfType<Ok<Carro>>().Value.ShouldBe(carro);
        }

        [Fact(DisplayName = "SUCESSO - Deve usar o resultado informado para o sucesso, como o 201 do cadastro")]
        public void ParaResultado_WhenSuccessHandlerIsInformed_ShouldUseIt()
        {
            // Arrange
            var carro = CarroDataBuilder.Create().Build();

            // Act
            var resultado = RespostasApi.Sucesso(carro).ParaResultado(c => Results.Created($"/api/carros/{c.Id}", c));

            // Assert
            resultado.ShouldBeOfType<Created<Carro>>().Location.ShouldBe($"/api/carros/{carro.Id}");
        }

        [Fact(DisplayName = "ERRO - Deve repassar 4xx com o mesmo status e o ProblemDetails original do serviço")]
        public async Task ParaResultado_WhenServiceReturnsClientError_ShouldKeepStatusAndBody()
        {
            // Arrange
            var resposta = await RespostasApi.ErroHttpAsync<Carro>(HttpStatusCode.Conflict, ProblemaConflito);

            // Act
            var resultado = resposta.ParaResultado();

            // Assert
            var conteudo = resultado.ShouldBeOfType<ContentHttpResult>();
            conteudo.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
            conteudo.ContentType!.ShouldStartWith("application/problem+json");
            conteudo.ResponseContent.ShouldBe(ProblemaConflito);
        }

        [Fact(DisplayName = "ERRO - Deve transformar 5xx do serviço em 502, mantendo o ProblemDetails original")]
        public async Task ParaResultado_WhenServiceReturnsServerError_ShouldReturnBadGatewayWithOriginalBody()
        {
            // Arrange
            const string corpo = """{"title":"Não foi possível gerar a planilha no serviço de relatórios.","status":502}""";
            var resposta = await RespostasApi.ErroHttpAsync<Carro>(HttpStatusCode.InternalServerError, corpo);

            // Act
            var resultado = resposta.ParaResultado();

            // Assert
            var conteudo = resultado.ShouldBeOfType<ContentHttpResult>();
            conteudo.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
            conteudo.ResponseContent.ShouldBe(corpo);
        }

        [Fact(DisplayName = "ERRO - Deve montar um ProblemDetails 502 quando o 5xx do serviço vem sem corpo")]
        public async Task ParaResultado_WhenServerErrorHasNoBody_ShouldReturnBadGatewayProblem()
        {
            // Arrange
            var resposta = await RespostasApi.ErroHttpAsync<Carro>(HttpStatusCode.ServiceUnavailable);

            // Act
            var resultado = resposta.ParaResultado();

            // Assert
            var problema = resultado.ShouldBeOfType<ProblemHttpResult>();
            problema.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
            problema.ProblemDetails.Title.ShouldBe("Serviço dependente respondeu com erro 503.");
        }

        [Fact(DisplayName = "ERRO - Deve relançar a falha de rede para o manipulador de falha de integração")]
        public void ParaResultado_WhenThereIsNoResponse_ShouldRethrowRefitException()
        {
            // Arrange
            var resposta = RespostasApi.FalhaRede<Carro>(new HttpRequestException("Conexão recusada"));

            // Act & Assert
            var excecao = Should.Throw<ApiRequestException>(() => resposta.ParaResultado());
            excecao.InnerException.ShouldBeOfType<HttpRequestException>();
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver 204 quando a exclusão dá certo")]
        public void ParaResultadoSemConteudo_WhenResponseIsSuccessful_ShouldReturnNoContent()
        {
            // Act
            var resultado = RespostasApi.SemConteudo().ParaResultadoSemConteudo();

            // Assert
            resultado.ShouldBeOfType<NoContent>();
        }

        [Fact(DisplayName = "ERRO - Deve repassar o 409 da exclusão de veículo já vendido")]
        public async Task ParaResultadoSemConteudo_WhenServiceReturnsConflict_ShouldKeepStatus()
        {
            // Arrange
            var resposta = await RespostasApi.ErroHttpAsync<object>(HttpStatusCode.Conflict, ProblemaConflito);

            // Act
            var resultado = resposta.ParaResultadoSemConteudo();

            // Assert
            resultado.ShouldBeOfType<ContentHttpResult>().StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver o arquivo com o nome e o tipo de conteúdo enviados pelo serviço")]
        public async Task ParaArquivoAsync_WhenServiceSendsFile_ShouldKeepNameAndContentType()
        {
            // Arrange
            var bytes = Encoding.UTF8.GetBytes("planilha");
            var disposicao = new ContentDispositionHeaderValue("attachment") { FileName = "\"relatorio-vendas-carros.xlsx\"" };
            var resposta = RespostasApi.Arquivo(bytes, disposicao, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            // Act
            var resultado = await resposta.ParaArquivoAsync(CancellationToken.None);

            // Assert
            var arquivo = resultado.ShouldBeOfType<FileContentHttpResult>();
            arquivo.FileContents.ToArray().ShouldBe(bytes);
            arquivo.FileDownloadName.ShouldBe("relatorio-vendas-carros.xlsx");
            arquivo.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        [Fact(DisplayName = "SUCESSO - Deve preferir o nome com acento (filename*) quando o serviço envia os dois")]
        public async Task ParaArquivoAsync_WhenBothFileNamesAreSent_ShouldPreferFileNameStar()
        {
            // Arrange
            var disposicao = new ContentDispositionHeaderValue("attachment") { FileName = "\"estoque.xlsx\"", FileNameStar = "relatório-estoque.xlsx" };
            var resposta = RespostasApi.Arquivo([1, 2, 3], disposicao);

            // Act
            var resultado = await resposta.ParaArquivoAsync(CancellationToken.None);

            // Assert
            resultado.ShouldBeOfType<FileContentHttpResult>().FileDownloadName.ShouldBe("relatório-estoque.xlsx");
        }

        [Fact(DisplayName = "SUCESSO - Deve usar nome e tipo padrão quando o serviço não envia os cabeçalhos")]
        public async Task ParaArquivoAsync_WhenHeadersAreMissing_ShouldUseDefaults()
        {
            // Arrange
            var resposta = RespostasApi.Arquivo([1, 2, 3]);

            // Act
            var resultado = await resposta.ParaArquivoAsync(CancellationToken.None);

            // Assert
            var arquivo = resultado.ShouldBeOfType<FileContentHttpResult>();
            arquivo.FileDownloadName.ShouldBe("relatorio.xlsx");
            arquivo.ContentType.ShouldBe("application/octet-stream");
        }

        [Fact(DisplayName = "ERRO - Deve transformar em 502 a falha do serviço ao gerar a planilha")]
        public async Task ParaArquivoAsync_WhenServiceFails_ShouldReturnBadGateway()
        {
            // Arrange
            var resposta = await RespostasApi.ErroHttpAsync<Stream>(HttpStatusCode.BadGateway, """{"title":"Relatórios fora","status":502}""");

            // Act
            var resultado = await resposta.ParaArquivoAsync(CancellationToken.None);

            // Assert
            resultado.ShouldBeOfType<ContentHttpResult>().StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
        }
    }
}
