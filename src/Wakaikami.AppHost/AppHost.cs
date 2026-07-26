using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", password: sqlPassword).WithDataVolume().WithHostPort(5550);

var loginDb = sql.AddDatabase("Login");
var worldDb = sql.AddDatabase("World");

var migrator = builder
    .AddProject<Wakaikami_Database_Migrations>("migrator")
    .WithReference(loginDb) // -> ConnectionStrings:Login
    .WithReference(worldDb) // -> ConnectionStrings:World
    .WaitFor(sql);

// Distinct Prometheus ports: all services share the host machine here, unlike in k8s.
var login = builder
    .AddProject<Wakaikami_LoginService>("login-service")
    .WithEnvironment("Login__ConnectionString__Login", loginDb)
    .WithEnvironment("Metrics__Port", "9464")
    .WithEndpoint(port: 8500, targetPort: 8500, scheme: "https", name: "grpc-intern", isProxied: false)
    .WaitForCompletion(migrator);

var world = builder
    .AddProject<Wakaikami_WorldService>("world-service")
    .WithReference(worldDb) // -> ConnectionStrings:World
    .WithEndpoint(port: 8510, targetPort: 8510, scheme: "https", name: "grpc-intern", isProxied: false)
    .WithEnvironment("Grpc__LoginEndpoint", login.GetEndpoint("grpc-intern"))
    .WithEnvironment("Metrics__Port", "9465")
    .WaitForCompletion(migrator)
    .WaitFor(login);

var zone = builder
    .AddProject<Wakaikami_ZoneService>("zone-service")
    .WithReference(worldDb) // -> ConnectionStrings:World
    .WithEnvironment("Grpc__WorldEndpoint", world.GetEndpoint("grpc-intern"))
    .WithEnvironment("Metrics__Port", "9466")
    .WaitForCompletion(migrator)
    .WaitFor(world);

builder
    .AddProject<Wakaikami_Web>("web")
    .WithReference(loginDb) // -> ConnectionStrings:Login
    .WaitForCompletion(migrator);

builder.Build().Run();
