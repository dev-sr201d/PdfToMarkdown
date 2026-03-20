# AGENTS.md — PdfToMarkdown

This document defines the coding standards, architectural guidelines, and best practices for the **PdfToMarkdown** project — a C# console application that runs as a local MCP (Model Context Protocol) stdio server.

All agents and contributors must follow these guidelines. When domain-specific guidance conflicts with general guidance, prefer the domain-specific rule.

---

## 1. Project Overview

- **Runtime:** .NET 9 (latest LTS at time of writing)
- **Language:** C# 13
- **Application type:** Console app acting as a local MCP stdio server
- **Primary packages:** `ModelContextProtocol`, `Microsoft.Extensions.Hosting`
- **Transport:** stdio (stdin/stdout) — no HTTP, no ASP.NET Core

---

## 2. C# Language Standards (C# 13 / .NET 9)

### 2.1 Target Framework & Language Version

- Target `net9.0` in every `.csproj`.
- Set `<LangVersion>13</LangVersion>` (or `latest`) explicitly.
- Enable nullable reference types: `<Nullable>enable</Nullable>`.
- Enable implicit usings: `<ImplicitUsings>enable</ImplicitUsings>`.

### 2.2 Modern Language Features — Use Them

Leverage C# 13 and recent features actively:

| Feature | Guidance |
|---|---|
| **File-scoped namespaces** | Always use `namespace Foo;` — never block-scoped. |
| **Primary constructors** | Use on `class` and `struct` where it reduces boilerplate. Use camelCase for parameters on classes/structs, PascalCase on records. |
| **Collection expressions** | Prefer `string[] items = ["a", "b"];` over `new[] { "a", "b" }`. |
| **Raw string literals** | Use `"""` for multi-line strings or strings containing special characters instead of escape sequences or `@""`. |
| **`required` properties** | Prefer `required` properties over constructor parameters to force initialization. |
| **Pattern matching** | Use `is`, `switch` expressions, and property/list patterns over cascading `if`/`else`. |
| **`params` collections** | Use `params ReadOnlySpan<T>` or `params IEnumerable<T>` where appropriate, not only arrays. |
| **`using` declarations** | Prefer `using var stream = ...;` (no braces) over `using (var stream = ...) { }`. |
| **Target-typed `new`** | Use `ExampleClass instance = new();` when the type is clear from context. |
| **`var`** | Use `var` only when the type is obvious from the right-hand side. Use explicit types otherwise. |
| **Field keyword (preview)** | Do NOT use `field` keyword yet — it is a preview feature in C# 13. |

### 2.3 Naming Conventions

| Symbol | Convention | Example |
|---|---|---|
| Namespaces | PascalCase, match folder structure | `PdfToMarkdown.Tools` |
| Classes, structs, records | PascalCase | `PdfConverter` |
| Interfaces | `I` + PascalCase | `IPdfParser` |
| Public methods & properties | PascalCase | `ConvertToMarkdown()` |
| Private fields | `_camelCase` with underscore prefix | `_pdfStream` |
| Local variables, parameters | camelCase | `chapterIndex` |
| Constants | PascalCase | `MaxHeadingLevel` |
| Generic type parameters | `T` or `T` + PascalCase | `TResult` |

### 2.4 Code Layout

- **One file per type.** File name matches type name.
- **Four-space indentation.** No tabs.
- **Allman brace style.** Opening brace on its own line.
- **One statement per line.** One declaration per line.
- **Blank line** between method and property definitions.
- **`using` directives outside the namespace**, at the top of the file.
- **Sort `using` directives**: `System.*` first, then alphabetical.

### 2.5 Async / Await

- Use `async`/`await` for all I/O-bound operations (file reads, PDF parsing).
- Suffix async methods with `Async` (e.g., `ConvertAsync`).
- Accept `CancellationToken` as the last parameter on all async public methods.
- Never use `.Result` or `.Wait()` — always `await`.
- Use `ConfigureAwait(false)` in library-level code (not in the hosting entry point).

### 2.6 Error Handling

- Catch **specific** exception types — never bare `catch (Exception)` without an exception filter.
- Use `McpException` (or derived types) for tool-level errors that should surface to the MCP client with a meaningful message.
- Use `McpProtocolException` for protocol-level errors (invalid parameters, etc.).
- Let `OperationCanceledException` propagate naturally when a `CancellationToken` is triggered.
- Provide clear, user-facing error messages — e.g., `"File not found: {filePath}"`.

### 2.7 Dependency Injection

- Register services in the DI container; do not use `static` service locators or singletons.
- Use constructor injection (or primary constructor injection).
- Scope lifetimes appropriately (`Singleton`, `Scoped`, `Transient`).

### 2.8 XML Documentation

- Document all `public` and `protected` members with XML doc comments (`///`).
- Use `<summary>`, `<param>`, `<returns>`, and `<exception>` tags.
- Use `[Description("...")]` attributes on MCP tool methods and parameters (required by the SDK for schema generation).

---

## 3. MCP Server Standards (stdio Transport)

### 3.1 Hosting Pattern

Use `Microsoft.Extensions.Hosting` with the MCP SDK's stdio transport. The entry point must follow this pattern:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// All console logging must go to stderr — stdout is reserved for MCP protocol messages
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

**Critical rules:**
- **Never write to `Console.Out` / `stdout`** — it is exclusively for MCP JSON-RPC messages. All diagnostic output goes to `stderr`.
- Configure logging to send **all** log levels to `stderr` via `LogToStandardErrorThreshold = LogLevel.Trace`.
- Use `WithToolsFromAssembly()` for automatic discovery, or `WithTools<T>()` for explicit registration.

### 3.2 Tool Definition

Tools are the primary extensibility mechanism. Follow the attribute-based pattern:

```csharp
[McpServerToolType]
public static class PdfConvertTool
{
    [McpServerTool, Description("Converts a PDF file to Markdown and writes the output alongside the source file.")]
    public static async Task<string> ConvertPdfToMarkdown(
        [Description("Absolute path to the PDF file")] string pdfPath,
        [Description("If true, splits output into separate files per chapter")] bool chunkByChapter = false,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

**Tool rules:**
- Mark tool container classes with `[McpServerToolType]`.
- Mark tool methods with `[McpServerTool]`.
- Add `[Description("...")]` to **every** tool method and **every** parameter — these become the JSON Schema descriptions that the LLM reads.
- Accept `CancellationToken` as a parameter for cancellable operations.
- Tools may accept `McpServer`, `IProgress<ProgressNotificationValue>`, or DI-registered services as parameters — these are resolved automatically.
- Return `string` for simple text responses (auto-wrapped in `TextContentBlock`).
- Return `IEnumerable<ContentBlock>` for mixed/rich responses.
- Keep tool logic thin — delegate to service classes registered in DI.

### 3.3 Error Handling in Tools

The MCP SDK handles exceptions automatically:

| Exception Type | SDK Behavior |
|---|---|
| `McpException` (non-protocol) | Message is included in the error result sent to the client |
| `McpProtocolException` | Re-thrown as JSON-RPC error response |
| `OperationCanceledException` (token triggered) | Re-thrown |
| Any other exception | Generic error message returned (details NOT leaked) |

**Preferred approach:** Validate inputs at the top of the tool method. Throw `McpException` with a descriptive message for expected failures (file not found, unsupported PDF, etc.). Let unexpected exceptions fall through to the generic handler.

```csharp
if (!File.Exists(pdfPath))
{
    throw new McpException($"PDF file not found: {pdfPath}");
}
```

### 3.4 No HTTP / No ASP.NET Core

This project uses **stdio transport only**. Do not:
- Reference `ModelContextProtocol.AspNetCore`.
- Use `WithHttpTransport()` or `MapMcp()`.
- Bind to any network port.

### 3.5 VS Code Integration

The MCP server is configured in VS Code via `.vscode/mcp.json` or the user's settings. The project should include a working configuration example:

```jsonc
// .vscode/mcp.json
{
  "servers": {
    "pdf-to-markdown": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "src/PdfToMarkdown"]
    }
  }
}
```

---

## 4. Project Structure

```
PdfToMarkdown/
├── AGENTS.md                    # This file
├── specs/
│   └── prd.md                   # Product requirements
├── src/
│   └── PdfToMarkdown/
│       ├── PdfToMarkdown.csproj
│       ├── Program.cs           # Host bootstrap (stdio MCP server)
│       ├── Tools/               # MCP tool definitions (thin wrappers)
│       │   └── PdfConvertTool.cs
│       └── Services/            # Business logic (PDF parsing, Markdown generation)
│           ├── IPdfParser.cs
│           ├── PdfParser.cs
│           ├── IMarkdownWriter.cs
│           └── MarkdownWriter.cs
└── tests/
    └── PdfToMarkdown.Tests/
        ├── PdfToMarkdown.Tests.csproj
        └── ...
```

**Conventions:**
- `Tools/` — Contains only MCP tool classes (`[McpServerToolType]`). These are thin wrappers that delegate to services.
- `Services/` — Contains business logic. Define interfaces and implementations. Register in DI.
- `Models/` — If needed, data transfer objects and domain models.
- One class/interface per file. File name = type name.

---

## 5. Testing Standards

- Use **xUnit** as the test framework.
- Use **FluentAssertions** for readable assertions.
- Name test methods: `MethodName_Condition_ExpectedResult` (e.g., `ConvertAsync_InvalidPath_ThrowsMcpException`).
- Test business logic in `Services/` independently from MCP tool wrappers.
- Use actual PDF fixtures in a `TestData/` folder for integration tests.
- Aim for high coverage on the Markdown conversion logic (headings, lists, tables, emphasis).

---

## 6. Output & File-Writing Rules

Per the PRD, the tool writes output files to disk — it does **not** return converted content to the caller.

| Mode | Output | Example |
|---|---|---|
| **Standard** (no chunking) | Single `.md` file alongside the source PDF | `report.pdf` → `report.md` |
| **Chunked** (by chapter) | One `.md` file per chapter with `_N` suffix | `report.pdf` → `report_1.md`, `report_2.md`, … |

- Use the same directory as the source PDF.
- Use the same base filename.
- Return a confirmation message (file paths written) as the tool's string response.
- Overwrite existing files without prompting (the tool is non-interactive).

---

## 7. Logging

- Use `ILogger<T>` from `Microsoft.Extensions.Logging` — never `Console.WriteLine`.
- Log levels:
  - `Information` — major operations (starting conversion, files written).
  - `Warning` — recoverable issues (e.g., unrecognized PDF structure element).
  - `Error` — failures that prevent completion.
  - `Debug` / `Trace` — detailed diagnostic info.
- All logs go to **stderr** (configured in `Program.cs`).

---

## 8. Dependencies & Package Management

- Use **Central Package Management** (`Directory.Packages.props`) if multiple projects exist.
- Pin package versions explicitly.
- Required packages:
  - `ModelContextProtocol` — MCP server SDK with hosting and DI.
  - `Microsoft.Extensions.Hosting` — Generic host for console apps.
  - A PDF-parsing library (evaluate options in ADR; candidates include `PdfPig`, `iText`, `QuestPDF`).
- Do **not** add `ModelContextProtocol.AspNetCore` — this is a stdio-only server.

---

## 9. Build & Run

```bash
# Build
dotnet build src/PdfToMarkdown

# Run directly (stdio mode — for VS Code MCP integration)
dotnet run --project src/PdfToMarkdown

# Run tests
dotnet test tests/PdfToMarkdown.Tests
```

- Ensure the project builds with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- Enable code analysis: `<EnableNETAnalyzers>true</EnableNETAnalyzers>` and `<AnalysisLevel>latest-recommended</AnalysisLevel>`.

---

## 10. Summary of Non-Negotiable Rules

1. **C# 13 on .NET 9** — use modern language features.
2. **File-scoped namespaces** — always.
3. **Nullable reference types enabled** — always.
4. **stdout is sacred** — never write anything to stdout; it belongs to MCP JSON-RPC.
5. **All logging to stderr** — via `ILogger<T>` with console provider configured for stderr.
6. **`[Description]` on all tools and parameters** — the LLM depends on these.
7. **Thin tools, fat services** — MCP tools delegate to DI-registered services.
8. **Async all the way** — no `.Result`, no `.Wait()`, accept `CancellationToken`.
9. **Output to disk** — tools write Markdown files alongside the PDF; return only confirmation.
10. **No HTTP** — stdio transport only; no ASP.NET Core references.
