# Repository Guidelines

## Project Structure & Modules

`CitasMedicas.slnx` currently contains the ASP.NET Core MVC project at `CitasMedicas.Web/`. Controllers, models, Razor views, and static assets live under that project; assets are in `wwwroot/`. The `src/` tree is a scaffold for the planned implementation: `AppCitasMedicas.Modules/` has `CatalogoMedico`, `AgendaMedica`, and `Reservas` modules, each with `Features/` and `Persistence/`; `AppCitasMedicas.Infrastructure/` is reserved for infrastructure, and `AppCitasMedicas.Tests/` is organized by module. These folders currently contain placeholders, not project files. Follow `CONVENTIONS.md.txt` when extending the architecture.

## Build, Test, and Run

Run commands from the repository root:

- `dotnet restore CitasMedicas.slnx` restores solution dependencies.
- `dotnet build CitasMedicas.slnx` builds the projects currently included in the solution.
- `dotnet run --project CitasMedicas.Web/CitasMedicas.Web.csproj` starts the MVC app locally.
- `dotnet test CitasMedicas.slnx` runs tests once test projects are added to the solution; no test project is currently included.

## Architecture & Coding Style

Keep the MVP simple and easy to change. Place business behavior in its owning module and group each use case in its own feature folder (for example, `Reservas/Features/BookAppointment/`). Keep commands for writes and queries for reads; locate validation beside the use case. Configure EF Core mappings with Fluent API in `Persistence/`. Avoid global `Commands`, `Handlers`, or `Validators` folders, cross-module internal access, and speculative base classes or generic repositories. Use standard C# conventions: four-space indentation, PascalCase for types and public members, camelCase for locals and parameters, and descriptive filenames matching their primary type. No formatter or linter is configured; match surrounding code.

## Testing

There is no configured test framework or coverage threshold yet. When adding tests, place them under `src/AppCitasMedicas.Tests/<Module>/` and name them for the behavior under test. Add the test project to `CitasMedicas.slnx` so `dotnet test CitasMedicas.slnx` discovers it.

## Commits & Pull Requests

The available history contains only the initial commit, so no established commit-message pattern can be inferred. Use a short, imperative summary (for example, `Add appointment booking slice`). Pull requests should explain the user-facing change, note relevant design decisions, link any issue, and include screenshots for UI changes. Mention build/test results and any required configuration.

## Configuration

Keep secrets and machine-specific values out of committed `appsettings*.json`; use .NET user secrets or environment variables for local credentials. Preserve the MVP priorities in `CONVENTIONS.md.txt`: clarity, small changes, and only abstractions justified by current needs.

## Git Workflow

- El repositorio de GitHub de este Proyecto es: `https://github.com/boasman/App-Citas-Medicas`
- Nunca trabaje directamente sobre 'main'
- Antes de implementar cualquier feature, bugfix o issue, verifica la rama actual, 
- Si la rama actual es `main`, crea una rama antes de realizer cualquier cambio
- Usa nombres descriptivos 
	- feature/<descripcion>
	- fix/<descripcion>
	- refactor/<descripcion>
- Realiza todo los commits en la Nueva rama.
- nunca haga push directo a `main`