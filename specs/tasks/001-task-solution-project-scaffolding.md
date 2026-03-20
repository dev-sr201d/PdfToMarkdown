# Task: Solution & Main Project Scaffolding

## Task ID
001

## Feature
FRD-001 — MCP Server Hosting & Configuration

## Description
Create the .NET solution file and the main `PdfToMarkdown` console application project with all required project settings, NuGet package references, and folder structure as defined in AGENTS.md. This is the foundational scaffolding that all subsequent tasks depend on.

## Dependencies
- None (first task)

## Technical Requirements

### Solution structure
- A solution file (`PdfToMarkdown.sln`) at the repository root.
- A console application project at `src/PdfToMarkdown/PdfToMarkdown.csproj`.

### Project file settings
The `.csproj` must include:
- `<TargetFramework>net9.0</TargetFramework>`
- `<LangVersion>13</LangVersion>`
- `<Nullable>enable</Nullable>`
- `<ImplicitUsings>enable</ImplicitUsings>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- `<EnableNETAnalyzers>true</EnableNETAnalyzers>`
- `<AnalysisLevel>latest-recommended</AnalysisLevel>`
- Output type: `Exe`

### NuGet packages
- `ModelContextProtocol` — MCP server SDK (latest stable)
- `Microsoft.Extensions.Hosting` — Generic host for console apps (latest stable for .NET 9)

### Folder structure inside `src/PdfToMarkdown/`
- `Tools/` — for MCP tool classes (empty for now; will be populated by FRD-002)
- `Services/` — for business logic interfaces and implementations (empty for now)

### Placeholder Program.cs
- A minimal `Program.cs` that compiles and runs (e.g., a simple `Console.Error.WriteLine("Server starting…")` or an empty host). The full MCP host bootstrap is covered in task 003.

## Acceptance Criteria
- [ ] `PdfToMarkdown.sln` exists at the repository root and includes the `src/PdfToMarkdown` project.
- [ ] `dotnet build src/PdfToMarkdown` completes successfully with zero errors and zero warnings.
- [ ] The `.csproj` contains all required settings listed above.
- [ ] Both `ModelContextProtocol` and `Microsoft.Extensions.Hosting` packages are referenced.
- [ ] The `Tools/` and `Services/` directories exist inside the project folder.
- [ ] `dotnet run --project src/PdfToMarkdown` executes without crashing.

## Testing Requirements
- Build verification only at this stage — confirm the project compiles and runs.
- No unit tests required for scaffolding; the test project is set up in task 002.
