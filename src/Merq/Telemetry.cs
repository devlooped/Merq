using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Merq;

static class Telemetry
{
    static readonly ActivitySource tracer = new("Merq", ThisAssembly.Info.Version);
    static Meter Meter { get; } = new Meter("Merq", ThisAssembly.Info.Version);

    static readonly Counter<long> commands;
    static readonly Counter<long> events;

    /// <summary>
    /// Duration of commands executed by the Merq message bus.
    /// </summary>
    public static Histogram<long> Processing { get; } =
        Meter.CreateHistogram<long>("Processing", unit: "ms", description: "Duration of commands executed by the Merq message bus.");

    /// <summary>
    /// Duration of event publishing by the Merq message bus.
    /// </summary>
    public static Histogram<long> Publishing { get; } =
        Meter.CreateHistogram<long>("Publishing", unit: "ms", description: "Duration of event publishing by the Merq message bus.");

    static Telemetry()
    {
        commands = Meter.CreateCounter<long>("Commands", description: "Commands executed by the Merq message bus.");
        events = Meter.CreateCounter<long>("Events", description: "Events published to the Merq message bus.");
    }

    // See https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#operation-names
    // publish:	A message is sent to a destination by a message producer/client.
    //          This would be the event being published to the bus. 
    public const string Publish = "publish";
    // receive:	A message is received from a destination by a message consumer/server.
    //          This would be the event being received by an event handler via the OnNext on the Subject.
    public const string Receive = "receive";
    // process: A message that was previously received from a destination is processed by a message consumer/server.
    //          The "destination" would be the message bus, which would process the execution of the command by invoking a handler.
    //          NOTE: this is not an entirely satisfactory way to tell events from commands apart.
    public const string Process = "process";

    public static Activity? StartCommandActivity(Type type, object command, string? callerName, string? callerFile, int? callerLine)
        => StartActivity(type, Process, callerName, callerFile, callerLine, "Command", command);

    public static Activity? StartEventActivity(Type type, object @event, string? callerName, string? callerFile, int? callerLine)
        => StartActivity(type, Publish, callerName, callerFile, callerLine, "Event", @event);

    public static Activity? StartActivity(Type type, string operation, string? callerName, string? callerFile, int? callerLine, string? property = default, object? value = default)
    {
        if (operation == Publish)
            events.Add(1, new KeyValuePair<string, object?>("Name", type.FullName));
        else if (operation == Process)
            commands.Add(1, new KeyValuePair<string, object?>("Name", type.FullName));

        // Span name convention should be: <destination> <operation> (see https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#span-name)
        // Requirement is that the destination has low cardinality.
        // The event/command is the destination in our case, and the operation distinguishes
        // events (publish/receive operations) from commands (process operation).
        var activity = tracer.CreateActivity($"{type.FullName} {operation}", ActivityKind.Producer)
            ?.SetTag("code.function", callerName)
            ?.SetTag("code.filepath", callerFile)
            ?.SetTag("code.lineno", callerLine)
            ?.SetTag("messaging.system", "merq")
            ?.SetTag("messaging.destination.name", type.FullName)
            ?.SetTag("messaging.destination.kind", "topic")
            ?.SetTag("messaging.operation", operation.ToLowerInvariant())
            ?.SetTag("messaging.protocol.name", type.Assembly.GetName().Name)
            ?.SetTag("messaging.protocol.version", type.Assembly.GetName().Version?.ToString() ?? "unknown");

        if (property != null && value != null &&
            // Additional optimization so we don't incur allocation of activity custom props storage 
            // unless someone is actually requesting the data. See https://github.com/open-telemetry/opentelemetry-dotnet/issues/1397 
            activity?.IsAllDataRequested == true)
            activity.SetCustomProperty(property, value);

        activity?.Start();

        return activity;
    }

    public static void RecordException(this Activity? activity, Exception e)
    {
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, e.Message);

            activity.SetTag("otel.status_code", "ERROR");
            activity.SetTag("otel.status_description", e.Message);

            // See https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/exceptions.md
            activity.AddEvent(new ActivityEvent("exception", tags: new()
            {
                { "exception.message", e.Message },
                { "exception.type", e.GetType().FullName },
                { "exception.stacktrace", e.ToString() },
            }));
        }
    }
}
