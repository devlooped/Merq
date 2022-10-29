using Microsoft.CodeAnalysis;

namespace Merq;

public static class Diagnostics
{
    static class AsyncOnSync
    {
        public const string DiagnosticId = "MERQ001";
        public static LocalizableString Title { get; } = "Using asynchronous invocation on synchronous command";
        public static LocalizableString MessageFormat { get; } = "Command is synchronous and cannot be executed asynchronously.";
        public static LocalizableString Description { get; } = "Invoking synchronous commands in an asynchronous way is not allowed.";
    }

    static class SyncOnAsync
    {
        public const string DiagnosticId = "MERQ002";
        public static LocalizableString Title { get; } = "Using synchronous invocation on asynchronous command";
        public static LocalizableString MessageFormat { get; } = "Command is asynchronous and cannot be executed synchronously.";
        public static LocalizableString Description { get; } = "Invoking asynchronous commands in a synchronous way can lead to deadlocks and is therefore not allowed.";
    }

    public static DiagnosticDescriptor AsyncOnSyncRule { get; } = new(AsyncOnSync.DiagnosticId, AsyncOnSync.Title, AsyncOnSync.MessageFormat, "Design", DiagnosticSeverity.Error, isEnabledByDefault: true, description: AsyncOnSync.Description);
    public static DiagnosticDescriptor SyncOnAsyncRule { get; } = new(SyncOnAsync.DiagnosticId, SyncOnAsync.Title, SyncOnAsync.MessageFormat, "Design", DiagnosticSeverity.Error, isEnabledByDefault: true, description: SyncOnAsync.Description);
}