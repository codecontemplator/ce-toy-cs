namespace ce_toy_cs.Framework.Functional
{
    public interface IUnit { }

    public record Unit : IUnit
    {
        private Unit() { }

        public static Unit Value { get; } = new Unit();

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "()";
        }
    }
}
