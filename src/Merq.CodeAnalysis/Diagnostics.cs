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

    public static DiagnosticDescriptor AsyncOnSyncRule { get; } = new(AsyncOnSync.DiagnosticId, AsyncOnSync.Title, AsyncOnSync.MessageFormat, "Design", DiagnosticSeverity.Error, isEnabledByDefault: true, description: AsyncOnSync.Description);
    public static DiagnosticDescriptor SyncOnAsyncRule { get; } = new(SyncOnAsync.DiagnosticId, SyncOnAsync.Title, SyncOnAsync.MessageFormat, "Design", DiagnosticSeverity.Error, isEnabledByDefault: true, description: SyncOnAsync.Description);
}