using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Merq.CodeFixes;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class CommandInterfaceFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Diagnostics.WrongCommandInterface.Id,
        Diagnostics.WrongCommandReturnType.Id);

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var changeReturn = context.Diagnostics.FirstOrDefault(x => x.Id == Diagnostics.WrongCommandReturnType.Id);

        if (context.Diagnostics.FirstOrDefault() is not Diagnostic diagnostic ||
            !diagnostic.Properties.TryGetValue("TCommand", out var commandType) ||
            commandType is null ||
            !diagnostic.Properties.TryGetValue("TInterface", out var interfaceType) ||
            interfaceType is null ||
            await context.Document.GetSemanticModelAsync(context.CancellationToken) is not SemanticModel semantic ||
            await context.Document.Project.GetCompilationAsync(context.CancellationToken) is not Compilation compilation ||
            compilation.GetTypeByFullName(commandType) is not INamedTypeSymbol commandSymbol ||
            compilation.GetTypeByFullName(interfaceType) is not INamedTypeSymbol interfaceSymbol ||
            commandSymbol.DeclaringSyntaxReferences.FirstOrDefault() is not SyntaxReference commandReference ||
            await commandReference.GetSyntaxAsync(context.CancellationToken) is not TypeDeclarationSyntax commandSyntax ||
            context.Document.Project.GetDocument(commandReference.SyntaxTree) is not Document commandDocument)
            return;

        var title = $"Implement '{interfaceSymbol.ToMinimalDisplayString(semantic, context.Span.Start)}' on '{commandSymbol.Name}'";
        string? returnType = default;

        if (changeReturn is not null)
        {
            returnType = interfaceSymbol.TypeArguments[0].ToFullName();
            title = $"Change command return type to '{interfaceSymbol.TypeArguments[0].ToMinimalDisplayString(semantic, context.Span.Start)}' for '{commandSymbol.Name}'";
        }

        context.RegisterCodeFix(new SetCommandInterfaceAction(new Args(
            context.Document, commandDocument, context.Span, title, compilation, semantic,
            commandSyntax, commandSymbol, commandType, interfaceSymbol, interfaceType, returnType)), diagnostic);
    }

    record Args(
        Document HandlerDocument,
        Document CommandDocument, TextSpan Span, string Title, Compilation Compilation, SemanticModel Semantic,
        TypeDeclarationSyntax CommandSyntax,
        INamedTypeSymbol CommandSymbol, string CommandType,
        INamedTypeSymbol InterfaceSymbol, string InterfaceTypeName, string? ReturnType)
    {
        // It makes sense for the command + interface names used in the code fix action title to be minimal 
        // with regards to the codefix action context, rather than the command declaration context which could 
        // be an entirely different file with different usings.
        //public string CommandName { get; } = CommandSymbol.ToMinimalDisplayString(Semantic, Span.Start);
        //public string InterfaceName { get; } = InterfaceSymbol.ToMinimalDisplayString(Semantic, Span.Start);
    }

    class SetCommandInterfaceAction : CodeAction
    {
        readonly Args args;

        public SetCommandInterfaceAction(Args args) => this.args = args;

        public override string Title => args.Title;

        public override string? EquivalenceKey => Title;

        protected override async Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            var root = await args.CommandDocument.GetSyntaxRootAsync(cancellationToken);
            if (root == null)
                return args.CommandDocument.Project.Solution;

            // Add annotation to command syntax node for easier tracking
            root = root.ReplaceNode(args.CommandSyntax, args.CommandSyntax.WithAdditionalAnnotations(new SyntaxAnnotation("CommandSyntax")));
            var document = args.CommandDocument.WithSyntaxRoot(root);
            root = await document.GetSyntaxRootAsync(cancellationToken);

            if (root == null ||
                root.GetAnnotatedNodes("CommandSyntax").OfType<TypeDeclarationSyntax>().FirstOrDefault() is not TypeDeclarationSyntax commandSyntax ||
                await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel semantic ||
                await document.Project.GetCompilationAsync(cancellationToken) is not Compilation compilation ||
                compilation.GetTypeByFullName(args.CommandType) is not INamedTypeSymbol commandSymbol)
                return args.CommandDocument.Project.Solution;

            if (args.ReturnType != null &&
                compilation.GetTypeByFullName(args.ReturnType) is INamedTypeSymbol returnType &&
                document.Project.GetDocument(args.HandlerDocument.Id) is Document handlerDoc &&
                await handlerDoc.GetSemanticModelAsync(cancellationToken) is SemanticModel handlerSemantic &&
                await handlerDoc.GetSyntaxRootAsync(cancellationToken) is SyntaxNode handlerRoot)
            {
                var rewriter = new ExecuteReturnRewriter(handlerSemantic, commandSymbol,
                    returnType.ToMinimalDisplayString(handlerSemantic, args.Span.Start));

                handlerDoc = handlerDoc.WithSyntaxRoot(rewriter.Visit(handlerRoot));
                document = handlerDoc.Project.GetDocument(document.Id);
                if (document == null)
                    return args.CommandDocument.Project.Solution;

                root = await document.GetSyntaxRootAsync(cancellationToken);
                if (root?.GetAnnotatedNodes("CommandSyntax").OfType<TypeDeclarationSyntax>().FirstOrDefault() is not TypeDeclarationSyntax syntax)
                    return args.CommandDocument.Project.Solution;

                commandSyntax = syntax;

                if (await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
                    return args.CommandDocument.Project.Solution;

                semantic = model;
            }

            // Determine if document contains import for Merq namespace
            if (root.DescendantNodes(node => node.IsKind(SyntaxKind.CompilationUnit) || node.IsKind(SyntaxKind.NamespaceDeclaration))
                    .OfType<UsingDirectiveSyntax>()
                    .FirstOrDefault(node => node.Name.ToString() == "Merq") is null &&
                root.DescendantNodesAndSelf()
                    .OfType<CompilationUnitSyntax>()
                    .FirstOrDefault() is CompilationUnitSyntax unit)
            {
                var usingMerq = UsingDirective(IdentifierName("Merq")).WithTrailingTrivia(CarriageReturnLineFeed);

                if (unit
                    .DescendantNodes(node => node.IsKind(SyntaxKind.NamespaceDeclaration))
                    .OfType<UsingDirectiveSyntax>()
                    .LastOrDefault() is UsingDirectiveSyntax lastUsing &&
                    lastUsing.Parent != null)
                {
                    root = root.ReplaceNode(
                        lastUsing.Parent,
                        lastUsing.Parent.InsertNodesAfter(lastUsing, new[] { usingMerq }));
                }
                else if (unit
                    .ChildNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .LastOrDefault() is NamespaceDeclarationSyntax lastNamespace)
                {
                    root = root.ReplaceNode(lastNamespace, lastNamespace.AddUsings(usingMerq));
                }
                else
                {
                    // It's fine adding to the top, since we didn't find other usings, or we have a 
                    // FileScopedNamespaceDeclarationSyntax instead of a NamespaceDeclarationSyntax
                    root = root.ReplaceNode(unit, unit.AddUsings(usingMerq));
                }

                document = document.WithSyntaxRoot(root);
                root = await document.GetSyntaxRootAsync(cancellationToken);
                if (root?.GetAnnotatedNodes("CommandSyntax").OfType<TypeDeclarationSyntax>().FirstOrDefault() is not TypeDeclarationSyntax syntax)
                    return args.CommandDocument.Project.Solution;

                commandSyntax = syntax;
                if (await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
                    return args.CommandDocument.Project.Solution;

                semantic = model;
            }

            if (await document.Project.GetCompilationAsync(cancellationToken) is not Compilation comp ||
                comp.GetTypeByFullName(args.InterfaceTypeName) is not INamedTypeSymbol interfaceSymbol)
                return args.CommandDocument.Project.Solution;

            var interfaceSyntax = ParseTypeName(interfaceSymbol.ToMinimalDisplayString(semantic, commandSyntax.SpanStart));

            var existingInterface = commandSyntax.BaseList?.Types
                .Select(x => (Syntax: x, semantic.GetSymbolInfo(x.Type).Symbol))
                .FirstOrDefault(x => x.Symbol is INamedTypeSymbol symbol && (symbol.Name == "ICommand" || symbol.Name == "IAsyncCommand"));

            if (existingInterface?.Symbol is not null)
            {
                return document.WithSyntaxRoot(root.ReplaceNode(existingInterface.Value.Syntax,
                    existingInterface.Value.Syntax.WithType(interfaceSyntax))).Project.Solution;
            }

            return document.WithSyntaxRoot(root.ReplaceNode(commandSyntax,
                commandSyntax.AddBaseListTypes(SimpleBaseType(interfaceSyntax)))).Project.Solution;
        }
    }
}