FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
ENV HUSKY=0
ENV CI=true

COPY Directory.Build.props Directory.Build.targets ./
COPY src/gateway/*.csproj src/gateway/
COPY src/shared-libs/DomainFramework/*.csproj src/shared-libs/DomainFramework/
COPY src/shared-libs/ServiceDefaults/*.csproj src/shared-libs/ServiceDefaults/

RUN dotnet restore src/gateway/gateway.csproj

COPY . .

RUN dotnet publish src/gateway/gateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "gateway.dll"]
