using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerator;

public static class ConsoleWriteLineCreator
{
    public static ExpressionStatementSyntax CreateConsoleWriteLine(string argumentStr)
    {
        var console = SyntaxFactory.IdentifierName("Console");
        var writeLine = SyntaxFactory.IdentifierName("WriteLine");
        var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, console, writeLine);

        var argument = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(argumentStr)));
        var argumentList = SyntaxFactory.SeparatedList(new[] { argument });

        return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(memberAccess,
                    SyntaxFactory.ArgumentList(argumentList)));
    }
}