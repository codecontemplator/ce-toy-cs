using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    class Process
    {
        private static RuleExprAst<int> AbsoluteMaxAmount(int amountLimit)
        {
            return
                from amount in Dsl.GetAmount()
                select Math.Min(amount, amountLimit);
        }

        //private static RuleExprAst<int> MaxTotalDebt(int debtLimit)
        //{
        //    return
        //        from amount in Dsl.GetAmount()
        //        from creditA in Dsl.GetValues("CreditA")
        //        from creditB in Dsl.GetValues("CreditB")
        //        let totalCredit = creditA + creditB
        //        select totalCredit > debtLimit ? 0 : amount;
        //}

        private static RuleExprAst<int> MinTotalSalary(int salaryLimit)
        {
            return
                from amount in Dsl.GetAmount()
                from salaries in Dsl.GetValues("Salary")
                select salaries.Sum() < salaryLimit ? 0 : amount;
        }

        public static IRule GetProcess()
        {
            return
                new RuleBuilder()
                    .Add(new AtomicRule("AbsoluteMaxAmount", AbsoluteMaxAmount(100)))
//                    .Add(new AtomicRule("MaxTotalDebt", MaxTotalDebt(50)))
                    .Add(new AtomicRule("MinTotalSalary", MinTotalSalary(50)))
                    .Build();
        }
    }
}
