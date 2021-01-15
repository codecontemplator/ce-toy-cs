using System;
using System.Linq;
using static ce_toy_cs.RuleExprTrans;
using Decision = ce_toy_cs.RuleExprAst<int, ce_toy_cs.MRuleExprContext>;

namespace ce_toy_cs
{
    class Process
    {
        private const int reject = 0;

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
                    from creditA in SDsl.GetValue<double>("CreditA")
                    from creditB in SDsl.GetValue<double>("CreditB")
                    let totalCredit = creditA + creditB
                    where totalCredit > debtLimit
                    select reject
               );
        }

        private static Decision MinTotalSalary(int salaryLimit)
        {
            return
                from salaries in MDsl.GetValues<int>("Salary")
                where salaries.Sum() < salaryLimit
                select reject;
        }

        private static Decision PrimaryApplicantMustHaveAddress()
        {
            return
                Lift(
                    from role in SDsl.GetValue<string>("Role")
                    where role == "Primary"
                    from address in SDsl.GetValue<string>("Address")
                    where string.IsNullOrEmpty(address)
                    select reject
               );
        }

        private static Decision CreditScoreUnderLimit(double limit)
        {
            return
                Lift(
                    from creditScore in SDsl.GetValue<double>("CreditScore")
                    where creditScore > limit
                    select reject
               );
        }

        public static IRule GetProcess()
        {
            return
                new RuleBuilder()
                    .Add(() => AbsoluteMaxAmount(110))
                    .Add(() => MaxTotalDebt(50))
                    .Add(() => MinTotalSalary(50))
                    .Add(() => PrimaryApplicantMustHaveAddress())
                    .Add(() => CreditScoreUnderLimit(0.8))
                    .Build();
        }
    }
}
