using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PdfToMarkdown.Services;

if (args.Length > 0)
{
    // CLI mode — perform a one-shot conversion and exit.
    var builder = Host.CreateApplicationBuilder([]);

    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    builder.Services
        .AddSingleton<IInputValidator, InputValidator>()
        .AddTransient<IMarkdownWriter, MarkdownWriter>()
        .AddSingleton<IPdfMarkdownConverter, PdfMarkdownConverter>()
        .AddTransient<IConversionOrchestrator, ConversionOrchestrator>()
        .AddTransient<CliRunner>();

    using IHost host = builder.Build();

    CliRunner runner = host.Services.GetRequiredService<CliRunner>();
    int exitCode = await runner.RunAsync(args, Console.Out, Console.Error, CancellationToken.None);

    Environment.Exit(exitCode);
}
else
{
    // MCP server mode — start the stdio MCP server.
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    builder.Services
        .AddSingleton<IInputValidator, InputValidator>()
        .AddTransient<IMarkdownWriter, MarkdownWriter>()
        .AddSingleton<IPdfMarkdownConverter, PdfMarkdownConverter>()
        .AddTransient<IConversionOrchestrator, ConversionOrchestrator>();

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    await builder.Build().RunAsync();
}
