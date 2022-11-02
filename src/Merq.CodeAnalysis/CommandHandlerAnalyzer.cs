using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Merq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Diagnostics.MissingCommandReturnType,
            Diagnostics.WrongCommandReturnType);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(c =>
        {
            c.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ClassDeclaration);
            c.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.RecordDeclaration);
        });
    }

    static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var declaration = (TypeDeclarationSyntax)context.Node;
        var semantic = context.SemanticModel;
        // If we can get the symbol, the type has been properly declared already.
        if (semantic.GetSymbolInfo(declaration).Symbol is INamedTypeSymbol)
            return;

        if (declaration.BaseList?.ChildNodes().OfType<SimpleBaseTypeSyntax>()
            .Where(x => x.Type is GenericNameSyntax generic)
            .Select(x => (GenericNameSyntax)x.Type)
            .Where(x => x.Identifier.ToString() == "ICommandHandler" || x.Identifier.ToString() == "IAsyncCommandHandler")
            .FirstOrDefault() is not GenericNameSyntax handlerName)
            return;

        if (semantic.GetSymbolInfo(handlerName).Symbol is not INamedTypeSymbol handlerSymbol ||
            !handlerSymbol.IsGenericType)
            return;

        var asyncCmd = context.Compilation.GetTypeByMetadataName("Merq.IAsyncCommand`1");
        var syncCmd = context.Compilation.GetTypeByMetadataName("Merq.ICommand`1");

        if (asyncCmd == null || syncCmd == null)
            return;

        if (handlerSymbol.TypeArguments.Length == 1)
        {
            if (handlerSymbol.TypeArguments[0] is INamedTypeSymbol cmdSymbol &&
                cmdSymbol.AllInterfaces
                    .Where(i => i.IsGenericType)
                    .FirstOrDefault(i =>
                        i.ConstructedFrom.Equals(asyncCmd, SymbolEqualityComparer.Default) ||
                        i.ConstructedFrom.Equals(syncCmd, SymbolEqualityComparer.Default)) is INamedTypeSymbol cmdInterface)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingCommandReturnType,
                    handlerName.GetLocation(),
                    ImmutableDictionary<string, string?>.Empty
                        .Add("CommandType", cmdSymbol.ToFullName())
                        .Add("ReturnType", cmdInterface.TypeArguments[0].ToFullName()),
                    cmdInterface.TypeArguments[0].ToMinimalDisplayString(semantic, context.Node.SpanStart)));
            }
        }
        else if (handlerSymbol.TypeArguments.Length == 2)
        {
            if (handlerSymbol.TypeArguments[0] is INamedTypeSymbol cmdSymbol &&
                cmdSymbol.AllInterfaces
                    .Where(i => i.IsGenericType)
                    .FirstOrDefault(i =>
                        i.ConstructedFrom.Equals(asyncCmd, SymbolEqualityComparer.Default) ||
                        i.ConstructedFrom.Equals(syncCmd, SymbolEqualityComparer.Default)) is INamedTypeSymbol cmdInterface &&
                !cmdInterface.TypeArguments[0].Equals(handlerSymbol.TypeArguments[1], SymbolEqualityComparer.Default))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.WrongCommandReturnType,
                    handlerName.GetLocation(),
                    ImmutableDictionary<string, string?>.Empty
                        .Add("CommandType", cmdSymbol.ToFullName())
                        .Add("ReturnType", cmdInterface.TypeArguments[0].ToFullName()),
                    handlerSymbol.TypeArguments[1].ToMinimalDisplayString(semantic, context.Node.SpanStart),
                    cmdSymbol.ToMinimalDisplayString(semantic, context.Node.SpanStart),
                    cmdInterface.TypeArguments[0].ToMinimalDisplayString(semantic, context.Node.SpanStart)));
            }
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
}
