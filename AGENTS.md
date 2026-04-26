# Repository Guidelines

## Project Structure & Module Organization

DocTree is a .NET 10 Windows Forms application. The solution entry point is `DocTree.slnx`, and the main project is `DocTree/DocTree.csproj`.

- `DocTree/Program.cs` starts the app and bootstraps shared context.
- `DocTree/Forms/` contains WinForms UI code and designer-generated partial classes.
- `DocTree/App/`, `DocTree/Models/`, and `DocTree/Services/` hold application state, data models, and feature services.
- `DocTree/Resources/default-settings.jsonc` is embedded as the default configuration resource.
- `docs/` contains user and maintainer documentation, including build, configuration, architecture, and troubleshooting notes.

No dedicated test project is currently present.

## Build, Test, and Development Commands

Run commands from the repository root on Windows with the .NET 10 SDK installed.

```powershell
dotnet build DocTree.slnx
```

Builds the solution in Debug configuration.

```powershell
dotnet run --project DocTree/DocTree.csproj
```

Runs the WinForms app locally for development.

```powershell
dotnet build DocTree.slnx -c Release
```

Creates a Release build under `DocTree/bin/Release/net10.0-windows/`.

```powershell
dotnet publish DocTree/DocTree.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Publishes a self-contained Windows x64 single-file build.

## Coding Style & Naming Conventions

Use C# with nullable reference types enabled and implicit usings. Follow the existing brace style: file-scoped namespaces are not used, braces appear on their own lines, and indentation is four spaces. Use PascalCase for public types, methods, properties, and enum values; use camelCase for locals and parameters. Keep service classes grouped by capability under `DocTree/Services/<Area>/`.

Do not hand-edit `*.Designer.cs` unless the change is specifically designer-related. Keep Japanese UI strings consistent with the existing interface text.

## Testing Guidelines

There is no test suite yet. For new test coverage, add a separate test project such as `DocTree.Tests/` and include it in the solution. Prefer focused unit tests for services in `DocTree/Services/`, especially filesystem, encoding, settings, and read-only behavior. Name test files after the class under test, for example `SettingsLoaderTests.cs`.

Until automated tests exist, verify changes with `dotnet build DocTree.slnx` and a manual run of affected UI flows.

## Commit & Pull Request Guidelines

Recent commits use short, descriptive subjects; scoped prefixes such as `docs:` are acceptable. Keep commit messages imperative or descriptive and focused on one change, for example `docs: update configuration guide` or `Add external editor launcher`.

Pull requests should include a summary, affected areas, verification steps, and screenshots or screen recordings for visible UI changes. Link related issues when applicable and call out configuration, filesystem, or read-only behavior changes explicitly.

## Security & Configuration Tips

User configuration and state live under `%AppData%\DocTree\` unless running in portable mode with `settings.jsonc` beside the executable. Avoid committing local settings, build outputs, or machine-specific paths.
