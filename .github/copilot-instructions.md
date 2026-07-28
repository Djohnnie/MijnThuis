# MijnThuis - GitHub Copilot Instructions

## Project Overview

MijnThuis is a .NET 10.0 home IoT solution using Clean Architecture with CQRS (MediatR). It manages solar energy (SolarEdge), electric car charging (Tesla/Tessie), power consumption (Shelly), heating (Modbus), smart lights (LIFX), smart locks, and sauna systems.

## Architecture & Code Style

### CQRS with MediatR
- **Contracts** (`MijnThuis.Contracts`): Define Commands (`{Action}{Feature}Command : IRequest<TResponse>`) and Queries (`Get{What}Query : IRequest<TResponse>`) in feature folders.
- **Handlers** (`MijnThuis.Application`): Implement `IRequestHandler<TRequest, TResponse>` in `{Feature}/Commands/` or `{Feature}/Queries/` folders.
- Always include `CancellationToken` parameter, use async/await.

### Naming Conventions
- Queries: `Get{What}Query` → `GetPowerOverviewQuery`
- Commands: `{Action}{What}Command` → `LockCarCommand`, `ChargeBatteryCommand`
- Handlers: `{Query/Command}Handler` → `GetPowerOverviewQueryHandler`
- Services: `I{Feature}Service` / `{Feature}Service` → `ICarService` / `CarService`
- Repositories: `I{Entity}Repository` / `{Entity}Repository`
- Flags: `{Purpose}Flag` → `ManualCarChargeFlag`
- Database tables: `UPPERCASE_SNAKE_CASE` → `SOLAR_ENERGY_HISTORY`

### Dependency Injection
- Register via extension methods in `_di/ServiceCollectionExtensions.cs` per project.
- `AddApplication()` chains `AddIntegrations()` and `AddDataAccess()`.

### Data Access (Entity Framework Core 10.0)
- SQL Server database via `MijnThuisDbContext`.
- Entities have `Guid Id` (PK) + `long SysId` (clustered index).
- Decimal energy metrics use `HasPrecision(9, 3)`.
- Use `.AsNoTracking()` for read-only queries.
- Repository pattern with interface abstractions.

### Integration Services
- Inherit from `BaseService` for HTTP client setup.
- Configuration from environment variables via `IConfiguration`.
- Use `System.Net.Http.Json` extensions for JSON.
- Use `using` statements for `HttpClient` disposal.

### Worker Pattern
- Background services extend `BackgroundService`.
- Use `IServiceScopeFactory` to create scoped services per iteration.
- Pattern: create scope → resolve mediator/services → execute → delay → repeat.

### Blazor Dashboard
- Blazor Server with MudBlazor (Material Design) and ApexCharts.
- Pages by feature: `Car.razor`, `Power.razor`, `Solar.razor`, `Heating.razor`, `Chart.razor`.

### Testing (BDD)
- Reqnroll (SpecFlow successor) + xUnit for BDD tests.
- `.feature` files in Gherkin syntax → `StepDefinitions/` for implementations.
- FakeItEasy for mocking, Shouldly for assertions.

### General Rules
- `ImplicitUsings` enabled - common namespaces auto-imported.
- `Nullable` enabled project-wide - always handle null cases.
- Use Mapster for object mapping between entities and DTOs.
- Feature folders over technical folders (no separate "Controllers", "Services" folders).
- Code should be self-documenting - minimal comments.
- Configuration secrets always via environment variables, never hardcoded.

### Key NuGet Packages
- MediatR 12.5.0 / 13.1.0, Mapster 7.4.0, EF Core 10.0.0
- MudBlazor, ApexCharts, LifxCloud/LifxNet
- xUnit 2.9.3, Reqnroll.xUnit 3.2.1, FakeItEasy 8.3.0, Shouldly 4.3.0
