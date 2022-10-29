using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Merq;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public class CommandExecuteAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.AsyncOnSyncRule, Diagnostics.SyncOnAsyncRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        //context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);


#pragma warning disable RS1012 // Start action has no registered actions
        context.RegisterCompilationStartAction(c =>
        {
            if (c.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.DebugMerqAnalyzer", out var debugValue) &&
                bool.TryParse(debugValue, out var shouldDebug) &&
                shouldDebug)
            {
                Debugger.Launch();
            }
        });
#pragma warning restore RS1012 // Start action has no registered actions
    }

    static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semantic = context.SemanticModel;
        var target = semantic.GetSymbolInfo(invocation.Expression);

        // Overload resolution worked fine, nothing else to do.
        var method = semantic.GetSymbolInfo(invocation);
        if (method.Symbol != null)
            return;

        // First deal with direct invocations on IMessageBus.
        // TODO: consider IMessageBusExtensions too.
        var busType = context.Compilation.GetTypeByMetadataName("Merq.IMessageBus");
        var asyncCmd = context.Compilation.GetTypeByMetadataName("Merq.IAsyncCommand");
        var syncCmd = context.Compilation.GetTypeByMetadataName("Merq.ICommand");

        if (busType == null || asyncCmd == null || syncCmd == null)
            return;

        if (method.CandidateSymbols.OfType<IMethodSymbol>().All(x => !IsType(busType, x.ContainingType)))
            return;

        var isAsync = method.CandidateSymbols.All(x => x.Name == "ExecuteAsync");
        var isSync = method.CandidateSymbols.All(x => x.Name == "Execute");

        // It's some other method, but not command execution.
        if (!isAsync && !isSync)
            return;

        var args = invocation.ArgumentList.Arguments.Select(x => semantic.GetSymbolInfo(x.Expression).Symbol).FirstOrDefault();
        if (args == null)
            return;

        var command = args switch
        {
            IMethodSymbol m => m.MethodKind == MethodKind.Constructor ? m.ContainingType : !m.ReturnsVoid ? m.ReturnType : null,
            IPropertySymbol p => p.Type,
            IFieldSymbol f => f.Type,
            ILocalSymbol l => l.Type,
            _ => null
        };

        if (command == null)
            return;

        if (isAsync && IsType(syncCmd, command))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.AsyncOnSyncRule, invocation.ArgumentList.Arguments[0].GetLocation()));
        }
        else if (isSync && IsType(asyncCmd, command))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SyncOnAsyncRule, invocation.ArgumentList.Arguments[0].GetLocation()));
        }
    }

    static bool IsType(ITypeSymbol? expected, ITypeSymbol? actual)
    {
        if (expected == null || actual == null)
            return false;

        if (actual.Equals(expected, SymbolEqualityComparer.Default) == true)
            return true;

        if (expected.BaseType?.Name.Equals("object", StringComparison.OrdinalIgnoreCase) == true)
            return false;

        foreach (var iface in actual.AllInterfaces)
            if (iface.Equals(expected, SymbolEqualityComparer.Default) == true)
                return true;

        return IsType(expected.BaseType, actual);
    }


    static void AnalyzeOperation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;

        Console.WriteLine(invocation.TargetMethod.Name);
    }
}
