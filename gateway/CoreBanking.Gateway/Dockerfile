FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY gateway/ gateway/
RUN dotnet restore gateway/CoreBanking.Gateway/CoreBanking.Gateway.csproj
RUN dotnet publish gateway/CoreBanking.Gateway/CoreBanking.Gateway.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CoreBanking.Gateway.dll"]
