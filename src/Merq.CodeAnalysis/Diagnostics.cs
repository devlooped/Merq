﻿using Microsoft.CodeAnalysis;

namespace Merq;

public static class Diagnostics
{
    /// <summary>
    /// MERQ001: Using asynchronous invocation on synchronous command
    /// </summary>
    public static DiagnosticDescriptor InvalidAsyncOnSync { get; } = new(
        "MERQ001",
        "Using asynchronous invocation on synchronous command",
        "Command is synchronous and cannot be executed asynchronously.",
        "Build",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Invoking synchronous commands in an asynchronous way is not allowed.");

    /// <summary>
    /// MERQ002: Using synchronous invocation on asynchronous command
    /// </summary>
    public static DiagnosticDescriptor InvalidSyncOnAsync { get; } = new(
        "MERQ002",
        "Using synchronous invocation on asynchronous command",
        "Command is asynchronous and cannot be executed synchronously.",
        "Build",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Invoking asynchronous commands in a synchronous way can lead to deadlocks and is therefore not allowed.");

    /// <summary>
    /// MERQ003: Missing command return type
    /// </summary>
    public static DiagnosticDescriptor MissingCommandReturnType { get; } = new(
        "MERQ003",
        "Missing command return type",
        "Command handler is missing the command return type '{0}'.",
        "Build",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Command handlers must specify the same return type as the command they handle.");

    /// <summary>
    /// MERQ004: Wrong command return type
    /// </summary>
    public static DiagnosticDescriptor WrongCommandReturnType { get; } = new(
        "MERQ004",
        "Wrong command return type",
        "Mismatched return type '{0}' for command '{1}' which returns '{2}'.",
        "Build",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Command handlers must specify the same return type as the command they handle.");

    /// <summary>
    /// MERQ005: Command interface mismatch
    /// </summary>
    public static DiagnosticDescriptor WrongCommandInterface { get; } = new(
        "MERQ005",
        "Command interface mismatch",
        "Command '{0}' does not implement interface '{1}' required by command handler.",
        "Build",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Commands must implement the interface that matches the handler's.");

    /// <summary>
    /// MERQ006: Factory method is not accessible
    /// </summary>
    public static DiagnosticDescriptor CreateMethodNotAccessible { get; } = new(
        "MERQ006",
        "Factory method is not accessible",
        "Factory method '{0}.{1}' is not accessible within the current compilation to support hierarchical dynamic conversion.",
        "Build",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In order to support automatic hierarchical dynamic conversion for records, the Create method must be accessible within the compilation.");

    /// <summary>
    /// MERQ007: Consider making command types public.
    /// </summary>
    public static DiagnosticDescriptor CommandTypesShouldBePublic { get; } = new(
        "MERQ007",
        "Consider making command types public",
        "Consider making command '{0}' public.",
        "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Public commands have better performance characteristics when dynamic conversion (aka 'duck-typing') is used.");
}