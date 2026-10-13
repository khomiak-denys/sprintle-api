FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
ENV HUSKY=0
ENV CI=true

COPY Directory.Build.props Directory.Build.targets ./
COPY src/microservices/Notifications/Notifications.API/*.csproj src/microservices/Notifications/Notifications.API/
COPY src/microservices/Notifications/Notifications.Application/*.csproj src/microservices/Notifications/Notifications.Application/
COPY src/microservices/Notifications/Notifications.Infrastructure/*.csproj src/microservices/Notifications/Notifications.Infrastructure/
COPY src/microservices/Notifications/Notifications.Domain/*.csproj src/microservices/Notifications/Notifications.Domain/
COPY src/shared-libs/DomainFramework/*.csproj src/shared-libs/DomainFramework/
COPY src/shared-libs/EmailTemplates/*.csproj src/shared-libs/EmailTemplates/
COPY src/shared-libs/MessageBus/*.csproj src/shared-libs/MessageBus/
COPY src/shared-libs/Messaging/*.csproj src/shared-libs/Messaging/
COPY src/shared-libs/Messaging.Contracts/*.csproj src/shared-libs/Messaging.Contracts/
COPY src/shared-libs/MigrationsExtensions/*.csproj src/shared-libs/MigrationsExtensions/
COPY src/shared-libs/ServiceDefaults/*.csproj src/shared-libs/ServiceDefaults/

RUN dotnet restore src/microservices/Notifications/Notifications.API/Notifications.API.csproj

COPY . .

RUN dotnet publish src/microservices/Notifications/Notifications.API/Notifications.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Notifications.API.dll"]
