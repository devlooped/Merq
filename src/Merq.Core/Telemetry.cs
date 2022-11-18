using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Merq;

static class Telemetry
{
    static readonly ActivitySource tracer = new(ThisAssembly.Project.AssemblyName, ThisAssembly.Project.Version);

    public static Activity? StartActivity(Type type, string operation = "process", [CallerMemberName] string? member = default, [CallerFilePath] string? file = default, [CallerLineNumber] int? line = default)
        => tracer.StartActivity(ActivityKind.Producer, name: $"{member}/{type.FullName}")
            ?.SetTag("code.function", member)
            ?.SetTag("code.filepath", file)
            ?.SetTag("code.lineno", line)
            ?.SetTag("messaging.system", "merq")
            ?.SetTag("messaging.destination", type.FullName)
            ?.SetTag("messaging.destination_kind", "topic")
            ?.SetTag("messaging.operation", operation)
            ?.SetTag("messaging.protocol", type.Assembly.GetName().Name)
            ?.SetTag("messaging.protocol_version", type.Assembly.GetName().Version?.ToString() ?? "unknown");
}
