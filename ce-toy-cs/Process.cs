using System;
using System.Linq;

namespace ce_toy_cs
{
    using RuleExprAst = RuleExprAst<int, MRuleExprContext>;

    class Process
    {
        private const int reject = 0;

        private static RuleExprAst AbsoluteMaxAmount(int amountLimit)
        {
            return
                from amount in MDsl.GetAmount()
                select Math.Min(amount, amountLimit);
        }

        private static RuleExprAst MaxTotalDebt(double debtLimit)
        {
            return
               (
                    from creditA in SDsl.GetValue<double>("CreditA")
                    from creditB in SDsl.GetValue<double>("CreditB")
                    let totalCredit = creditA + creditB
                    where totalCredit > debtLimit
                    select reject
               ).Lift();
        }

        private static RuleExprAst MinTotalSalary(int salaryLimit)
        {
            return
                from salaries in MDsl.GetValues<int>("Salary")
                where salaries.Sum() < salaryLimit
                select reject;
        }

        private static RuleExprAst PrimaryApplicantMustHaveAddress()
        {
            return
                (
                    from role in SDsl.GetValue<string>("Role")
                    where role == "Primary"
                    from address in SDsl.GetValue<string>("Address")
                    where string.IsNullOrEmpty(address)
                    select reject
               ).Lift();
        }

        private static RuleExprAst CreditScoreUnderLimit(double limit)
        {
            return
               (
                    from creditScore in SDsl.GetValue<double>("CreditScore")
                    where creditScore > limit
                    select reject
               ).Lift();
        }

        private static RuleExprAst Policies(int minAge, int maxAge, int maxRemarks)
        {
            return
                 SDsl.GetValue<int> ("Age")      .Where(age => age < minAge || age > maxAge)  .Select(_ => reject).WithLogging($"Age must be greater than {minAge} and less than {maxAge}").AndThen(
                 SDsl.GetValue<bool>("Deceased") .Where(deceased => deceased)                 .Select(_ => reject).WithLogging($"Must be alive")).AndThen(
                 SDsl.GetValue<int> ("Flags")    .Where(flags => flags >= 2)                  .Select(_ => reject).WithLogging($"Flags must be less than {maxRemarks}")).Lift();
        }

        public static Rule GetProcess()
        {
            return
                new RuleBuilder()
                    .Add(() => AbsoluteMaxAmount(100))
                    .Add(() => Policies(18, 100, 2))
                    .Add(() => MaxTotalDebt(50))
                    .Add(() => MinTotalSalary(50))
                    .Add(() => PrimaryApplicantMustHaveAddress())
                    .Add(() => CreditScoreUnderLimit(0.8))
                    .Build();
        }
    }
}
