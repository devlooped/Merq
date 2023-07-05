using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Merq;

static class Telemetry
{
    static readonly ActivitySource tracer = new(ThisAssembly.Project.AssemblyName, ThisAssembly.Project.Version);

    public const string Execute = nameof(Execute);
    public const string Notify = nameof(Notify);

    public static Activity? StartActivity(Type type, string operation, [CallerMemberName] string? member = default, [CallerFilePath] string? file = default, [CallerLineNumber] int? line = default)
        => tracer.StartActivity(ActivityKind.Producer, name: $"{member}/{type.FullName}")
            ?.SetTag("code.function", member)
            ?.SetTag("code.filepath", file)
            ?.SetTag("code.lineno", line)
            ?.SetTag("messaging.system", "Merq")
            ?.SetTag("messaging.destination", type.FullName)
            ?.SetTag("messaging.destination_kind", "topic")
            ?.SetTag("messaging.operation", operation)
            ?.SetTag("messaging.protocol", type.Assembly.GetName().Name)
            ?.SetTag("messaging.protocol_version", type.Assembly.GetName().Version?.ToString() ?? "unknown");

    public static void RecordException(this Activity? activity, Exception e)
    {
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, e.Message);

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
