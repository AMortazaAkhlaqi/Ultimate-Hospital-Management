var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithImageTag("18.6");
var database = postgres.AddDatabase("hospitaldb");

var migrator = builder.AddProject<Projects.UltimateHospital_DbMigrator>("migrator")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.UltimateHospital_HttpApi_Host>("api")
    .WithReference(database)
    .WithHttpEndpoint(name: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpHealthCheck("/health")
    .WaitFor(database)
    .WaitForCompletion(migrator);

await builder.Build().RunAsync();
