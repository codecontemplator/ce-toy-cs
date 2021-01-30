using ce_toy_cs.Framework;
using ce_toy_cs.Framework.Functional;
using ce_toy_cs.VariableTypes;
using System;
using System.Linq;

namespace ce_toy_cs
{
    //using RuleExprAst = RuleExprAst<Decision, MRuleExprContext>;
    using static VoteMethods;

    class Process
    {
        private static RuleExprAst<Result, RuleExprContext<Unit>> AbsoluteMaxAmount(int amountLimit)
        {
            return
                from amount in Dsl.GetAmount<Unit>()
                select Result.NewAmount(Math.Min(amount, amountLimit));
        }

        private static RuleExprAst<Result, RuleExprContext<Unit>> MaxTotalDebt(double debtLimit)
        {
            return
               (
                    from creditA in Variables.CreditA.Value
                    from creditB in Variables.CreditB.Value
                    let totalCredit = creditA + creditB
                    where totalCredit < debtLimit
                    select Unit.Value
               ).Lift(FailIfAnyIncomplete).Select(_ => Result.Empty);
        }

        private static RuleExprAst<Result, RuleExprContext<Unit>> MinTotalSalary(int salaryLimit)
        {
            return
                from salaries in Variables.Salary.Values
                where salaries.Sum() > salaryLimit
                select Result.Empty;
        }

        private static RuleExprAst<Result, RuleExprContext<Unit>> PrimaryApplicantMustHaveAddress()
        {
            return
                (
                    from role in Variables.Role.Value
                    where role == Roles.Primary
                    from address in Variables.Address.Value
                    where !address.IsValid
                    select Unit.Value
               ).Lift(FailIfAnyComplete).Select(_ => Result.Empty);
        }

        private static RuleExprAst<Result, RuleExprContext<Unit>> CreditScoreUnderLimit(double limit)
        {
            return
               (
                    from creditScore in Variables.CreditScore.Value
                    where creditScore < limit
                    select Unit.Value
               ).Lift(FailIfAnyIncomplete).Select(_ => Result.Empty);
        }

        private static RuleExprAst<Result, RuleExprContext<Unit>> Policies(int minAge, int maxAge, int maxFlags)
        {
            return
                new RuleExprAst<Result, RuleExprContext<string>>[]
                {
                    Variables.Age.Value.RejectIf     (age => age < minAge || age > maxAge, $"Age must be greater than {minAge} and less than {maxAge}"),
                    Variables.Deceased.Value.RejectIf(deceased => deceased,                $"Must be alive"),
                    Variables.Flags.Value.RejectIf   (flags => flags >= 2,                 $"Flags must be less than {maxFlags}")
                }.Join().Lift(FailIfAnyIncomplete).Select(_ => Result.Empty);
        }

        public static Rule GetProcess()
        {
            return
                new[]
                {
                    AbsoluteMaxAmount(100).LogContext("AbsoluteMaxAmount"),
                    Policies(18, 100, 2).LogContext("Policies"),
                    MaxTotalDebt(50).LogContext("MaxTotalDebt"),
                    MinTotalSalary(50).LogContext("MinTotalSalary"),
                    PrimaryApplicantMustHaveAddress().LogContext("PrimaryApplicantMustHaveAddress"),
                    CreditScoreUnderLimit(0.8).LogContext("CreditScoreUnderLimit")
                }.Join().CompileToRule();
        }
    }
}
