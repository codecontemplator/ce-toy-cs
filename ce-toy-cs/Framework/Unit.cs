namespace ce_toy_cs.Framework
{
    public class Unit : IRuleExprContextApplicable
    {
        private Unit()
        {

        }

        public static Unit Value { get; } = new Unit();

        public (Option<Unit>, IRuleExprContext) ApplyTo(IRuleExprContext ctx)
        {
            return (Option<Unit>.Some(this), ctx);
        }

        public override bool Equals(object obj)
        {
            return obj is Unit;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    public class Reject : IRuleExprContextApplicable
    {
        private Reject()
        {

        }

        public static Reject Value { get; } = new Reject();

        public (Option<Unit>, IRuleExprContext) ApplyTo(IRuleExprContext ctx)
        {
            return (Option<Unit>.None, ctx);
        }

        public override bool Equals(object obj)
        {
            return obj is Reject;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}
