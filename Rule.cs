using System.Collections.Immutable;

namespace ce_toy_cs
{
    interface IRule
    {
        (int, RuleContext) Eval(RuleContext context);
    }

    record RuleLogEntry
    {
        public string RuleName { get; init; }
        public int AmountIn { get; init; }
        public int AmountOut { get; init; }
    }

    record RuleContext
    {
        public RuleExprContext RuleExprContext { get; init; }
        public ImmutableList<RuleLogEntry> Log { get; init; }
    }

    class AtomicRule : IRule
    {
        public AtomicRule(string name, RuleExpr<int> expr)
        {
            Name = name;
            Expr = expr;
        }

        public string Name { get; }
        public RuleExpr<int> Expr { get; }

        public (int, RuleContext) Eval(RuleContext context)
        {
            var (amount,ruleExprContext) = Expr(context.RuleExprContext);
            return
                (
                    amount,
                    context with {
                        Log = context.Log.Add(new RuleLogEntry { RuleName = Name, AmountIn = context.RuleExprContext.Amount, AmountOut = amount }),
                        RuleExprContext = ruleExprContext with { Amount = amount }
                    }
                );
        }
    }

    class AndThenRule : IRule
    {
        private readonly IRule r1;
        private readonly IRule r2;

        public AndThenRule(IRule r1, IRule r2)
        {
            this.r1 = r1;
            this.r2 = r2;
        }

        public (int, RuleContext) Eval(RuleContext context)
        {
            var (a, context2) = r1.Eval(context);
            var context3 = context2 with { RuleExprContext = context2.RuleExprContext with { Amount = a } };
            return r2.Eval(context3);
        }
    }

    class RuleBuilder
    {
        private IRule _current;

        public RuleBuilder()
        {

        }

        public RuleBuilder Add(IRule rule)
        {
            if (_current == null)
                _current = rule;
            else
                _current = new AndThenRule(_current, rule);

            return this;
        }

        public IRule Build() => _current;
    }

}