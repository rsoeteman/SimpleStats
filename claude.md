# SimpleStats

An Umbraco backoffice package that provides site statistics and analytics directly in the Umbraco back-office dashboard.

## Solution Structure

```
SimpleStats/
├── SimpleStats.sln
└── src/
    ├── SimpleStats/           # Main Umbraco class-library package (C#)
    └── SimpleStats.Client/    # Backoffice extension (C# host + Vite/TypeScript frontend)
```

### SimpleStats (main project)

- Class library targeting `net9.0`
- Umbraco package that registers services, API controllers, and manifests
- Depends on `Umbraco.Cms` packages
- Supports Umbraco 17

### SimpleStats.Client

- Umbraco backoffice extension following the [Backoffice Extensions](https://docs.umbraco.com/umbraco-cms/extend-your-project/backoffice-extensions/) guide
- Vite + TypeScript frontend built with `@umbraco-cms/backoffice` web components
- The C# project embeds the compiled frontend assets and registers a `StaticFilesEndpointRoute`
- The `dist/` output is served via `App_Plugins/SimpleStats/`

## Supported Umbraco versions

| Umbraco | .NET  | Status  |
|---------|-------|---------|
| 17.x    | 10    | ✅ Supported |
| 18.x    | 10    | ❌ Not yet supported ([Umbraco 18 breaking change](https://github.com/umbraco/Announcements/issues/32)) |
| 19.x    | TBD   | ❌ Not yet supported |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/) (for the Client frontend build)

## Getting Started

```bash
# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Build the frontend assets (from src/SimpleStats.Client)
cd src/SimpleStats.Client
npm install
npm run build
```

## Development Guidelines
- Every Change must be added in a separate branch. NEVER commit directly to main, always via a Pull Request. always prefix branchename with `claude/`
- Follow [Umbraco  documentaion](https://docs.umbraco.com/umbraco-cms/17.latest)
- Keep C# code compatible with **Umbraco 17**
- Database is using EFCORE 10/ IMPORTANT Read Umbraco instructions https://docs.umbraco.com/umbraco-cms/extend-your-project/tutorials/getting-started-with-entity-framework-core
- Frontend components should use the `@umbraco-cms/backoffice` Lit/Web Component APIs
- All public APIs must be documented with XML doc comments
- Use `IOptions<SimpleStatsSettings>` for any configurable values
- Register services and manifests via an `IUmbracoBuilder` extension method in `UmbracoBuilderExtensions.cs`

## Project conventions

- Solution and projects live under `src/`
- Tests (future) live under `tests/`
- NuGet packaging metadata is maintained in `Directory.Build.props`
