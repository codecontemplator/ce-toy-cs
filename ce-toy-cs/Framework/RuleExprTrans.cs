using ce_toy_cs.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ce_toy_cs.Framework
{
    static class VotingMethods
    {
        public static Decision SelectMin(IEnumerable<(Applicant, Decision)> arg)
        {
            var decisions = arg.Select(x => x.Item2).ToList();
            return decisions.Min();
        }
    }

    static class RuleExprTrans
    {
        public static RuleExprAst<Decision, MRuleExprContext> Lift(this RuleExprAst<Decision, SRuleExprContext> sRuleExprAst)
        {
            return sRuleExprAst.Lift(VotingMethods.SelectMin);
        }

        public static RuleExprAst<Decision, MRuleExprContext> Lift(this RuleExprAst<Decision, SRuleExprContext> sRuleExprAst, Func<IEnumerable<(Applicant, Decision)>, Decision> vote)
        {
            var sRule = sRuleExprAst.Compile();
            var sKeys = sRuleExprAst.GetKeys();
            return
                from evalResult in MEval(sRule, sKeys)
                select vote(evalResult);
        }

        private static RuleExprAst<IEnumerable<(Applicant, Decision)>, MRuleExprContext> MEval(RuleExpr<Decision, SRuleExprContext> sRule, IEnumerable<string> sKeys)
        {
            return
                from applicants in MDsl.GetApplicants()
                from amountApplicantPairs in from applicant in applicants.Values select SEval(applicant, sRule, sKeys)
                select amountApplicantPairs;
        }

        private static RuleExprAst<(Applicant, Decision), MRuleExprContext> SEval(Applicant applicant, RuleExpr<Decision, SRuleExprContext> sRule, IEnumerable<string> sKeys)
        {
            return new RuleExprAst<(Applicant, Decision), MRuleExprContext>
            {
                Expression = mcontext => SEvalImpl(applicant, sRule)(mcontext)
            };
        }

        private static RuleExpr<(Applicant, Decision), MRuleExprContext> SEvalImpl(Applicant applicant, RuleExpr<Decision, SRuleExprContext> sRule)
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

                if (newAmountOption.IsSome(out var decision))
                {
                    return (Option<(Applicant, Decision)>.Some((applicant, decision)), newMContext);  // Rule applied to applicant and gave a result
                }
                else
                {
                    return (Option<(Applicant, Decision)>.Some((applicant, Decision.Accept)), newMContext);  // Rule did not apply to applicant => accept
                }
            };
        }
    }
}