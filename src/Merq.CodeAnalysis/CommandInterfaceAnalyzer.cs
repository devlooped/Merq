using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Merq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandInterfaceAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Diagnostics.WrongCommandInterface);

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
        if (declaration.BaseList?.ChildNodes().OfType<SimpleBaseTypeSyntax>()
            .Select(x => (x.Type, Symbol: semantic.GetSymbolInfo(x.Type).Symbol as INamedTypeSymbol))
            .Where(x => x.Symbol is not null && x.Symbol.IsGenericType)
            .FirstOrDefault(x => x.Symbol!.Name == "ICommandHandler" || x.Symbol.Name == "IAsyncCommandHandler") is not var (typeSyntax, handlerSymbol) ||
            typeSyntax.DescendantNodesAndSelf().OfType<GenericNameSyntax>().FirstOrDefault() is not GenericNameSyntax handlerName ||
            !handlerSymbol.IsGenericType ||
            handlerSymbol.TypeArguments[0] is not INamedTypeSymbol commandSymbol ||
            handlerName.TypeArgumentList.Arguments[0] is not TypeSyntax commandSyntax ||
            context.Compilation.GetTypeByMetadataName("Merq.ICommand") is not INamedTypeSymbol syncCmd ||
            context.Compilation.GetTypeByMetadataName("Merq.IAsyncCommand") is not INamedTypeSymbol asyncCmd ||
            context.Compilation.GetTypeByMetadataName("Merq.ICommand`1") is not INamedTypeSymbol syncCmdRet ||
            context.Compilation.GetTypeByMetadataName("Merq.IAsyncCommand`1") is not INamedTypeSymbol asyncCmdRet ||
            context.Compilation.GetTypeByMetadataName("Merq.ICommandHandler`1") is not INamedTypeSymbol syncHandler ||
            context.Compilation.GetTypeByMetadataName("Merq.IAsyncCommandHandler`1") is not INamedTypeSymbol asyncHandler ||
            context.Compilation.GetTypeByMetadataName("Merq.ICommandHandler`2") is not INamedTypeSymbol syncHandlerRet ||
            context.Compilation.GetTypeByMetadataName("Merq.IAsyncCommandHandler`2") is not INamedTypeSymbol asyncHandlerRet)
            return;

        var isAsync = handlerSymbol.Is(asyncHandler) || handlerSymbol.Is(asyncHandlerRet);
        var isSync = handlerSymbol.Is(syncHandler) || handlerSymbol.Is(syncHandlerRet);
        var handlerHasReturn = handlerSymbol.Is(asyncHandlerRet) || handlerSymbol.Is(syncHandlerRet);
        var commandHasReturn = commandSymbol.Is(asyncCmdRet) || commandSymbol.Is(syncCmdRet);
        var location = typeSyntax.GetLocation();
        var expectedInterface = handlerHasReturn ?
            (isAsync ? asyncCmdRet : syncCmdRet).Construct(handlerSymbol.TypeArguments[1]) :
            (isAsync ? asyncCmd : syncCmd);

        if (commandSymbol.Is(expectedInterface))
            return;

        var commandInterface = commandSymbol.AllInterfaces
            .Where(i => i.IsGenericType)
            .FirstOrDefault(i =>
                i.ConstructedFrom.Equals(asyncCmd, SymbolEqualityComparer.Default) ||
                i.ConstructedFrom.Equals(syncCmd, SymbolEqualityComparer.Default) ||
                i.ConstructedFrom.Equals(asyncCmdRet, SymbolEqualityComparer.Default) ||
                i.ConstructedFrom.Equals(syncCmdRet, SymbolEqualityComparer.Default));

        var properties = ImmutableDictionary<string, string?>.Empty
            .Add("TCommand", commandSymbol.ToFullName())
            .Add("TInterface", expectedInterface.ToFullName());

        if (isAsync)
            properties = properties.Add("IsAsync", "true");

        if (handlerHasReturn)
            properties = properties.Add("TResult", handlerSymbol.TypeArguments[1].ToFullName());

        // no interface at all
        if (commandInterface == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.WrongCommandInterface, location,
                properties.Add("Add", "true"),
                commandSymbol.Name,
                expectedInterface.ToMinimalDisplayString(semantic, declaration.SpanStart)));
        }
        // sync/async mismatch
        else if (isAsync && !(commandSymbol.Is(asyncCmd) || commandSymbol.Is(asyncCmdRet)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.WrongCommandInterface, location, properties, commandSymbol.Name,
                "IAsyncCommand"));
        }
        else if (isSync && !(commandSymbol.Is(syncCmd) || commandSymbol.Is(syncCmdRet)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.WrongCommandInterface, location, properties, commandSymbol.Name,
                "ICommand"));
        }
        // void/ret mismatch
        else if ((handlerHasReturn && !commandHasReturn) ||
            (!handlerHasReturn && commandHasReturn))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.WrongCommandInterface, location, properties, commandSymbol.Name,
                expectedInterface.ToMinimalDisplayString(semantic, declaration.SpanStart)));
        }
        // return type mismatch
        else if (handlerHasReturn && commandHasReturn &&
            !handlerSymbol.TypeArguments[1].Equals(commandInterface.TypeArguments[0], SymbolEqualityComparer.Default))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.WrongCommandInterface, location, properties, commandSymbol.Name,
                expectedInterface.ToMinimalDisplayString(semantic, declaration.SpanStart)));
        }
    }
}
