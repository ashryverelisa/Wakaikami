# Wakaikami

A modern, distributed server emulator, built with **C# / .NET 10**. The server cluster is split into independent services (Login, World, Zone) that communicate over mutual-TLS gRPC, while game clients connect through a custom TCP network layer implementing the Fiesta packet protocol.

## Architecture

```
                        ┌─────────────┐
  Game Client ──TCP──►  │ LoginService│ ◄──┐
      │                 └─────────────┘    │ gRPC (mTLS)
      │                 ┌─────────────┐    │
      ├───────TCP────►  │ WorldService│ ◄──┘◄──┐
      │                 └─────────────┘        │ gRPC (mTLS)
      │                 ┌─────────────┐        │
      └───────TCP────►  │ ZoneService │ ───────┘
                        └─────────────┘
                              │
                     SQL Server (Login / World DBs)
```

| Project | Purpose |
|---|---|
| `Wakaikami.AppHost` | .NET Aspire orchestrator starts SQL Server (container), the migrator, and all services in the right order |
| `Wakaikami.LoginService` | Handles client authentication, account sessions, and world server registration |
| `Wakaikami.WorldService` | World/character server; registers itself at the Login service and manages zones |
| `Wakaikami.ZoneService` | Zone (map) server; registers itself at the World service |
| `Wakaikami.Web` | Blazor web frontend for account registration |
| `Wakaikami.Database` | EF Core entities, `DbContext`s, and Dapper-based repositories (Login & World databases) |
| `Wakaikami.Database.Migrations` | Standalone EF Core migration runner (incl. custom migrations / stored procedures) |
| `Wakaikami.Networking` | Game TCP network layer: sessions, listeners, packet reader/writer, Fiesta protocol & crypto |
| `Wakaikami.Networking.Grpc` | Shared gRPC infrastructure: proto contracts, push-stream hub, internal control service |
| `Wakaikami.Networking.Generators` | Roslyn source generators for packet serialization and handler registration |
| `Wakaikami.Content` | Shared game domain types (accounts, sessions, server status) |
| `Wakaikami.Core` | Cross-cutting concerns: hosting modules, security (BCrypt password hashing), time, helpers |
| `Wakaikami.Networking.Tests` | Unit tests for the networking layer (packet reader/writer, handler stores) |

### Key design points

- **Service clustering via gRPC** Zone registers at World, World registers at Login. Server-side push streams (`PushHub` / `PushStreamServer`) propagate state (e.g. account sessions, server status) across the cluster.
- **Mutual TLS** all internal gRPC traffic is secured with certificates (`src/certs`, dev-only certs included).
- **Source-generated packet handling** packet serialization and handler dispatch code is generated at compile time from attributes (`[GenerateSerialization]`, `[Field]`, …), avoiding reflection at runtime.
- **Password security** BCrypt with a configurable work factor and a server-side pepper (`PasswordHashing:Pepper`; the dev value **must** be overridden in production).
- **Central package management** all NuGet versions are pinned in `Directory.Packages.props`; analyzers (NetAnalyzers, Roslynator, Meziantou) are enforced build-wide.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (or Podman) the AppHost runs SQL Server as a container

## Getting started

The easiest way to run the full cluster is through the Aspire AppHost:

```bash
cd src
dotnet run --project Wakaikami.AppHost
```

This will:

1. Start a SQL Server container (host port `5550`) and create the `Login` and `World` databases
2. Run `Wakaikami.Database.Migrations` to apply all EF Core migrations and custom stored procedures
3. Start the Login, World, and Zone services (in dependency order) plus the web frontend
4. Open the Aspire dashboard with logs, traces, and metrics (OpenTelemetry) for every service

On first run, Aspire prompts for the `sql-password` secret parameter.

Services can also be run standalone (`dotnet run --project Wakaikami.LoginService`, etc.); in that case configure the connection strings and endpoints in each service's `appsettings.json` yourself.

### Default ports

| Endpoint | Port |
|---|---|
| Login game TCP | `9010` |
| World game TCP | `9120` |
| Zone game TCP | `9230` |
| Login internal gRPC | `8500` |
| World internal gRPC | `8510` |
| SQL Server (Aspire) | `5550` |

## Configuration

Each service is configured via its `appsettings.json` (overridable per environment or through environment variables, e.g. `Login__ConnectionString__Login`). Notable settings:

- `Login:ClientVersion` MD5 hash of the accepted game client version
- `PasswordHashing:Pepper` / `PasswordHashing:WorkFactor` password hashing parameters (shared between LoginService and the web registration)
- `World:WorldInfo` world name, ID, connect IP, and player limits
- `Grpc:*` internal gRPC ports and upstream endpoints

## Tests

```bash
cd src
dotnet test
```

Test runs collect code coverage via Coverlet (`coverlet.runsettings`).