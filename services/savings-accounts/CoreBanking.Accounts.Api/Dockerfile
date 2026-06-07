FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CoreBanking.slnx .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY shared/ shared/
COPY services/clients/CoreBanking.Clients.Contracts/ services/clients/CoreBanking.Clients.Contracts/
COPY services/savings-products/CoreBanking.Products.Contracts/ services/savings-products/CoreBanking.Products.Contracts/
COPY services/savings-accounts/ services/savings-accounts/
RUN dotnet restore services/savings-accounts/CoreBanking.Accounts.Api/CoreBanking.Accounts.Api.csproj
RUN dotnet publish services/savings-accounts/CoreBanking.Accounts.Api/CoreBanking.Accounts.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CoreBanking.Accounts.Api.dll"]
