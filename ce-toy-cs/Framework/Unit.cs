namespace ce_toy_cs.Framework.Functional
{
    public class Unit : IRuleContextApplicable
    {
        private Unit() { }

        public static Unit Value { get; } = new Unit();

        public RuleContext ApplyTo<RuleContext>(RuleContext ctx) where RuleContext : RuleExprContextBase
        {
            return ctx;
        }

        public override bool Equals(object obj)
        {
            return obj is Unit;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "()";
        }
    }

    public class FailUnit : IRuleContextApplicable
    {
        private FailUnit() { }

        public static FailUnit Value { get; } = new FailUnit();

        public override bool Equals(object obj)
        {
            return obj is FailUnit;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "(<fail>)";
        }

        public RuleContext ApplyTo<RuleContext>(RuleContext ctx) where RuleContext : RuleExprContextBase
        {
            return ctx;
        }
    }

    public class PassUnit : IRuleContextApplicable
    {
        private PassUnit() { }

        public static PassUnit Value { get; } = new PassUnit();

        public override bool Equals(object obj)
        {
            return obj is PassUnit;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "(<pass>)";
        }

        public RuleContext ApplyTo<RuleContext>(RuleContext ctx) where RuleContext : RuleExprContextBase
        {
            return ctx;
        }
    }
}
