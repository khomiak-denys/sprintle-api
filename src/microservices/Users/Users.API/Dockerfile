FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
ENV HUSKY=0
ENV CI=true

COPY Directory.Build.props Directory.Build.targets ./
COPY src/microservices/Users/Users.API/*.csproj src/microservices/Users/Users.API/
COPY src/microservices/Users/Users.Application/*.csproj src/microservices/Users/Users.Application/
COPY src/microservices/Users/Users.Infrastructure/*.csproj src/microservices/Users/Users.Infrastructure/
COPY src/shared-libs/DomainFramework/*.csproj src/shared-libs/DomainFramework/
COPY src/shared-libs/MessageBus/*.csproj src/shared-libs/MessageBus/
COPY src/shared-libs/Messaging/*.csproj src/shared-libs/Messaging/
COPY src/shared-libs/Messaging.Contracts/*.csproj src/shared-libs/Messaging.Contracts/
COPY src/shared-libs/MigrationsExtensions/*.csproj src/shared-libs/MigrationsExtensions/
COPY src/shared-libs/ServiceDefaults/*.csproj src/shared-libs/ServiceDefaults/

RUN dotnet restore src/microservices/Users/Users.API/Users.API.csproj

COPY . .

RUN dotnet publish src/microservices/Users/Users.API/Users.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Users.API.dll"]
