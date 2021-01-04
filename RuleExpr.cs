using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    public delegate (T, RuleExprContext) RuleExpr<T, RuleExprContext>(RuleExprContext input);

    public record Applicant
    {
        public IEnumerable<ILoader> Loaders { get; init; }
        public ImmutableDictionary<string, object> KeyValueMap { get; init; }
    }

    public interface IRuleExprContext
    {
        public int Amount { get; }
    }

    public record MRuleExprContext : IRuleExprContext
    {
        public int Amount { get; init; }
        public ImmutableDictionary<string, Applicant> Applicants { get; init; }
    }

    public record RuleExprAst<T, RuleExprContext>
    {
        public Expression<RuleExpr<T, RuleExprContext>> Expression { get; init; }
        public RuleExpr<T, RuleExprContext> Compile() => Expression.Compile();
        public IEnumerable<string> GetKeys()
        {
            var findKeysVisitor = new FindKeysVisistor();
            findKeysVisitor.Visit(Expression);
            return findKeysVisitor.FoundKeys;
        }
    }
}