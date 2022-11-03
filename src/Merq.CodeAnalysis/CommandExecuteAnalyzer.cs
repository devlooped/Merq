using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Merq;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public class CommandExecuteAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.InvalidAsyncOnSync, Diagnostics.InvalidSyncOnAsync);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(c =>
        {
            c.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        });
    }

    static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semantic = context.SemanticModel;

        // Overload resolution worked fine, nothing else to do.
        var method = semantic.GetSymbolInfo(invocation);
        if (method.Symbol != null)
            return;

        // First deal with direct invocations on IMessageBus.
        // TODO: consider IMessageBusExtensions too.
        var busType = context.Compilation.GetTypeByMetadataName("Merq.IMessageBus");
        var asyncCmd = context.Compilation.GetTypeByMetadataName("Merq.IAsyncCommand");
        var asyncCmdRet = context.Compilation.GetTypeByMetadataName("Merq.IAsyncCommand`1");
        var syncCmd = context.Compilation.GetTypeByMetadataName("Merq.ICommand");
        var syncCmdRet = context.Compilation.GetTypeByMetadataName("Merq.ICommand`1");

        if (busType == null || asyncCmd == null || syncCmd == null || asyncCmdRet == null || syncCmdRet == null)
            return;

        if (method.CandidateSymbols.OfType<IMethodSymbol>().All(x => !x.ContainingType.Is(busType)))
            return;

        var isAsync = method.CandidateSymbols.All(x => x.Name == "ExecuteAsync");
        var isSync = method.CandidateSymbols.All(x => x.Name == "Execute");

        // It's some other method, but not command execution.
        if (!isAsync && !isSync)
            return;

        // When attempting to execute a command passing just the TCommand, instead 
        // of resolving to the IMessageBusExtensions, the compiler will provide as 
        // candidate the Execute<TResult> and ExecuteAsync<TResult> methods in IMessageBus 
        // instead. In this case, we need to get the command type from the return type 
        // of those methods, even if they are the incorrect compiler resolution.
        var returnType = method.CandidateSymbols
            .OfType<IMethodSymbol>()
            .Where(x => x.IsGenericMethod && !x.ReturnsVoid && x.ReturnType is INamedTypeSymbol)
            .Select(x => (INamedTypeSymbol)x.ReturnType)
            // In the async case, we'll get Task<T> as return type
            .Select(x => isAsync && x.IsGenericType ? x.TypeArguments[0] : x)
            .FirstOrDefault();

        var arg = invocation.ArgumentList.Arguments.Select(x => semantic.GetSymbolInfo(x.Expression).Symbol).FirstOrDefault();
        var location = invocation.ArgumentList.Arguments.Select(x => x.GetLocation())
            // Use either the location of the first argument in the regular case
            .Concat(invocation.DescendantNodesAndSelf()
            // Or the generic argument location in the second case
            .OfType<GenericNameSyntax>()
            .SelectMany(x => x.TypeArgumentList.Arguments.Select(x => x.GetLocation())))
            .FirstOrDefault();

        // If we got no command argument, then get the command type from the false match 
        // on the generic return type.
        arg ??= returnType;

        if (arg == null)
            return;

        var command = arg switch
        {
            IMethodSymbol m => m.MethodKind == MethodKind.Constructor ? m.ContainingType : !m.ReturnsVoid ? m.ReturnType : null,
            IPropertySymbol p => p.Type,
            IFieldSymbol f => f.Type,
            ILocalSymbol l => l.Type,
            ITypeSymbol t => t,
            _ => null
        };

        if (command == null)
            return;

        if (isAsync && (command.Is(syncCmd) || command.Is(syncCmdRet)))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidAsyncOnSync, location));
        }
        else if (isSync && (command.Is(asyncCmd) || command.Is(asyncCmdRet)))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidSyncOnAsync, location));
        }
    }
}