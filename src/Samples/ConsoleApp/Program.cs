extern alias Library1;
extern alias Library2;
using Merq;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using static Spectre.Console.AnsiConsole;

// Initialize services
var collection = new ServiceCollection();
// Library1 contains [Service]-annotated classes, which will be automatically registered here.
collection.AddMessageBus(addDiscoveredServices: true, enableAutoMapping: true);

var services = collection.BuildServiceProvider();
var bus = services.GetRequiredService<IMessageBus>();

// Setup OpenTelemetry: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs
using var tracer = Sdk
    .CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ConsoleApp"))
    .AddSource("Merq.Core")
    .AddSource("Merq.AutoMapper")
    .AddConsoleExporter()
    .AddZipkinExporter()
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

// Test rapid fire messages
//Parallel.For(0, 10, i 
//    => bus.Execute(new Library2::Library.Echo($"Hello World ({i})")));
