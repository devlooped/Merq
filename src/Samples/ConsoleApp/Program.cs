extern alias Library1;
extern alias Library2;
using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using Merq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using static Spectre.Console.AnsiConsole;

var source = new ActivitySource("ConsoleApp");
var config = new ConfigurationBuilder()
    .AddUserSecrets(ThisAssembly.Project.UserSecretsId)
    .Build();

// Initialize services
var collection = new ServiceCollection();
// Add automapping message bus
collection.AddMessageBus<AutoMapperMessageBus>();
// Library1 contains [Service]-annotated classes, which will be automatically registered here.
collection.AddServices();

// Showcase collecting telemetry from external process
// Usage:
// - dotnet counters monitor --process-id [ID] --counters Merq
// - dotnet trace collect --name ConsoleApp --providers="Microsoft-Diagnostics-DiagnosticSource:::FilterAndPayloadSpecs=[AS]Merq,System.Diagnostics.Metrics:::Metrics=Merq"
collection.AddLogging(builder =>
{
    // This is added automatically in the default ASP.NET Core template
    builder.AddEventSourceLogger();
});

var services = collection.BuildServiceProvider();
var bus = services.GetRequiredService<IMessageBus>();

// .NET-style activity listening
using var listener = new ActivityListener
{
    ActivityStarted = activity => MarkupLine($"[grey]Activity started: {activity.OperationName}[/]"),
    ActivityStopped = activity => MarkupLine($"[grey]Activity stopped: {activity.OperationName}[/]"),
    Sample = (ref ActivityCreationOptions<ActivityContext> options) => options.Source.Name == "Merq" ? ActivitySamplingResult.AllData : ActivitySamplingResult.None,
    ShouldListenTo = source => source.Name == "Merq",
};
ActivitySource.AddActivityListener(listener);


// Setup OpenTelemetry: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
using var tracer = Sdk
    .CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ConsoleApp"))
    .AddSource(source.Name)
    .AddSource("Merq")
    .AddSource("Merq.AutoMapper")
    .AddConsoleExporter()
    .AddZipkinExporter()
    .AddAzureMonitorTraceExporter(o =>
    {
        if (string.IsNullOrEmpty(config["AppInsights"]))
            MarkupLine("[red]AppInsights instrumentation key not found. Set `AppInsights` user secret.[/]");
        else
            o.ConnectionString = config["AppInsights"];
    })
    .Build();

MarkupLine("[yellow]Executing with command from same assembly[/]");

// NOTE: we subscribe to an event in Library2, which is (duck)compatible with the one 
// notified by the Library1.Echo command handler!
bus.Observe<Library2::Library.OnDidSay>()
    .Subscribe(e => MarkupLine($"[red]Received Library2:{e.GetType().Name}.Message={e.Message}[/]"));

// Also observe the original message, for comparison
bus.Observe<Library1::Library.OnDidSay>()
    .Subscribe(e => MarkupLine($"[lime]Received Library1:{e.GetType().Name}.Message={e.Message}[/]"));

// We can execute passing an object of the same type/assembly as the EchoHandler expects
var message = bus.Execute(new Library1::Library.Echo("Hello World"));

WriteLine(message);

MarkupLine("[yellow]Executing with command from different assembly[/]");

// But we can also execute passing an object from an entirely different assembly
message = bus.Execute(new Library2::Library.Echo("Hello World"));

WriteLine(message);

try
{
    MarkupLine("[yellow]Showcase error telemetry with invalid empty message[/]");
    using var _ = source.StartActivity("Error");
    // Showcase error telemetry
    bus.Execute(new Library1::Library.Echo(""));
}
catch (NotSupportedException)
{
}

// Simulate long-running to collect telemetry from external process
//while (true)
//{
//    bus.Execute(new Library2::Library.Echo($"Hello World {Random.Shared.Next(0, 100)}"));
//    await Task.Delay(500);
//}

// Test rapid fire messages
//Parallel.For(0, 10, i 
//    => bus.Execute(new Library2::Library.Echo($"Hello World ({i})")));
