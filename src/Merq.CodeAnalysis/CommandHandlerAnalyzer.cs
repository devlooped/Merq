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
            .Select(x => (x.Type, Symbol: semantic.GetSymbolInfo(x.Type).Symbol as INamedTypeSymbol))
            .Where(x => x.Symbol is not null && x.Symbol.IsGenericType)
            .FirstOrDefault(x => x.Symbol!.Name == "ICommandHandler" || x.Symbol.Name == "IAsyncCommandHandler") is not var (type, symbol))
            return;

        (var handlerName, var handlerSymbol) = (type, symbol);
        if (handlerName == null || handlerSymbol == null)
            return;

        var asyncCmd = context.Compilation.GetTypeByMetadataName("Merq.IAsyncCommand`1");
        var syncCmd = context.Compilation.GetTypeByMetadataName("Merq.ICommand`1");

        if (asyncCmd == null || syncCmd == null)
            return;

        if (handlerSymbol.TypeArguments[0] is not INamedTypeSymbol cmdSymbol)
            return;

        var cmdInterface = cmdSymbol.AllInterfaces
                .Where(i => i.IsGenericType)
                .FirstOrDefault(i =>
                    i.ConstructedFrom.Equals(asyncCmd, SymbolEqualityComparer.Default) ||
                    i.ConstructedFrom.Equals(syncCmd, SymbolEqualityComparer.Default));

        if (cmdInterface == null)
            return;

        if (handlerSymbol.TypeArguments.Length == 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingCommandReturnType,
                handlerName.GetLocation(),
                ImmutableDictionary<string, string?>.Empty
                    .Add("TCommand", cmdSymbol.ToFullName())
                    .Add("TReturn", cmdInterface.TypeArguments[0].ToFullName()),
                cmdInterface.TypeArguments[0].ToMinimalDisplayString(semantic, context.Node.SpanStart)));
        }
        else if (handlerSymbol.TypeArguments.Length == 2 &&
            !cmdInterface.TypeArguments[0].Equals(handlerSymbol.TypeArguments[1], SymbolEqualityComparer.Default))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.WrongCommandReturnType,
                handlerName.GetLocation(),
                ImmutableDictionary<string, string?>.Empty
                    .Add("TCommand", cmdSymbol.ToFullName())
                    .Add("TReturn", cmdInterface.TypeArguments[0].ToFullName())
                    .Add("TInterface", handlerSymbol.Name == "ICommandHandler" ?
                        syncCmd.Construct(handlerSymbol.TypeArguments[1]).ToFullName() :
                        asyncCmd.Construct(handlerSymbol.TypeArguments[1]).ToFullName()),
                handlerSymbol.TypeArguments[1].ToMinimalDisplayString(semantic, context.Node.SpanStart),
                cmdSymbol.ToMinimalDisplayString(semantic, context.Node.SpanStart),
                cmdInterface.TypeArguments[0].ToMinimalDisplayString(semantic, context.Node.SpanStart)));
        }
    }
}
