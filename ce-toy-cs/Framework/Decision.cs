using System;
using System.Collections.Generic;
using System.Linq;

namespace ce_toy_cs.Framework
{
    public enum DecisionType
    {
        Accept, Reject
    }
    /*

    public record Decision
    {
        public DecisionType? Type { get; init; }
        public int? Amount { get; init; }
        public decimal? Interest { get; init; }
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
                case DecisionType.AcceptLoweredAmount:
                    switch(y.Type)
                    {
                        case DecisionType.Reject:
                            return y;
                        case DecisionType.Accept:
                            return x;
                        case DecisionType.AcceptLoweredAmount:
                            return Decision.AcceptLoweredAmount(Math.Min(x.Amount.Value, y.Amount.Value));
                    }
                    break;
            }

            throw new Exception("Unknown decision type");
        }

        public static Decision Min(this IEnumerable<Decision> decisions)
        {
            return decisions.Aggregate(Decision.Accept, Min);
        }

        public static int GetGrantedAmount(this (Option<Decision>, MRuleExprContext) result)
        {
            if (result.Item1.IsSome(out var decision))
            {
                switch (decision.Type)
                {
                    case DecisionType.Accept:
                        return result.Item2.Amount;
                    case DecisionType.Reject:
                        return 0;
                    case DecisionType.AcceptLoweredAmount:
                        return decision.Amount.Value;
                    default:
                        throw new Exception("Unknown decision type");
                }
            }
            else
            {
                return result.Item2.Amount;
            }
        }
    }
    */
}
