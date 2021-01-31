namespace ce_toy_cs.Framework.Functional
{
    public class Unit
    {
        private Unit() { }

        public static Unit Value { get; } = new Unit();

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
}
