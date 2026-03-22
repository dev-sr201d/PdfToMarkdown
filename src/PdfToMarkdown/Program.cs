using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PdfToMarkdown.Services;

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
