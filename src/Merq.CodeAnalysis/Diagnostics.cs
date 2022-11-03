using Microsoft.CodeAnalysis;

namespace Merq;

public static class Diagnostics
{
    public static DiagnosticDescriptor InvalidAsyncOnSync { get; } = new(
        "MERQ001",
        "Using asynchronous invocation on synchronous command",
        "Command is synchronous and cannot be executed asynchronously.",
        "Build",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Invoking synchronous commands in an asynchronous way is not allowed.");

    public static DiagnosticDescriptor InvalidSyncOnAsync { get; } = new(
        "MERQ002",
        "Using synchronous invocation on asynchronous command",
        "Command is asynchronous and cannot be executed synchronously.",
        "Build",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Invoking asynchronous commands in a synchronous way can lead to deadlocks and is therefore not allowed.");

    public static DiagnosticDescriptor MissingCommandReturnType { get; } = new(
        "MERQ003",
        "Missing command return type",
        "Command handler is missing the command return type '{0}'.",
        "Build",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Command handlers must specify the same return type as the command they handle.");

    public static DiagnosticDescriptor WrongCommandReturnType { get; } = new(
        "MERQ004",
        "Wrong command return type",
        "Mismatched return type '{0}' for command '{1}' which returns '{2}'.",
        "Build",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Command handlers must specify the same return type as the command they handle.");

    public static DiagnosticDescriptor WrongCommandInterface { get; } = new(
        "MERQ005",
        "Command interface mismatch",
        "Command '{0}' does not implement interface '{1}' required by command handler.",
        "Build",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Commands must implement the interface that matches the handler's.");
}