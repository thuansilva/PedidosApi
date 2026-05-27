# ============================================================
#  PedidosApi — Dockerfile multi-stage
#  Build: mcr.microsoft.com/dotnet/sdk:10.0
#  Runtime: mcr.microsoft.com/dotnet/aspnet:10.0
# ============================================================

# ── Estágio 1: Build ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Versão injetada pelo pipeline via --build-arg.
# Defaults para desenvolvimento local sem pipeline.
ARG VERSION=0.0.0
ARG ASSEMBLY_VERSION=0.0.0

# Restaura dependências antes de copiar o restante (otimiza cache)
COPY PedidosApi.csproj ./
RUN dotnet restore PedidosApi.csproj

# Copia o código-fonte (excluindo o que está no .dockerignore)
COPY . .

# Publica com a versão recebida via build args
RUN dotnet publish PedidosApi.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    -p:Version=${ASSEMBLY_VERSION} \
    -p:AssemblyVersion=${ASSEMBLY_VERSION} \
    -p:FileVersion=${ASSEMBLY_VERSION} \
    -p:InformationalVersion=${VERSION} \
    -p:IncludeSourceRevisionInInformationalVersion=false

# ── Estágio 2: Runtime ───────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Usuário não-root (UID 1000) — imagem mínima não inclui adduser,
# USER numérico não requer criação prévia do usuário no OS.
USER 1000

# ASP.NET Core 8+ usa porta 8080 por padrão (não-root friendly)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "PedidosApi.dll"]
