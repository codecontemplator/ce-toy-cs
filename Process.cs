using System;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    class Process
    {
        public static Expression<RuleExpr<int>> AbsoluteMaxAmount(int amountLimit)
        {
            return
                from amount in Dsl.GetAmount()
                select Math.Min(amount, amountLimit);
        }

        public static Expression<RuleExpr<int>> MaxTotalDebt(int debtLimit)
        {
            return
                from amount in Dsl.GetAmount()
                from creditA in Dsl.GetValue("CreditA")
                //from creditB in Dsl.GetValue("CreditB")
                let totalCredit = creditA + creditB
                select creditA > debtLimit ? 0 : amount;
        }

        //public static IRule GetProcess()
        //{
        //    return
        //        new RuleBuilder()
        //            .Add(new AtomicRule("AbsoluteMaxAmount", AbsoluteMaxAmount(100)))
        //            .Add(new AtomicRule("MaxTotalDebt", MaxTotalDebt(50)))
        //            .Build();
        //}
    }
}
