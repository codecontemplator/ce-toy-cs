using ce_toy_cs.Framework;
using ce_toy_cs.Framework.Functional;
using ce_toy_cs.VariableTypes;
using System;
using System.Linq;

namespace ce_toy_cs
{
    using Rule = RuleExprAst<Result, RuleExprContext<Unit>>;
    using RejectPolicy = RuleExprAst<DslSpecialized.PolicyRejectType, RuleExprContext<string>>;
    using static ce_toy_cs.Framework.DslSpecialized;

    class TestProcess
    {
        private static readonly PolicyPassType policy_pass = PolicyPassType.Value;
        private static readonly PolicyRejectType policy_reject = PolicyRejectType.Value;
        private static readonly Result rule_pass = Result.Empty;

        private static Rule AbsoluteMaxAmount(int amountLimit)
        {
            return
                from amount in Dsl.GetAmount<Unit>()
                select Math.Min(amount, amountLimit).ToResult();
        }

        private static Rule MaxTotalDebt(double debtLimit)
        {
            return
               (
                    from creditA in Variables.CreditA.Value
                    from creditB in Variables.CreditB.Value
                    let totalCredit = creditA + creditB
                    where totalCredit < debtLimit
                    select policy_pass
               ).LiftPolicy();
        }

        private static Rule MinTotalSalary(int salaryLimit)
        {
            return
                from salaries in Variables.Salary.Values
                where salaries.Sum() > salaryLimit
                select rule_pass;
        }

        private static Rule PrimaryApplicantMustHaveAddress()
        {
            return
                (
                    from role in Variables.Role.Value
                    where role == Roles.Primary
                    from address in Variables.Address.Value
                    where !address.IsValid
                    select policy_reject
               ).LiftPolicy();
        }

        private static Rule CreditScoreUnderLimit(double limit)
        {
            return
               (
                    from creditScore in Variables.CreditScore.Value
                    where creditScore < limit
                    select policy_pass
               ).LiftPolicy();
        }

        private static Rule Policies(int minAge, int maxAge, int maxFlags)
        {
            return
                new RejectPolicy[]
                {
                    Variables.Age.Value.RejectPolicy     (age => age < minAge || age > maxAge, $"Age must be greater than {minAge} and less than {maxAge}"),
                    Variables.Deceased.Value.RejectPolicy(deceased => deceased,                $"Must be alive"),
                    Variables.Flags.Value.RejectPolicy   (flags => flags >= 2,                 $"Flags must be less than {maxFlags}")
                }.LiftPolicies();
        }

        public static Process GetProcess()
        {
            return
                new[]
                {
                    AbsoluteMaxAmount(100).LogContext("AbsoluteMaxAmount"),
                    //Policies(18, 100, 2).LogContext("Policies"),
                    //MaxTotalDebt(50).LogContext("MaxTotalDebt"),
                    //MinTotalSalary(50).LogContext("MinTotalSalary"),
                    //PrimaryApplicantMustHaveAddress().LogContext("PrimaryApplicantMustHaveAddress"),
                    //CreditScoreUnderLimit(0.9).LogContext("CreditScoreUnderLimit")
                }.CompileToProcess("Test process");
        }
    }
}
