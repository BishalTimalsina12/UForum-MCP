using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr (required for MCP stdio transport)
builder.Logging.AddConsole(consoleLogOptions =>
{
    // All logs go to stderr to not interfere with MCP stdio protocol
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Add HttpClient for Umbraco forum API calls
builder.Services.AddHttpClient();

// Register MCP server with stdio transport and tools
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(); // Auto-discovers all [McpServerTool] methods

await builder.Build().RunAsync();
