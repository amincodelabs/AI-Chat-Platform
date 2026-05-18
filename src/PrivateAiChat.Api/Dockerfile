FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app /home/app/.aspnet

USER app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY PrivateAiChat.sln ./
COPY src/PrivateAiChat.Api/PrivateAiChat.Api.csproj src/PrivateAiChat.Api/
COPY src/PrivateAiChat.Application/PrivateAiChat.Application.csproj src/PrivateAiChat.Application/
COPY src/PrivateAiChat.Contracts/PrivateAiChat.Contracts.csproj src/PrivateAiChat.Contracts/
COPY src/PrivateAiChat.Domain/PrivateAiChat.Domain.csproj src/PrivateAiChat.Domain/
COPY src/PrivateAiChat.Infrastructure/PrivateAiChat.Infrastructure.csproj src/PrivateAiChat.Infrastructure/

RUN dotnet restore src/PrivateAiChat.Api/PrivateAiChat.Api.csproj

COPY . .
RUN dotnet publish src/PrivateAiChat.Api/PrivateAiChat.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PrivateAiChat.Api.dll"]
