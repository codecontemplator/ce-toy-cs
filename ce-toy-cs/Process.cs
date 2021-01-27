using ce_toy_cs.Framework;
using ce_toy_cs.Framework.Details;
using ce_toy_cs.VariableTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ce_toy_cs
{
    //using Convert = Framework.Convert;
    using RuleExprAst = RuleExprAst<IRuleExprContextApplicable, MRuleExprContext>;
    using static Return;

    class Process
    {
        private static RuleExprAst AbsoluteMaxAmount(int amountLimit)
        {
            return
                from amount in MDsl.GetAmount()
                select Amount(Math.Min(amount, amountLimit));
        }

        private static RuleExprAst MaxTotalDebt(double debtLimit)
        {
            return
               (
                    from creditA in Variables.CreditA.Value
                    from creditB in Variables.CreditB.Value
                    let totalCredit = creditA + creditB
                    where totalCredit > debtLimit
                    select Reject()
               ).Lift();
        }

        private static RuleExprAst MinTotalSalary(int salaryLimit)
        {
            return
                from salaries in Variables.Salary.Values
                where salaries.Sum() < salaryLimit
                select Reject();
        }

        private static RuleExprAst PrimaryApplicantMustHaveAddress()
        {
            return
                (
                    from role in Variables.Role.Value
                    where role == Roles.Primary
                    from address in Variables.Address.Value
                    where !address.IsValid
                    select Reject()
               ).Lift();
        }

        private static RuleExprAst CreditScoreUnderLimit(double limit)
        {
            return
               (
                    from creditScore in Variables.CreditScore.Value
                    where creditScore > limit
                    select Reject()
               ).Lift();
        }

        private static RuleExprAst<Unit, MRuleExprContext> Policies(int minAge, int maxAge, int maxFlags)
        {
            return
                new []
                {
                    Variables.Age.Value.RejectIf(age => age < minAge || age > maxAge, $"Age must be greater than {minAge} and less than {maxAge}"),
                    Variables.Deceased.Value.RejectIf(deceased => deceased, $"Must be alive"),
                    Variables.Flags.Value.RejectIf(flags => flags >= 2, $"Flags must be less than {maxFlags}")
                }.JoinMany().Lift();
        }

        public static Rule GetProcess()
        {
            throw new NotImplementedException();
            //return
            //    new[]
            //    {
            //        Convert.ToRuleExprAst(() => AbsoluteMaxAmount(100)),
            //        Convert.ToRuleExprAst(() => Policies(18, 100, 2)),
            //        Convert.ToRuleExprAst(() => MaxTotalDebt(50)),
            //        Convert.ToRuleExprAst(() => MinTotalSalary(50)),
            //        Convert.ToRuleExprAst(() => PrimaryApplicantMustHaveAddress()),
            //        Convert.ToRuleExprAst(() => CreditScoreUnderLimit(0.8))
            //    }.JoinMany().CompileToRule();
        }
    }
}
