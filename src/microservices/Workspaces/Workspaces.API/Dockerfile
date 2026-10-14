FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
ENV HUSKY=0
ENV CI=true

COPY Directory.Build.props Directory.Build.targets ./
COPY src/microservices/Workspaces/Workspaces.API/*.csproj src/microservices/Workspaces/Workspaces.API/
COPY src/microservices/Workspaces/Workspaces.Application/*.csproj src/microservices/Workspaces/Workspaces.Application/
COPY src/microservices/Workspaces/Workspaces.Infrastructure/*.csproj src/microservices/Workspaces/Workspaces.Infrastructure/
COPY src/microservices/Workspaces/Workspaces.Domain/*.csproj src/microservices/Workspaces/Workspaces.Domain/
COPY src/shared-libs/DomainFramework/*.csproj src/shared-libs/DomainFramework/
COPY src/shared-libs/MessageBus/*.csproj src/shared-libs/MessageBus/
COPY src/shared-libs/Messaging/*.csproj src/shared-libs/Messaging/
COPY src/shared-libs/Messaging.Contracts/*.csproj src/shared-libs/Messaging.Contracts/
COPY src/shared-libs/MigrationsExtensions/*.csproj src/shared-libs/MigrationsExtensions/
COPY src/shared-libs/ServiceDefaults/*.csproj src/shared-libs/ServiceDefaults/

RUN dotnet restore src/microservices/Workspaces/Workspaces.API/Workspaces.API.csproj

COPY . .

RUN dotnet publish src/microservices/Workspaces/Workspaces.API/Workspaces.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Workspaces.API.dll"]
