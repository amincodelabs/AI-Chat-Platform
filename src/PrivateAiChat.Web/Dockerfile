FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app /home/app/.aspnet

USER app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY PrivateAiChat.sln ./
COPY src/PrivateAiChat.Web/PrivateAiChat.Web.csproj src/PrivateAiChat.Web/
COPY src/PrivateAiChat.Contracts/PrivateAiChat.Contracts.csproj src/PrivateAiChat.Contracts/

RUN dotnet restore src/PrivateAiChat.Web/PrivateAiChat.Web.csproj

COPY . .
RUN dotnet publish src/PrivateAiChat.Web/PrivateAiChat.Web.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PrivateAiChat.Web.dll"]
