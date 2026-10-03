# Ultimate Hospital Management

An early .NET runtime skeleton with an HTTP host, a dedicated database migrator,
PostgreSQL persistence, OpenTelemetry support, and automated tests.
Clinical and administrative business features are not implemented.

## Run locally

Install the .NET SDK specified in `global.json` and start a Docker-compatible
container runtime. From the repository root:

```sh
dotnet restore UltimateHospital.slnx --locked-mode
dotnet build UltimateHospital.slnx -c Release --no-restore
dotnet run --project src/UltimateHospital.AppHost
```

The development AppHost starts PostgreSQL, runs the migrator, and starts the HTTP
host. The HTTP host exposes `/alive` and `/health` in Development. Supply database
credentials through the runtime environment; never commit credentials or real
patient, customer, or employee records.

## Validate

```sh
dotnet format UltimateHospital.slnx --verify-no-changes --no-restore --severity warn
dotnet test tests/UltimateHospital.ArchitectureTests -c Release --no-build --no-restore
dotnet test tests/UltimateHospital.IntegrationTests -c Release --no-build --no-restore
```

Integration tests start isolated containers and use temporary test data.
They require a running container engine and a local development certificate
(`dotnet dev-certs https`).

No project license has been selected. Refer to `THIRD-PARTY-NOTICES.md` for
dependency license information.
hee
