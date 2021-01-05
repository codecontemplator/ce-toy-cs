using System;
using System.Linq;
using static ce_toy_cs.RuleExprTrans;
using Decision = ce_toy_cs.RuleExprAst<int, ce_toy_cs.MRuleExprContext>;

namespace ce_toy_cs
{
    class Process
    {
        private static Decision AbsoluteMaxAmount(int amountLimit)
        {
            return
                from amount in MDsl.GetAmount()
                select Math.Min(amount, amountLimit);
        }

        private static Decision MaxTotalDebt(double debtLimit)
        {
            return
               Lift(
                    from amount in SDsl.GetAmount()
                    from creditA in SDsl.GetValue<double>("CreditA")
                    from creditB in SDsl.GetValue<double>("CreditB")
                    let totalCredit = creditA + creditB
                    select totalCredit > debtLimit ? 0 : amount
               );
        }

        private static Decision MinTotalSalary(int salaryLimit)
        {
            return
                from amount in MDsl.GetAmount()
                from salaries in MDsl.GetValues<int>("Salary")
                select salaries.Sum() < salaryLimit ? 0 : amount;
        }

        public static IRule GetProcess()
        {
            return
                new RuleBuilder()
                    .Add(new AtomicRule("AbsoluteMaxAmount", AbsoluteMaxAmount(100)))
                    .Add(new AtomicRule("MaxTotalDebt", MaxTotalDebt(50)))
                    .Add(new AtomicRule("MinTotalSalary", MinTotalSalary(50)))
                    .Build();
        }
    }
}
