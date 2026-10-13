var builder = DistributedApplication.CreateBuilder(args);

var messageBus = builder.AddRabbitMQ("Message-bus")
    .WithManagementPlugin()
    .WithDataVolume();

var usersDb = builder.AddPostgres("Users-database")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Session)
    .WithPgAdmin()
    .AddDatabase("UsersDb");

var notificationsDb = builder.AddPostgres("Notifications-database")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Session)
    .WithPgAdmin()
    .AddDatabase("NotificationsDb");

var workspacesDb = builder.AddPostgres("Workspaces-database")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Session)
    .WithPgAdmin()
    .AddDatabase("WorkspacesDb");

var usersApi = builder.AddProject<Projects.Users_API>("Users-Service")
    .WithReference(usersDb)
    .WaitFor(usersDb)
    .WithReference(messageBus)
    .WaitFor(messageBus);

var notificationsApi = builder.AddProject<Projects.Notifications_API>("Notifications-Service")
    .WithReference(notificationsDb)
    .WaitFor(notificationsDb)
    .WithReference(messageBus)
    .WaitFor(messageBus);

var workspacesApi = builder.AddProject<Projects.Workspaces_API>("Workspaces-Service")
    .WithReference(workspacesDb)
    .WithReference(usersApi)
    .WithReference(messageBus)
    .WaitFor(messageBus)
    .WaitFor(workspacesDb)
    .WaitFor(usersApi);

var gateway = builder.AddProject<Projects.gateway>("API-Gateway")
    .WithReference(usersApi)
    .WithReference(workspacesApi)
    .WaitFor(workspacesApi)
    .WaitFor(usersApi);

await builder.Build().RunAsync();
