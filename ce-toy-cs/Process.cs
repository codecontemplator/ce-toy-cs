using System;
using System.Linq;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    using RuleExprAst = RuleExprAst<int, MRuleExprContext>;
    using SRuleExprAst = RuleExprAst<int, SRuleExprContext>;

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
            SRuleExprAst Policy<T>(string varName, Expression<Func<T,bool>> predicate, string message) =>
                SDsl.GetValue<T>(varName).Where(predicate).Select(_ => reject).WithLogging(message);

            return
                Policy<int> ("Age",      age => age < minAge || age > maxAge, $"Age must be greater than {minAge} and less than {maxAge}" ).AndThen(
                Policy<bool>("Deceased", deceased => deceased,                $"Must be alive"                                           )).AndThen(
                Policy<int> ("Flags",    flags => flags >= 2,                 $"Flags must be less than {maxRemarks}"                    )).Lift();
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
