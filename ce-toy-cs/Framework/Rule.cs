using ce_toy_cs.Framework.Functional;
using System.Collections.Immutable;

namespace ce_toy_cs.Framework
{
    public record Rule
    {
        public RuleExpr<Unit, RuleExprContext<Unit>> RuleExpr { get; init; }
        public IImmutableList<string> Keys { get; init; }
    }

    public static class RuleExtensions
    {
        public static Rule CompileToRule(this RuleExprAst<Unit, RuleExprContext<Unit>> ruleAst)
        {
            return new Rule
            {
                Keys = ruleAst.GetKeys().ToImmutableList(),
                RuleExpr = ruleAst.Compile()
            };
        }
    }
}
