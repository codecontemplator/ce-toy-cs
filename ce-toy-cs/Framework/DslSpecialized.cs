namespace ce_toy_cs.Framework
{
    public static class DslSpecialized
    {
        public class PolicyRejectType
        {
            private PolicyRejectType() { }

            public static PolicyRejectType Value { get; } = new PolicyRejectType();

            public override int GetHashCode()
            {
                return 0;
            }

            public override string ToString()
            {
                return "()";
            }

            public RuleExprContext<SelectorType> Apply<SelectorType>(RuleExprContext<SelectorType> ctx)
            {
                return ctx;
            }
        }

        public class PolicyPassType
        {
            private PolicyPassType() { }

            public static PolicyPassType Value { get; } = new PolicyPassType();

            public override int GetHashCode()
            {
                return 0;
            }

            public override string ToString()
            {
                return "()";
            }

            public RuleExprContext<SelectorType> Apply<SelectorType>(RuleExprContext<SelectorType> ctx)
            {
                return ctx;
            }
        }
    }
}
