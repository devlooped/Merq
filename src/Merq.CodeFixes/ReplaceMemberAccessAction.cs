using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Merq;

class ReplaceMemberAccessAction : CodeAction
{
    readonly Document document;
    readonly SyntaxNode root;
    readonly MemberAccessExpressionSyntax member;
    readonly string newMemberName;

    public ReplaceMemberAccessAction(Document document, SyntaxNode root, MemberAccessExpressionSyntax member, string newMemberName)
        => (this.document, this.root, this.member, this.newMemberName)
        = (document, root, member, newMemberName);

    public override string Title => $"Replace {member.Name} with {newMemberName}";
    public override string EquivalenceKey => Title;

    protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        => Task.FromResult(document.WithSyntaxRoot(
            root.ReplaceNode(member, member.WithName(SyntaxFactory.IdentifierName(newMemberName)))));
}