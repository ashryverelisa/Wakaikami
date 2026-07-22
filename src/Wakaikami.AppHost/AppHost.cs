using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", password: sqlPassword).WithDataVolume().WithHostPort(5550);

var loginDb = sql.AddDatabase("Login");

var migrator = builder
    .AddProject<Wakaikami_Database_Migrations>("migrator")
    .WithReference(loginDb) // -> ConnectionStrings:Login
    .WaitFor(sql);

var login = builder
    .AddProject<Wakaikami_LoginService>("login-service")
    .WithEndpoint(port: 8500, targetPort: 8500, scheme: "https", name: "grpc-intern", isProxied: false)
    .WaitForCompletion(migrator);

var world = builder
    .AddProject<Wakaikami_WorldService>("world-service")
    .WithEndpoint(port: 8510, targetPort: 8510, scheme: "https", name: "grpc-intern", isProxied: false)
    .WithEnvironment("Grpc__LoginEndpoint", login.GetEndpoint("grpc-intern"))
    .WaitForCompletion(migrator)
    .WaitFor(login);

var zone = builder.AddProject<Wakaikami_ZoneService>("zone-service").WithEnvironment("Grpc__WorldEndpoint", world.GetEndpoint("grpc-intern")).WaitFor(world);

builder.AddProject<Wakaikami_Web>("web").WaitForCompletion(migrator);

builder.Build().Run();
