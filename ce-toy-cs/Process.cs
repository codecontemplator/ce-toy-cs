using ce_toy_cs.Framework;
using ce_toy_cs.Framework.Functional;
using ce_toy_cs.VariableTypes;
using System;
using System.Linq;

namespace ce_toy_cs
{
    using RuleDef = RuleExprAst<Unit, RuleExprContext<Unit>>;

    class Process
    {
        private static readonly PassUnit passed = PassUnit.Value;

        private static RuleDef AbsoluteMaxAmount(int amountLimit)
        {
            return
                (
                    from amount in Dsl.GetAmount<Unit>()
                    select new Amount(Math.Min(amount, amountLimit))
                ).Apply();
        }

        private static RuleDef MaxTotalDebt(double debtLimit)
        {
            return
               (
                    from creditA in Variables.CreditA.Value
                    from creditB in Variables.CreditB.Value
                    let totalCredit = creditA + creditB
                    where totalCredit < debtLimit
                    select passed
               ).Lift().Apply();// (AllShouldPass).Select(_ => Result.Empty);
        }

        private static RuleDef MinTotalSalary(int salaryLimit)
        {
            return
                (
                    from salaries in Variables.Salary.Values
                    where salaries.Sum() > salaryLimit
                    select passed
                ).Apply();
        }

        private static RuleDef PrimaryApplicantMustHaveAddress()
        {
            return
                (
                    from role in Variables.Role.Value
                    where role == Roles.Primary
                    from address in Variables.Address.Value
                    where !address.IsValid
                    select passed
               ).Lift().Apply();
        }

        private static RuleDef CreditScoreUnderLimit(double limit)
        {
            return
               (
                    from creditScore in Variables.CreditScore.Value
                    where creditScore < limit
                    select passed
               ).Lift().Apply();
        }

        //private static RuleDef Policies(int minAge, int maxAge, int maxFlags)
        //{
        //    return
        //        new RuleExprAst<Unit, RuleExprContext<string>>[]
        //        {
        //            Variables.Age.Value.RejectIf     (age => age < minAge || age > maxAge, $"Age must be greater than {minAge} and less than {maxAge}"),
        //            Variables.Deceased.Value.RejectIf(deceased => deceased,                $"Must be alive"),
        //            Variables.Flags.Value.RejectIf   (flags => flags >= 2,                 $"Flags must be less than {maxFlags}")
        //        }.Join().Lift();
        //}

        public static Rule GetProcess()
        {
            return
                new[]
                {
                    AbsoluteMaxAmount(100).LogContext("AbsoluteMaxAmount"),
                    //Policies(18, 100, 2).LogContext("Policies"),
                    MaxTotalDebt(50).LogContext("MaxTotalDebt"),
                    MinTotalSalary(50).LogContext("MinTotalSalary"),
                    PrimaryApplicantMustHaveAddress().LogContext("PrimaryApplicantMustHaveAddress"),
                    CreditScoreUnderLimit(0.9).LogContext("CreditScoreUnderLimit")
                }.Join().CompileToRule();
        }
    }
}
