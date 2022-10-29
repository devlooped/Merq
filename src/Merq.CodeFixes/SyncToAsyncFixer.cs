using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
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
public class SyncToAsyncFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Diagnostics.SyncOnAsyncRule.Id);

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
            new SyncToAsyncAction(context.Document, root, member),
            context.Diagnostics);
    }

    class SyncToAsyncAction : CodeAction
    {
        readonly Document document;
        readonly SyntaxNode root;
        readonly MemberAccessExpressionSyntax member;

        public SyncToAsyncAction(Document document, SyntaxNode root, MemberAccessExpressionSyntax member)
            => (this.document, this.root, this.member)
            = (document, root, member);

        public override string Title => $"Replace Execute with ExecuteAsync";
        public override string EquivalenceKey => Title;

        protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            SimpleNameSyntax newName = member.Name is GenericNameSyntax generic ?
                generic.WithIdentifier(SyntaxFactory.Identifier("ExecuteAsync")) :
                SyntaxFactory.IdentifierName("Execute");

            var asyncMember = member.WithName(newName);
            var newRoot = root.ReplaceNode(member, asyncMember);
            var newMember = newRoot.FindNode(new TextSpan(member.SpanStart, asyncMember.Span.Length));
            var invocation = newMember.FirstAncestorOrSelf<InvocationExpressionSyntax>();

            if (invocation != null)
            {
                return Task.FromResult(document.WithSyntaxRoot(
                    newRoot.ReplaceNode(invocation, SyntaxFactory.AwaitExpression(invocation.WithoutLeadingTrivia()).WithLeadingTrivia(invocation.GetLeadingTrivia()))));
            }

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}