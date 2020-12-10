using System;
using System.Collections.Immutable;

namespace ce_toy_cs
{
    class Process
    {
        public static RuleExpr<int> AbsoluteMaxAmount(int amountLimit)
        {
            return
                from amount in Dsl.GetAmount()
                select Math.Min(amount, amountLimit);
        }

        public static RuleExpr<int> MaxTotalDebt(int debtLimit)
        {
            return
                from creditA in Dsl.GetValue("CreditA")
                from creditB in Dsl.GetValue("CreditB")
                from amount in Dsl.GetAmount()
                let totalCredit = creditA + creditB
                select totalCredit > debtLimit ? 0 : amount;
        }

        public static IRule GetProcess()
        {
            return
                new RuleBuilder()
                    .Add(new AtomicRule("AbsoluteMaxAmount", AbsoluteMaxAmount(100)))
                    .Add(new AtomicRule("MaxTotalDebt", MaxTotalDebt(50)))
                    .Build();
        }
    }
}
