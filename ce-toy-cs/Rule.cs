using System.Collections.Immutable;

namespace ce_toy_cs
{
    public record Rule
    {
        public RuleExpr<int, MRuleExprContext> RuleExpr { get; init; }
        public IImmutableList<string> Keys { get; init; }
    }

    class RuleBuilder
    {
        private RuleExprAst<int, MRuleExprContext> _current;

        public RuleBuilder()
        {
        }

        public RuleBuilder Add(params RuleExprAst<int, MRuleExprContext>[] ruleExprAsts)
        {
            var i = 0;
            if (_current == null)
                _current = ruleExprAsts[i++]; 
            for (; i < ruleExprAsts.Length; ++i)
                _current = _current.AndThen(ruleExprAsts[i]);
            return this;
        }

        public Rule Build()
        {
            return new Rule
            {
                RuleExpr = _current.Compile(),
                Keys = ImmutableList.ToImmutableList(_current.GetKeys())
            };
        }
    }
}
