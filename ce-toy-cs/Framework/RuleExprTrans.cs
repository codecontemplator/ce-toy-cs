using ce_toy_cs.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ce_toy_cs.Framework
{
    static class VotingMethods
    {
        public static T SelectMin<T>(IEnumerable<(Applicant, T)> arg)
        {
            var decisions = arg.Select(x => x.Item2).ToList();
            return decisions.Min();
        }
    }

    static class RuleExprTrans
    {
        public static RuleExprAst<Credit, MRuleExprContext> LiftAmountRule(this RuleExprAst<int, MRuleExprContext> ruleExprAst)
        {
            return
                from amount in ruleExprAst
                from credit in MDsl.GetCredit()
                select credit with { Amount = amount };
        }

        public static RuleExprAst<Credit, MRuleExprContext> LiftAmountRule(this RuleExprAst<DecisionType, MRuleExprContext> ruleExprAst)
        {
            return
                from amount in ruleExprAst
                from credit in MDsl.GetCredit()
                select credit with { Amount = amount };
        }

        public static RuleExprAst<T, MRuleExprContext> Lift<T>(this RuleExprAst<T, SRuleExprContext> sRuleExprAst)
        {
            return sRuleExprAst.Lift(VotingMethods.SelectMin);
        }

        public static RuleExprAst<T, MRuleExprContext> Lift<T>(this RuleExprAst<T, SRuleExprContext> sRuleExprAst, Func<IEnumerable<(Applicant, T)>, T> vote, T defaultValue = default(T))
        {
            var sRule = sRuleExprAst.Compile();
            var sKeys = sRuleExprAst.GetKeys();
            return
                from evalResult in MEval(sRule, sKeys, defaultValue)
                select vote(evalResult);
        }

        private static RuleExprAst<IEnumerable<(Applicant, T)>, MRuleExprContext> MEval<T>(RuleExpr<T, SRuleExprContext> sRule, IEnumerable<string> sKeys, T defaultValue)
        {
            return
                from applicants in MDsl.GetApplicants()
                from amountApplicantPairs in from applicant in applicants.Values select SEval(applicant, sRule, sKeys, defaultValue)
                select amountApplicantPairs;
        }

        private static RuleExprAst<(Applicant, T), MRuleExprContext> SEval<T>(Applicant applicant, RuleExpr<T, SRuleExprContext> sRule, IEnumerable<string> sKeys, T defaultValue)
        {
            return new RuleExprAst<(Applicant, T), MRuleExprContext>
            {
                Expression = mcontext => SEvalImpl(applicant, sRule, defaultValue)(mcontext)
            };
        }

        private static RuleExpr<(Applicant, T), MRuleExprContext> SEvalImpl<T>(Applicant applicant, RuleExpr<T, SRuleExprContext> sRule, T defaultValue)
        {
            return mcontext =>
            {
                var (newValueOption, newSContext) = sRule(new SRuleExprContext
                {
                    RequestedAmount = mcontext.RequestedAmount,
                    Applicant = applicant,
                    Log = ImmutableList<LogEntry>.Empty,
                });

                var newMContext = mcontext with
                {
                    Applicants = mcontext.Applicants.SetItem(applicant.Id, newSContext.Applicant),
                    Log = mcontext.Log.AddRange(newSContext.Log)
                };

                if (newValueOption.IsSome(out var value))
                {
                    return (Option<(Applicant, T)>.Some((applicant, value)), newMContext);  // Rule applied to applicant and gave a result
                }
                else
                {
                    return (Option<(Applicant, T)>.Some((applicant, defaultValue)), newMContext);  // Rule did not apply to applicant => use default
                }
            };
        }
    }
}