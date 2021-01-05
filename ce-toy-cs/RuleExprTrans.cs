using System;
using System.Collections.Generic;
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
        public static RuleExprAst<int, MRuleExprContext> Lift(RuleExprAst<int, SRuleExprContext> sRuleExprAst)
        {
            return Lift(sRuleExprAst, VotingMethods.SelectMin);
        }

        public static RuleExprAst<int, MRuleExprContext> Lift(RuleExprAst<int, SRuleExprContext> sRuleExprAst, Func<IEnumerable<(Applicant, int)>, int> vote)
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
                var (newAmount, newSContext) = sRule(new SRuleExprContext
                {
                    Amount = mcontext.Amount,
                    Applicant = applicant
                });

                var newMContext = mcontext with
                {
                    Applicants = mcontext.Applicants.SetItem(applicant.Id, newSContext.Applicant)
                };

                return ((applicant, newAmount), newMContext);
            };
        }
    }
}