FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CoreBanking.slnx .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY shared/ shared/
COPY services/clients/ services/clients/
RUN dotnet restore services/clients/CoreBanking.Clients.Api/CoreBanking.Clients.Api.csproj
RUN dotnet publish services/clients/CoreBanking.Clients.Api/CoreBanking.Clients.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CoreBanking.Clients.Api.dll"]
