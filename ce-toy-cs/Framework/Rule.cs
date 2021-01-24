using System.Collections.Immutable;

namespace ce_toy_cs.Framework
{
    public record Rule
    {
        public RuleExpr<int, MRuleExprContext> RuleExpr { get; init; }
        public IImmutableList<string> Keys { get; init; }
    }
}
