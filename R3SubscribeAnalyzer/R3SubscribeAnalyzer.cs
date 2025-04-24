using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace R3SubscribeAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class R3SubscribeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "R3SUB001";
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Subscribe without disposal",
        "Subscribe result is not disposed or AddTo-ed",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name.Identifier.Text != "Subscribe" && 
            memberAccess.Name.Identifier.Text != "SubscribeAwait")
            return;

        var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol as IMethodSymbol;
        if (symbol == null || (symbol.Name != "Subscribe" && symbol.Name != "SubscribeAwait"))
            return;

        if (!ImplementsIDisposable(symbol.ReturnType))
            return;

        // OK if it's part of a variable assignment or declaration
        if (invocation.Parent is not ExpressionStatementSyntax &&
            invocation.Parent is not MemberAccessExpressionSyntax)
            return;

        // OK if it's inside a using statement
        if (IsInsideUsing(invocation))
            return;

        // Get the topmost expression in the chain
        var topMostExpression = invocation;
        while (topMostExpression.Parent is ExpressionStatementSyntax ||
               (topMostExpression.Parent is InvocationExpressionSyntax parentInvoke &&
                parentInvoke.Expression is MemberAccessExpressionSyntax))
        {
            if (topMostExpression.Parent is ExpressionStatementSyntax)
                break;
            topMostExpression = (InvocationExpressionSyntax)topMostExpression.Parent;
        }

        // Check if AddTo exists in the chain
        if (!IsChainedWith(invocation, "AddTo"))
        {
            var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsInsideUsing(SyntaxNode node)
    {
        var current = node;
        while (current != null)
        {
            if (current is UsingStatementSyntax)
                return true;
            current = current.Parent;
        }
        return false;
    }

    private static bool ImplementsIDisposable(ITypeSymbol type)
    {
        if (type == null)
        {
            return false;
        }

        if (type.SpecialType == SpecialType.System_IDisposable)
        {
            return true;
        }

        return type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_IDisposable);
    }

    private static bool IsChainedWith(SyntaxNode node, string methodName)
    {
        var current = node;
        while (current?.Parent != null)
        {
            if (current.Parent is InvocationExpressionSyntax parentInvoke &&
                parentInvoke.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == methodName)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }
}
