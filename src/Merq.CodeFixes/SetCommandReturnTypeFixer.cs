using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
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
            !diagnostic.Properties.TryGetValue("CommandType", out var commandType) ||
            commandType is null ||
            !diagnostic.Properties.TryGetValue("ReturnType", out var returnType) ||
            returnType is null ||
            await context.Document.GetSemanticModelAsync(context.CancellationToken) is not SemanticModel semantic ||
            await context.Document.Project.GetCompilationAsync(context.CancellationToken) is not Compilation compilation ||
            compilation.GetTypeByFullName(commandType) is not INamedTypeSymbol commandSymbol ||
            compilation.GetTypeByFullName(returnType) is not INamedTypeSymbol returnSymbol)
            return;

        context.RegisterCodeFix(new SetCommandReturnTypeAction(
                compilation,
                context.Document,
                root, typeSyntax,
                commandSymbol,
                returnSymbol.ToMinimalDisplayString(semantic, context.Span.Start)),
            context.Diagnostics);
    }

    class SetCommandReturnTypeAction : CodeAction
    {
        readonly Document document;
        readonly SyntaxNode root;
        readonly SimpleBaseTypeSyntax baseType;
        readonly INamedTypeSymbol commandSymbol;
        readonly string returnType;

        public SetCommandReturnTypeAction(Compilation compilation, Document document, SyntaxNode root, SimpleBaseTypeSyntax typeSyntax, INamedTypeSymbol commandSymbol, string returnType)
            => (this.document, this.root, baseType, this.commandSymbol, this.returnType)
            = (document, root, typeSyntax, commandSymbol, returnType);

        public override string Title => $"Specify command return type {returnType}";
        public override string EquivalenceKey => Title;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var baseType = this.baseType;
            if (baseType.Type is not GenericNameSyntax genericName ||
                await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel semantic)
                return document;

            var root = new MethodReturnTypeRewriter(semantic, commandSymbol, returnType).Visit(this.root);

            // If we cannot locate again the baseType, then 
            // we backtrack, since it's more important to fix the interface than the method.
            if (root.FindNode(genericName.Span) is not SimpleBaseTypeSyntax newBaseType ||
                newBaseType.Type is not GenericNameSyntax newGenericName)
                root = this.root;
            else
            {
                genericName = newGenericName;
                baseType = newBaseType;
            }

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

        class MethodReturnTypeRewriter : CSharpSyntaxRewriter
        {
            readonly SemanticModel semantic;
            readonly INamedTypeSymbol commandSymbol;
            readonly string returnType;

            public MethodReturnTypeRewriter(SemanticModel semantic, INamedTypeSymbol commandSymbol, string returnType)
                => (this.semantic, this.commandSymbol, this.returnType)
                = (semantic, commandSymbol, returnType);

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.ParameterList.Parameters.Count < 1 ||
                    node.ParameterList.Parameters.Count > 2)
                    return node;

                if (node.ParameterList.Parameters[0].Type is not TypeSyntax parameterSyntax ||
                    semantic.GetSymbolInfo(parameterSyntax).Symbol is not ITypeSymbol parameterType ||
                    !parameterType.Equals(commandSymbol, SymbolEqualityComparer.Default))
                    return node;

                if (node.Identifier.ToString() == "Execute")
                {
                    return node.WithReturnType(ParseTypeName(returnType).WithTrailingTrivia(Space));
                }
                else if (node.Identifier.ToString() == "ExecuteAsync")
                {
                    if (node.ReturnType is GenericNameSyntax generic)
                        return node.WithReturnType(generic
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SeparatedList(new[] { ParseTypeName(returnType) }))));
                    else
                        return node.WithReturnType(
                            GenericName(Identifier("Task"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SeparatedList(new[] { ParseTypeName(returnType) }))));
                }

                return node;
            }
        }
    }
}