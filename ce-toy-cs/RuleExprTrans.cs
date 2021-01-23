using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ce_toy_cs
{
    static class VotingMethods
    {
        public static int SelectMin(IEnumerable<(Applicant, int)> arg)
        {
            return arg.Min(x => x.Item2);
        }
    }

    static class RuleExprTrans
    {
        public static RuleExprAst<int, MRuleExprContext> Lift(this RuleExprAst<int, SRuleExprContext> sRuleExprAst)
        {
            return Lift(sRuleExprAst, VotingMethods.SelectMin);
        }

        public static RuleExprAst<int, MRuleExprContext> Lift(this RuleExprAst<int, SRuleExprContext> sRuleExprAst, Func<IEnumerable<(Applicant, int)>, int> vote)
        {
            var sRule = sRuleExprAst.Compile();
            var sKeys = sRuleExprAst.GetKeys();
            return
                from evalResult in MEval(sRule, sKeys)
                select vote(evalResult);
        }

        private static RuleExprAst<IEnumerable<(Applicant,int)>, MRuleExprContext> MEval(RuleExpr<int, SRuleExprContext> sRule, IEnumerable<string> sKeys)
        {
            return
                from applicants in MDsl.GetApplicants()
                from amountApplicantPairs in (from applicant in applicants.Values select SEval(applicant, sRule, sKeys))
                select amountApplicantPairs;
        }

        private static RuleExprAst<(Applicant, int), MRuleExprContext> SEval(Applicant applicant, RuleExpr<int, SRuleExprContext> sRule, IEnumerable<string> sKeys)
        {
            return new RuleExprAst<(Applicant, int), MRuleExprContext> {
                Expression = mcontext => SEvalImpl(applicant, sRule)(mcontext)
            };
        }

        private static RuleExpr<(Applicant, int), MRuleExprContext> SEvalImpl(Applicant applicant, RuleExpr<int, SRuleExprContext> sRule)
        {
            return mcontext =>
            {
                var (newAmountOption, newSContext) = sRule(new SRuleExprContext
                {
                    Amount = mcontext.Amount,
                    Applicant = applicant,
                    Log = ImmutableList<LogEntry>.Empty,
                });

                var newMContext = mcontext with
                {
                    Applicants = mcontext.Applicants.SetItem(applicant.Id, newSContext.Applicant),
                    Log = mcontext.Log.AddRange(newSContext.Log)
                };

                if (newAmountOption.IsSome(out var newAmount))
                {
                    return (Option<(Applicant, int)>.Some((applicant, newAmount)), newMContext);  // Rule applied to applicant and gave a result
                }
                else
                {
                    return (Option<(Applicant, int)>.Some((applicant, mcontext.Amount)), newMContext);  // Rule did not apply to applicant => amount is not affected => granted amount = requested amount
                }
            };
        }
    }
}