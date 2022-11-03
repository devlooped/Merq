using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Merq.CodeFixes;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class SetCommandReturnTypeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Diagnostics.MissingCommandReturnType.Id,
        Diagnostics.WrongCommandReturnType.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        if (root.FindNode(context.Span).FirstAncestorOrSelf<SimpleBaseTypeSyntax>() is not SimpleBaseTypeSyntax typeSyntax)
            return;

        if (context.Diagnostics.FirstOrDefault() is not Diagnostic diagnostic ||
            !diagnostic.Properties.TryGetValue("TCommand", out var commandType) ||
            commandType is null ||
            !diagnostic.Properties.TryGetValue("TReturn", out var returnType) ||
            returnType is null ||
            await context.Document.GetSemanticModelAsync(context.CancellationToken) is not SemanticModel semantic ||
            await context.Document.Project.GetCompilationAsync(context.CancellationToken) is not Compilation compilation ||
            compilation.GetTypeByFullName(returnType) is not INamedTypeSymbol returnSymbol)
            return;

        context.RegisterCodeFix(new SetCommandReturnTypeAction(
                compilation,
                context.Document,
                root, typeSyntax,
                commandType,
                returnSymbol.ToMinimalDisplayString(semantic, context.Span.Start)),
            context.Diagnostics);
    }

    class SetCommandReturnTypeAction : CodeAction
    {
        readonly Document document;
        readonly SyntaxNode root;
        readonly SimpleBaseTypeSyntax baseType;
        readonly string commandType;
        readonly string returnType;

        public SetCommandReturnTypeAction(Compilation compilation, Document document, SyntaxNode root, SimpleBaseTypeSyntax typeSyntax, string commandType, string returnType)
            => (this.document, this.root, baseType, this.commandType, this.returnType)
            = (document, root, typeSyntax, commandType, returnType);

        public override string Title => $"Fix command handler return type to '{returnType}'";
        public override string EquivalenceKey => Title;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            // Annotate generic name for tracking
            var root = this.root.ReplaceNode(this.baseType, this.baseType.WithAdditionalAnnotations(new SyntaxAnnotation("BaseType")));
            var document = this.document.WithSyntaxRoot(root);
            root = await document.GetSyntaxRootAsync(cancellationToken);

            if (await document.Project.GetCompilationAsync(cancellationToken) is not Compilation compilation ||
                await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel semantic ||
                compilation.GetTypeByFullName(commandType) is not INamedTypeSymbol commandSymbol ||
                root == null)
                return this.document;

            root = new ExecuteReturnRewriter(semantic, commandSymbol, returnType).Visit(root);
            var baseType = (SimpleBaseTypeSyntax)root.GetAnnotatedNodes("BaseType").Single();

            if (baseType.Type.DescendantNodesAndSelf().OfType<GenericNameSyntax>().FirstOrDefault() is not GenericNameSyntax genericName)
                return this.document;

            if (genericName.TypeArgumentList.Arguments.Count == 2)
            {
                var newName = genericName
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SeparatedList(new[]
                            {
                                genericName.TypeArgumentList.Arguments[0],
                                ParseTypeName(returnType)
                            })));

                return document.WithSyntaxRoot(root.ReplaceNode(baseType,
                    baseType.WithType(newName)));
            }
            else
            {
                return document.WithSyntaxRoot(root.ReplaceNode(baseType,
                    baseType.WithType(
                        genericName.AddTypeArgumentListArguments(
                            ParseTypeName(returnType)))));
            }
        }

    }
}