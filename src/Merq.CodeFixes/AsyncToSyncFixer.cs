using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Merq.CodeFixes;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class AsyncToSyncFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Diagnostics.AsyncOnSyncRule.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var invocation = root.FindNode(context.Span).FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation == null || invocation.Expression is not MemberAccessExpressionSyntax member)
            return;

        context.RegisterCodeFix(
            new AsyncToSyncAction(context.Document, root, member),
            context.Diagnostics);
    }

    class AsyncToSyncAction : CodeAction
    {
        readonly Document document;
        readonly SyntaxNode root;
        readonly MemberAccessExpressionSyntax member;

        public AsyncToSyncAction(Document document, SyntaxNode root, MemberAccessExpressionSyntax member)
        => (this.document, this.root, this.member)
        = (document, root, member);

        public override string Title => $"Replace ExecuteAsync with Execute";
        public override string EquivalenceKey => Title;

        protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            SimpleNameSyntax newName = member.Name is GenericNameSyntax generic ?
                generic.WithIdentifier(SyntaxFactory.Identifier("Execute")) :
                SyntaxFactory.IdentifierName("Execute");

            var syncMember = member.WithName(newName);
            var newRoot = root.ReplaceNode(member, syncMember);
            var newMember = newRoot.FindNode(new TextSpan(member.SpanStart, syncMember.Span.Length));
            var awaited = newMember.FirstAncestorOrSelf<AwaitExpressionSyntax>();

            if (awaited != null)
            {
                return Task.FromResult(document.WithSyntaxRoot(
                    newRoot.ReplaceNode(awaited, awaited.Expression.WithLeadingTrivia(awaited.GetLeadingTrivia()))));
            }

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}