# Task: Test Project Scaffolding

## Task ID
002

## Feature
FRD-001 — MCP Server Hosting & Configuration

## Description
Create the xUnit test project for the PdfToMarkdown application with all required test framework packages and a project reference to the main application. This project will house all unit and integration tests for subsequent features.

## Dependencies
- Task 001 (Solution & Main Project Scaffolding) — the solution and main project must exist.

## Technical Requirements

### Project location
- `tests/PdfToMarkdown.Tests/PdfToMarkdown.Tests.csproj`

### Project file settings
- `<TargetFramework>net9.0</TargetFramework>`
- `<LangVersion>13</LangVersion>`
- `<Nullable>enable</Nullable>`
- `<ImplicitUsings>enable</ImplicitUsings>`
- `<IsPackable>false</IsPackable>`

### NuGet packages
- `Microsoft.NET.Test.Sdk` — test host (latest stable)
- `xunit` — test framework
- `xunit.runner.visualstudio` — test runner integration
- `FluentAssertions` — assertion library
- `coverlet.collector` — code coverage collection

### Project reference
- Reference to `../../src/PdfToMarkdown/PdfToMarkdown.csproj`

### Solution integration
- The test project must be added to `PdfToMarkdown.sln`.

### Folder structure
- `TestData/` directory inside the test project (for future PDF fixture files).

### Placeholder test
- A single placeholder test class with one passing test to verify the test infrastructure works (e.g., a simple assertion that `true` is `true`). This placeholder can be removed once real tests are added.

## Acceptance Criteria
- [ ] `tests/PdfToMarkdown.Tests/PdfToMarkdown.Tests.csproj` exists with all required settings and packages.
- [ ] The test project is included in `PdfToMarkdown.sln`.
- [ ] `dotnet build tests/PdfToMarkdown.Tests` compiles successfully with zero errors.
- [ ] `dotnet test tests/PdfToMarkdown.Tests` discovers and runs the placeholder test successfully.
- [ ] The `TestData/` directory exists inside the test project folder.

## Testing Requirements
- Verify `dotnet test` runs the placeholder test and reports a pass result.
- No coverage threshold required at this stage.
