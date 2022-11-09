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
public class SyncToAsyncFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Diagnostics.InvalidSyncOnAsync.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var invocation = root.FindNode(context.Span).FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation == null || invocation.Expression is not MemberAccessExpressionSyntax member ||
            await context.Document.GetSemanticModelAsync(context.CancellationToken) is not SemanticModel semantic ||
            context.Diagnostics.FirstOrDefault() is not Diagnostic diagnostic ||
            !diagnostic.Properties.TryGetValue("TCommand", out var commandType) ||
            commandType is null ||
            await context.Document.Project.GetCompilationAsync(context.CancellationToken) is not Compilation compilation ||
            compilation.GetTypeByFullName(commandType) is not INamedTypeSymbol commandSymbol)
            return;

        var commandTypeName = commandSymbol.ToMinimalDisplayString(semantic, context.Span.Start);

        context.RegisterCodeFix(
            new SyncToAsyncAction(context.Document, member, commandTypeName,
                diagnostic.Properties.TryGetValue("Parameterless", out var value) &&
                bool.TryParse(value, out var parameterless) && parameterless),
            context.Diagnostics);
    }

    class SyncToAsyncAction : CodeAction
    {
        readonly Document document;
        readonly MemberAccessExpressionSyntax member;
        readonly string command;
        readonly bool parameterless;

        public SyncToAsyncAction(Document document, MemberAccessExpressionSyntax member, string command, bool parameterless)
            => (this.document, this.member, this.command, this.parameterless)
            = (document, member, command, parameterless);

        public override string Title => $"Replace Execute with ExecuteAsync";
        public override string EquivalenceKey => Title;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            if (root == null)
                return document;

            var isGeneric = member.Name is GenericNameSyntax;
            var newName = IdentifierName("ExecuteAsync");

            root = root.ReplaceNode(member, member.WithName(newName).WithAdditionalAnnotations(new SyntaxAnnotation("Member")));

            if (root.GetAnnotatedNodes("Member").FirstOrDefault() is not MemberAccessExpressionSyntax newMember)
                return document;

            var invocation = newMember.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocation == null)
                return document;

            root = root.ReplaceNode(invocation, invocation.WithAdditionalAnnotations(new SyntaxAnnotation("Invocation")));
            invocation = (InvocationExpressionSyntax)root.GetAnnotatedNodes("Invocation").FirstOrDefault();

            if (isGeneric && parameterless)
            {
                // Turn Execute<Command> to ExecuteAsync(new Command())
                root = root.ReplaceNode(invocation, invocation.WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                ObjectCreationExpression(
                                    IdentifierName(command))
                                .WithArgumentList(
                                    ArgumentList()))))));

                invocation = (InvocationExpressionSyntax)root.GetAnnotatedNodes("Invocation").First();
            }

            return document.WithSyntaxRoot(
                root.ReplaceNode(invocation, AwaitExpression(invocation.WithoutLeadingTrivia()).WithLeadingTrivia(invocation.GetLeadingTrivia())));
        }
    }
}