using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Merq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PublicCommandAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.CommandTypesShouldBePublic);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeTypeSymbol, SymbolKind.NamedType);
    }

    static void AnalyzeTypeSymbol(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;
        if (context.Compilation.GetTypeByMetadataName("Merq.ICommand") is not INamedTypeSymbol syncCmd ||
            context.Compilation.GetTypeByMetadataName("Merq.IAsyncCommand") is not INamedTypeSymbol asyncCmd ||
            context.Compilation.GetTypeByMetadataName("Merq.ICommand`1") is not INamedTypeSymbol syncCmdRet ||
            context.Compilation.GetTypeByMetadataName("Merq.IAsyncCommand`1") is not INamedTypeSymbol asyncCmdRet)
            return;

        if (namedType.DeclaredAccessibility == Accessibility.Public)
            return;

        if (namedType.Is(syncCmd) || namedType.Is(asyncCmd) || namedType.Is(syncCmdRet) || namedType.Is(asyncCmdRet))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.CommandTypesShouldBePublic,
                namedType.Locations[0],
                namedType.Name));
        }
    }
}
