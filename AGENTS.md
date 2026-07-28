# MijnThuis - AI Agent Development Guide

This document outlines the architectural patterns, coding conventions, and best practices for AI agents working on the MijnThuis home IoT solution.

## Project Overview

MijnThuis is a home IoT solution managing power consumption, solar energy, electric car charging, temperature sensors, electric switches, smart lights, and sauna systems. The solution follows Clean Architecture principles with clear separation of concerns.

## Solution Structure (11 Projects)

| Project | Framework | Description |
|---------|-----------|-------------|
| `MijnThuis.Contracts` | net10.0 | DTOs, Commands, Queries, Responses |
| `MijnThuis.Application` | net10.0 | MediatR handlers, business logic |
| `MijnThuis.DataAccess` | net10.0 | EF Core, repositories, migrations |
| `MijnThuis.Integrations` | net10.0 | External service clients |
| `MijnThuis.Api` | net9.0 | Minimal API endpoints |
| `MijnThuis.Dashboard.Web` | net10.0 | Blazor Server dashboard (MudBlazor + ApexCharts) |
| `MijnThuis.Dashboard.App` | net10.0 | MAUI mobile application |
| `MijnThuis.Worker` | net10.0 | Background hosted services |
| `MijnThuis.ModbusProxy.Api` | net10.0 | Modbus TCP/RTU proxy |
| `MijnThuis.Tests` | net10.0 | BDD tests (Reqnroll + xUnit) |
| `MijnThuis.MyConsole` | net10.0 | Console utility |

## Architecture

The solution is organized into three main layers:

### 1. Application Layer (`MijnThuis.Application`)
- Contains business logic and use case orchestration
- Implements CQRS pattern using MediatR
- Commands and Queries are separated by feature folders (Car, Power, Solar, Heating, etc.)
- Uses Mapster for object mapping

### 2. Domain Layer (`MijnThuis.Contracts`)
- Contains contracts (DTOs, Commands, Queries, Responses)
- Organized by feature areas matching the Application layer
- All commands implement `IRequest<TResponse>` from MediatR
- All queries implement `IRequest<TResponse>` from MediatR

### 3. Infrastructure Layer
- **`MijnThuis.DataAccess`**: EF Core repositories and entities (SQL Server)
- **`MijnThuis.Integrations`**: External service integrations (Tesla/Tessie, SolarEdge, Shelly, LIFX, ENTSO-E, Samsung, etc.)

### 4. Presentation / Entry Points
- **`MijnThuis.Api`**: Minimal API endpoints grouped by feature (`MapCarEndpoints()`, `MapPowerEndpoints()`, etc.)
- **`MijnThuis.Dashboard.Web`**: Blazor Server with MudBlazor UI framework and ApexCharts, includes Copilot MCP tools
- **`MijnThuis.Dashboard.App`**: MAUI mobile application
- **`MijnThuis.Worker`**: Background hosted services with scoped service pattern
- **`MijnThuis.ModbusProxy.Api`**: Modbus TCP/RTU communication proxy for heating systems

## Coding Conventions

### Naming Patterns

#### Commands
```csharp
// Contract
public class CarFartCommand : IRequest<CarCommandResponse>
{
}

// Handler
public class CarFartCommandHandler : IRequestHandler<CarFartCommand, CarCommandResponse>
{
    public async Task<CarCommandResponse> Handle(CarFartCommand request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

#### Queries
```csharp
// Contract
public class GetCarOverviewQuery : IRequest<GetCarOverviewResponse>
{
}

// Handler
public class GetCarOverviewQueryHandler : IRequestHandler<GetCarOverviewQuery, GetCarOverviewResponse>
{
    public async Task<GetCarOverviewResponse> Handle(GetCarOverviewQuery request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### File Organization

```
MijnThuis.Application/
├── [Feature]/
│   ├── Commands/
│   │   └── [Action]CommandHandler.cs
│   └── Queries/
│       └── Get[Entity]QueryHandler.cs
└── _di/
    └── ServiceCollectionExtensions.cs

MijnThuis.Contracts/
├── [Feature]/
│   ├── [Action]Command.cs
│   ├── [Action]Response.cs
│   ├── Get[Entity]Query.cs
│   └── Get[Entity]Response.cs

MijnThuis.DataAccess/
├── Entities/
│   └── [Entity].cs
├── Repositories/
│   └── [Entity]Repository.cs
├── Migrations/
└── _di/
    └── ServiceCollectionExtensions.cs

MijnThuis.Integrations/
├── [Feature]/
│   ├── [Entity].cs (DTOs)
│   └── [Feature]Service.cs
└── _di/
    └── ServiceCollectionExtensions.cs
```

### Dependency Injection

All layers register their dependencies via extension methods:

```csharp
// MijnThuis.Application
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    services.AddMediatR(c => c.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
    services.AddIntegrations();
    services.AddDataAccess();
    return services;
}

// MijnThuis.DataAccess
public static IServiceCollection AddDataAccess(this IServiceCollection services)
{
    services.AddDbContext<MijnThuisDbContext>();
    services.AddScoped<IFlagRepository, FlagRepository>();
    // Register other repositories
    return services;
}
```

### Repository Pattern

Repositories follow an interface-based approach:

```csharp
public interface IFlagRepository
{
    Task<Flag?> GetFlag(string name);
    Task SetFlag<TFlag>(string name, TFlag flag) where TFlag : IFlag;
}

public class FlagRepository : IFlagRepository
{
    private readonly MijnThuisDbContext _dbContext;

    public FlagRepository(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Flag?> GetFlag(string name)
    {
        return await _dbContext.Flags.AsNoTracking()
            .SingleOrDefaultAsync(f => f.Name == name);
    }
}
```

### Integration Services

External service integrations follow a consistent pattern:

```csharp
public interface ICarService
{
    Task<CarOverview> GetOverview();
    Task<bool> Lock();
    Task<bool> Unlock();
}

public class CarService : BaseService, ICarService
{
    private readonly string _vinNumber;

    public CarService(IConfiguration configuration) : base(configuration)
    {
        _vinNumber = configuration.GetValue<string>("CAR_VIN_NUMBER");
    }

    public async Task<bool> Lock()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/lock");
        return result.Result;
    }
}
```

### Entity Serialization

Flags and configuration entities implement `IFlag` interface for JSON serialization:

```csharp
public interface IFlag
{
    string Serialize();
}

public class ManualCarChargeFlag : IFlag
{
    public static string Name => "ManualCarCharge";
    public static ManualCarChargeFlag Default => new ManualCarChargeFlag();

    public bool ShouldCharge { get; set; }
    public int ChargeAmps { get; set; }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static ManualCarChargeFlag Deserialize(string json)
    {
        return JsonSerializer.Deserialize<ManualCarChargeFlag>(json) ?? Default;
    }
}
```

### Database Entity Conventions

Entities follow these patterns:
- All entities have `Guid Id` as primary key and `long SysId` as clustered index
- Table names use `UPPERCASE_SNAKE_CASE` (e.g., `SOLAR_ENERGY_HISTORY`, `DAY_AHEAD_ENERGY_PRICES`)
- Decimal properties for energy metrics use `HasPrecision(9, 3)`
- Heavy indexing on date, timestamp, and lookup fields
- Configuration stored as JSON-serialized `IFlag` objects in the `FLAGS` table

### Minimal API Endpoints (MijnThuis.Api)

```csharp
// Endpoints are grouped by feature using extension methods
app.MapCarEndpoints();
app.MapPowerEndpoints();
app.MapSolarEndpoints();

// Each endpoint group maps to MediatR commands/queries
public static void MapCarEndpoints(this WebApplication app)
{
    var group = app.MapGroup("/api/car");
    group.MapGet("/overview", async (IMediator mediator) =>
        await mediator.Send(new GetCarOverviewQuery()));
}
```

### Background Worker Pattern (MijnThuis.Worker)

```csharp
// Workers are registered as IHostedService
public class CarStatusWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            // Use scoped services per iteration
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
```

### Blazor Dashboard Pattern (MijnThuis.Dashboard.Web)

- Uses MudBlazor for Material Design UI components
- Uses ApexCharts for data visualization
- Pages organized by feature: `Index.razor`, `Car.razor`, `Power.razor`, `Solar.razor`, `Heating.razor`, `Chart.razor`
- Custom `SuperSecretAccessKeyMiddleware` for authentication
- Includes Copilot MCP tools (`MijnThuisPowerTools`, `MijnThuisSolarTools`, `MijnThuisCarTools`, etc.)

## Best Practices for AI Agents

### 1. Feature Organization
- Group related functionality by domain feature (Car, Power, Solar, Heating, etc.)
- Keep Commands and Queries separated
- Place handlers in the Application layer, contracts in Contracts layer

### 2. Handler Implementation
- Inject dependencies via constructor
- Use `IServiceScopeFactory` when needing scoped services (like DbContext) in singleton handlers
- Always include `CancellationToken` parameter
- Use async/await consistently

### 3. Data Access
- Use repositories for database operations
- Apply `.AsNoTracking()` for read-only queries
- Use bulk update operations (e.g., `ExecuteUpdateAsync`) for updates

### 4. External Services
- Inherit from `BaseService` for HTTP client initialization
- Use `System.Net.Http.Json` extensions for JSON deserialization
- Wrap external calls in try-catch when appropriate
- Use `using` statements for HttpClient disposal

### 5. Configuration
- Read configuration via `IConfiguration`
- Use strongly-typed configuration values
- Store sensitive data (API keys, tokens) in environment variables

### 6. Responses and DTOs
- Use Mapster for mapping between entities and DTOs
- Keep response objects focused and specific to use case
- Include descriptive property names

### 7. Testing (BDD with Reqnroll)
- Place tests in `MijnThuis.Tests` project
- Use Reqnroll (SpecFlow successor) with xUnit for BDD-style testing
- Write `.feature` files in Gherkin syntax in `Features/` folder
- Implement step definitions in `StepDefinitions/` folder
- Use FakeItEasy for mocking external dependencies
- Use Shouldly for assertions (e.g., `result.ShouldBe(expected)`)

### 8. Naming Conventions Summary

| Aspect | Convention | Example |
|--------|-----------|---------|
| Queries | `Get{What}Query` | `GetPowerOverviewQuery` |
| Commands | `{Action}{What}Command` | `LockCarCommand`, `ChargeBatteryCommand` |
| Handlers | `{Query/Command}Handler` | `GetPowerOverviewQueryHandler` |
| Services | `I{Feature}Service` / `{Feature}Service` | `ICarService` / `CarService` |
| Repositories | `I{Entity}Repository` / `{Entity}Repository` | `IFlagRepository` / `FlagRepository` |
| Flags | `{Purpose}Flag` | `ManualCarChargeFlag` |
| Tables | `UPPERCASE_SNAKE_CASE` | `SOLAR_ENERGY_HISTORY` |
| Feature folders | PascalCase domain name | `Car`, `Power`, `Solar` |

## Technology Stack

- **Runtime**: .NET 10.0 (preview), .NET 9.0 (Api project)
- **ORM**: Entity Framework Core 10.0.0
- **Database**: SQL Server
- **CQRS/Mediator**: MediatR 12.5.0 / 13.1.0
- **Mapping**: Mapster 7.4.0
- **Frontend**: Blazor Server (MudBlazor + ApexCharts), MAUI (Mobile)
- **API**: Minimal APIs with Swashbuckle/OpenAPI
- **Caching**: Microsoft.Extensions.Caching.Abstractions 10.0.0
- **Testing**: xUnit 2.9.3, Reqnroll.xUnit 3.2.1, FakeItEasy 8.3.0, Shouldly 4.3.0
- **Smart Home**: LIFX (LifxCloud 1.1.19, LifxNet 2.2.0), Modbus, Shelly
- **Auth/Identity**: Duende.IdentityModel 7.1.0
- **Serialization**: System.Text.Json (primary), Newtonsoft.Json 13.0.4 (legacy)

## Feature Areas

Current feature areas in the system:
- **Car**: Tesla/Tessie vehicle control and monitoring (lock, unlock, charging, preheating)
- **Power**: Energy consumption tracking, Shelly smart switches, day-ahead prices
- **Solar**: SolarEdge solar panel monitoring, battery management, forecasting
- **Forecast**: Solar and energy forecasting (Forecast.Solar API)
- **Heating**: Gas usage and heating control via Modbus
- **Sauna**: Sauna control
- **Lamps**: LIFX smart lighting control
- **SmartLock**: Door lock integration
- **Samsung**: Samsung The Frame TV art display integration

## External Service Integrations

| Service | API/Protocol | Purpose |
|---------|-------------|---------|
| SolarEdge | REST API | Solar production & battery data |
| Tesla/Tessie | REST API | Electric car control |
| Shelly | REST API | Smart switches & relays |
| Modbus | TCP/RTU | Heating system control |
| LIFX | Cloud API | Smart lights |
| ENTSO-E | REST API | Day-ahead energy prices |
| Forecast.Solar | REST API | Solar production forecast |
| Samsung SmartThings | REST API | The Frame art display |
| Wake-on-LAN | Network | PC remote power-on |
| Smart Lock | REST API | Door lock control |
| Sauna | REST API | Sauna control |

## Configuration & Environment Variables

All secrets and configuration are stored as environment variables:
- `CONNECTION_STRING` - SQL Server connection string
- `SOLAR_API_AUTH_TOKEN`, `SOLAR_SITE_ID` - SolarEdge API
- `CAR_VIN_NUMBER`, `PINCODE` - Tesla/Tessie
- `POWER_API_BASE_ADDRESS` - Power meter API
- `PORT`, `CERTIFICATE_FILENAME`, `CERTIFICATE_PASSWORD` - HTTPS configuration
- `SUPER_SECRET_ACCESS_KEY` - Copilot MCP access key
- Various service-specific credentials (LIFX, Samsung, etc.)

## Build & Deployment

- **CI/CD**: Azure DevOps YAML Pipelines
- **Container**: Docker images pushed to Docker Hub (`djohnnie/mijnthuis-*`)
- **Build Environment**: ubuntu-latest with .NET 10.0 SDK (preview)
- **Pipeline triggers**: Path-based (changes in specific project folders)
- Pipeline files: `pipelines-worker.yml`, `pipelines-web.yml`, `pipelines-proxy.yml`, `pipelines-app-android.yml`

## When Adding New Features

1. Create feature folder in `MijnThuis.Contracts`
2. Define Commands/Queries and Responses
3. Create corresponding folder in `MijnThuis.Application`
4. Implement CommandHandlers/QueryHandlers
5. If external API needed, add to `MijnThuis.Integrations`
6. If database storage needed, add entities and repositories to `MijnThuis.DataAccess`
7. Register services in appropriate `ServiceCollectionExtensions.cs`
8. Add tests in `MijnThuis.Tests`

## Important Notes

- Solution uses `ImplicitUsings` - common namespaces are automatically imported
- `Nullable` is enabled project-wide - handle null cases appropriately
- Follow existing naming conventions exactly
- Keep minimal comments - code should be self-documenting
- Use feature folders, not technical folders (no separate "Controllers", "Services" folders)
