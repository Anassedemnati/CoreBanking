FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CoreBanking.slnx .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY shared/ shared/
COPY services/savings-products/ services/savings-products/
RUN dotnet restore services/savings-products/CoreBanking.Products.Api/CoreBanking.Products.Api.csproj
RUN dotnet publish services/savings-products/CoreBanking.Products.Api/CoreBanking.Products.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CoreBanking.Products.Api.dll"]
