namespace ce_toy_cs.Framework
{
    public enum DecisionType
    {
        Accept, Reject, AcceptGivenAmount
    }

    public record Decision
    {
        public DecisionType Type { get; init; }
        public int? Amount { get; init; }

        public static Decision Accept { get; } = new Decision { Type = DecisionType.Accept };
        public static Decision Reject { get; } = new Decision { Type = DecisionType.Reject, Amount = 0 };
        public static Decision AcceptGivenAmount(int amount) => new Decision { Type = DecisionType.AcceptGivenAmount, Amount = amount };
    }
}
