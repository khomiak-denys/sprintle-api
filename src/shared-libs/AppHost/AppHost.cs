var builder = DistributedApplication.CreateBuilder(args);



var usersDb = builder.AddPostgres("users-db")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Session)
    .WithPgAdmin()
    .AddDatabase("UsersDb");

var workspacesDb = builder.AddPostgres("workspaces-db")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Session)
    .WithPgAdmin()
    .AddDatabase("WorkspacesDb");

var usersApi = builder.AddProject<Projects.Users_API>("users-api")
    .WithReference(usersDb)
    .WaitFor(usersDb);

var workspacesApi = builder.AddProject<Projects.Workspaces_API>("workspaces-api")
    .WithReference(workspacesDb)
    .WithReference(usersApi)
    .WaitFor(workspacesDb)
    .WaitFor(usersApi);

var gateway = builder.AddProject<Projects.gateway>("gateway")
    .WithReference(usersApi)
    .WithReference(workspacesApi)
    .WaitFor(workspacesApi)
    .WaitFor(usersApi);

await builder.Build().RunAsync();
