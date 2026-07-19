using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", password: sqlPassword).WithDataVolume().WithHostPort(5550);

var login = builder
    .AddProject<Wakaikami_LoginService>("login-service")
    .WithEndpoint(port: 8500, targetPort: 8500, scheme: "https", name: "grpc-intern", isProxied: false);

var world = builder
    .AddProject<Wakaikami_WorldService>("world-service")
    .WithEndpoint(port: 8510, targetPort: 8510, scheme: "https", name: "grpc-intern", isProxied: false)
    .WithEnvironment("Grpc__LoginEndpoint", login.GetEndpoint("grpc-intern"))
    .WaitFor(login);

var zone = builder.AddProject<Wakaikami_ZoneService>("zone-service").WithEnvironment("Grpc__WorldEndpoint", world.GetEndpoint("grpc-intern")).WaitFor(world);

builder.Build().Run();
