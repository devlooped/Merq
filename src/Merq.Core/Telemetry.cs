﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Merq;

static class Telemetry
{
    static readonly ActivitySource tracer = new("Merq", ThisAssembly.Project.Version);
    static Meter Meter { get; } = new Meter("Merq", ThisAssembly.Project.Version);

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
    //          This would be the event being published to the bus. Granted 
    public const string Publish = nameof(Publish);
    // receive:	A message is received from a destination by a message consumer/server.
    //          This would be the event being received by an event handler via the OnNext on the Subject.
    public const string Receive = nameof(Receive);
    // process: A message that was previously received from a destination is processed by a message consumer/server.
    //          The "destination" would be the message bus, which would process the execution of the command by invoking a handler.
    //          NOTE: this is not an entirely satisfactory way to tell events from commands apart.
    public const string Process = nameof(Process);

    public static Activity? StartCommandActivity(Type type, object command) => StartActivity(type, Process, "Command", command);

    public static Activity? StartEventActivity(Type type, object @event) => StartActivity(type, Publish, "Event", @event);

    public static Activity? StartActivity(Type type, string operation, string? property = default, object? value = default,
        [CallerMemberName] string? member = default, [CallerFilePath] string? file = default, [CallerLineNumber] int? line = default)
    {
        if (operation == Publish)
            events.Add(1, new KeyValuePair<string, object?>("Name", type.FullName));
        else if (operation == Process)
            commands.Add(1, new KeyValuePair<string, object?>("Name", type.FullName));

        // Span name convention should be: <destination> <operation> (see https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#span-name)
        // Requirement is that the destination has low cardinality. In our case, the destination is 
        // the logical operation being performed, such as "Execute", "Notify" or "Deliver". The 
        // operation is actually the type being acted on (such as CreateUser -a command- or UserCreated -event).
        var activity = tracer.CreateActivity($"{operation}/{type.FullName}", ActivityKind.Producer)
            ?.SetTag("code.function", member)
            ?.SetTag("code.filepath", file)
            ?.SetTag("code.lineno", line)
            ?.SetTag("messaging.system", "merq")
            ?.SetTag("messaging.destination.name", type.FullName)
            ?.SetTag("messaging.destination.kind", "topic")
            ?.SetTag("messaging.operation", operation.ToLowerInvariant())
            ?.SetTag("messaging.protocol.name", type.Assembly.GetName().Name)
            ?.SetTag("messaging.protocol.version", type.Assembly.GetName().Version?.ToString() ?? "unknown");

        if (property != null && value != null)
            activity?.SetCustomProperty(property, value);

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
