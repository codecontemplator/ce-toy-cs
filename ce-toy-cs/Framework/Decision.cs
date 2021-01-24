using System;
using System.Collections.Generic;
using System.Linq;

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

    public static class DecisionExtensions
    {
        public static Decision Min(Decision x, Decision y)
        {
            switch(x.Type)
            {
                case DecisionType.Reject:
                    return x;
                case DecisionType.Accept:
                    return y;
                case DecisionType.AcceptGivenAmount:
                    switch(y.Type)
                    {
                        case DecisionType.Reject:
                            return y;
                        case DecisionType.Accept:
                            return x;
                        case DecisionType.AcceptGivenAmount:
                            return Decision.AcceptGivenAmount(Math.Min(x.Amount.Value, y.Amount.Value));
                    }
                    break;
            }

            throw new Exception("Unknown decision type");
        }

        public static Decision Min(this IEnumerable<Decision> decisions)
        {
            return decisions.Aggregate(Decision.Accept, Min);
        }
    }
}
