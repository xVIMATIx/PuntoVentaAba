# Copilot instructions for PuntoVentaAba

## Build, run, migrations, test, lint

This repository has one .NET solution under `PuntoAba\`.

```powershell
cd PuntoAba
dotnet restore
dotnet build PuntoAba.sln
dotnet run --project PuntoAba\PuntoAba.csproj
```

Useful alternatives already used in the repo:

```powershell
cd PuntoAba
dotnet watch run --project PuntoAba\PuntoAba.csproj
dotnet ef database update --project PuntoAba\PuntoAba.csproj
dotnet ef migrations add <NombreMigracion> --project PuntoAba\PuntoAba.csproj
dotnet publish -c Release -o .\publish
```

Tests/lint status in current codebase:

- `dotnet test PuntoAba.sln` runs, but there are currently no test projects in the solution.
- No dedicated lint command/config is present (no `.editorconfig` at repo level, no analyzer/linter setup script).

When test projects are added, run one test with:

```powershell
dotnet test <TestProject.csproj> --filter "FullyQualifiedName~<TestNameFragment>"
```

## High-level architecture

- **Single-process app**: `PuntoAba\PuntoAba` is an ASP.NET Core 9 app that hosts both:
  - REST API controllers in `Controllers\`
  - Blazor UI pages in `Views\` (routable `.razor` pages)
- **Data access**: EF Core with `PuntoAbaDbContext` (`Data\PuntoAbaDbContext.cs`), defaulting to SQLite (`Data Source=PuntoAba.db`) via `appsettings*.json`.
- **Service layer**: Domain logic is mainly in `Services\` (`ProductoService`, `VentaService`, `AuthService`, `SincronizacionService`) and registered in `Program.cs`.
- **Mixed access pattern**:
  - Some controllers call services (e.g., productos/ventas/auth/sincronizacion).
  - Other controllers query `PuntoAbaDbContext` directly (e.g., clientes/dashboard/categorias).
  - Blazor pages usually inject services directly; `Views\Ventas\Nuevo.razor` also calls `api/clientes` through injected `HttpClient`.
- **Startup/runtime behavior** (`Program.cs`):
  - JWT auth + role authorization enabled.
  - Swagger exposed in Development at `/api`.
  - Migrations auto-applied in Development (`Database.Migrate()`).
  - Root endpoint `/` and health endpoint `/health` are mapped.
  - Balanza (scale) DI registration is currently commented out even though UI references `IBalanzaService`.

## Key codebase conventions

- **Spanish domain naming everywhere**: entities, DTOs, service methods, route names, and response messages are in Spanish (`Producto`, `Venta`, `Sincronizacion`, `Obtener*`, `Crear*`, etc.). Keep this naming style for new code.
- **Soft-delete convention**: entities like `Producto`, `Cliente`, and `Usuario` use `Activo` flag; deletes typically set `Activo = false` instead of physical deletion.
- **Audit/sync fields are first-class**:
  - `FechaCreacion` / `FechaActualizacion` use UTC updates.
  - Sync-aware entities use `UltimaSincronizacion`, plus `Venta.Sincronizado` and `Venta.OrigenVenta`.
  - Sync operations append `RegistroSincronizacion`.
- **Inventory integrity is transaction-based**:
  - `VentaService.CrearAsync` wraps sale creation in a DB transaction, validates stock/rules, decrements stock, and inserts `MovimientoInventario` rows.
  - Manual stock changes also create `MovimientoInventario` entries in `ProductoService.ActualizarStockAsync`.
- **Decimal precision semantics**:
  - Money fields use `decimal(18,2)`.
  - Quantity/stock fields use `decimal(18,3)`.
  - `EsPorPeso` controls quantity behavior across UI and backend (weighted products allow fractional quantities; unit products require integers in sale validation).
- **DTO/update pattern**:
  - Create DTOs (`Crear*Dto`) are full payloads.
  - Update DTOs (`Actualizar*Dto`) are nullable/optional patch-like inputs.
  - Mapping is mostly manual (`MapToDto`) rather than AutoMapper profiles.
- **API authorization pattern**:
  - Controllers commonly use `[Authorize]` at class level plus role-restricted actions (`Admin`, `Supervisor`).
  - Selected endpoints are intentionally anonymous (`/api/auth/login`, several `/api/clientes` reads).
