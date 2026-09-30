# Imagem genérica das 4 APIs. O contexto de build é a raiz do repositório (precisa do nuget.config).
#   docker build -f docker/api.Dockerfile --build-arg API=monitoramento-carros .
ARG API

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG API
WORKDIR /src
COPY nuget.config .
# O restore vem numa camada separada para ser reaproveitado do cache quando só o código muda.
COPY ${API}/${API}.csproj ${API}/
RUN dotnet restore ${API}/${API}.csproj
COPY ${API}/ ${API}/
RUN dotnet publish ${API}/${API}.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG API
ENV ASSEMBLY=${API}.dll
WORKDIR /app
COPY --from=build /app .
# O Serilog grava logs/{aplicacao}-.txt; o usuário padrão da imagem (app) não tem permissão de escrita em /app.
RUN mkdir logs && chown "$APP_UID" logs
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet \"$ASSEMBLY\""]
