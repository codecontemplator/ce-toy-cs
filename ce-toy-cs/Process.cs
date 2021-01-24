using ce_toy_cs.Framework;
using ce_toy_cs.VariableTypes;
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
                    from creditA in Variables.CreditA.Value
                    from creditB in Variables.CreditB.Value
                    let totalCredit = creditA + creditB
                    where totalCredit > debtLimit
                    select reject
               ).Lift();
        }

        private static RuleExprAst MinTotalSalary(int salaryLimit)
        {
            return
                from salaries in Variables.Salary.Values
                where salaries.Sum() < salaryLimit
                select reject;
        }

        private static RuleExprAst PrimaryApplicantMustHaveAddress()
        {
            return
                (
                    from role in Variables.Role.Value
                    where role == Roles.Primary
                    from address in Variables.Address.Value
                    where !address.IsValid
                    select reject
               ).Lift();
        }

        private static RuleExprAst CreditScoreUnderLimit(double limit)
        {
            return
               (
                    from creditScore in Variables.CreditScore.Value
                    where creditScore > limit
                    select reject
               ).Lift();
        }

        private static RuleExprAst Policies(int minAge, int maxAge, int maxFlags)
        {
            SRuleExprAst Policy<T>(Variable<T> variable, Expression<Func<T,bool>> predicate, string message) =>
                variable.Value.Where(predicate).Select(_ => reject).WithLogging(message);

            return
                new []
                {
                    Policy(Variables.Age,      age => age < minAge || age > maxAge, $"Age must be greater than {minAge} and less than {maxAge}"),
                    Policy(Variables.Deceased, deceased => deceased,                $"Must be alive"                                           ),
                    Policy(Variables.Flags,    flags => flags >= 2,                 $"Flags must be less than {maxFlags}"                      )
                }.Join().Lift();
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
