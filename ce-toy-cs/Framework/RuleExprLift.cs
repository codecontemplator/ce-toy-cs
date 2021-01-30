using ce_toy_cs.Framework.Functional;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ce_toy_cs.Framework
{
    public delegate Option<T> VoteMethod<T>(IEnumerable<Option<T>> input);

    static class VoteMethods
    {
        public static Option<Unit> FailIfAnyIncomplete(IEnumerable<Option<Unit>> input) => input.Any(x => !x.isSome) ? Option<Unit>.None : Option<Unit>.Some(Unit.Value);
        public static Option<Unit> FailIfAnyComplete(IEnumerable<Option<Unit>> input) => input.Any(x => x.isSome) ? Option<Unit>.None : Option<Unit>.Some(Unit.Value);
    }

    static class RuleExprLift
    {
        public static RuleExprAst<T, RuleExprContext<Unit>> Lift<T>(this RuleExprAst<T, RuleExprContext<string>> sRuleExprAst, VoteMethod<T> vote)
        {
            var sRule = sRuleExprAst.ExceptionContext().Compile();
            var sKeys = sRuleExprAst.GetKeys();
            return new RuleExprAst<T, RuleExprContext<Unit>>
            {
                Expression = mcontext => LiftImpl(sRule, vote, sKeys)(mcontext)
            };
        }

        public static RuleExpr<T, RuleExprContext<Unit>> LiftImpl<T>(this RuleExpr<Option<T>, RuleExprContext<string>> sRule, VoteMethod<T> vote, IEnumerable<string> sKeys)
        {
            return mcontext =>
            {
                var scontext = mcontext.WithSelector<string>(null);
                var result = new List<Option<T>>();
                foreach (var applicant in mcontext.Applicants)
                {
                    scontext = scontext.WithSelector(applicant.Key);
                    var (maybea, newSContext) = sRule(scontext);
                    scontext = newSContext;
                    if (!maybea.IsSome(out var a))
                        throw new Exception("Internal error. Lifting failed. Exception scope did not catch error as expected.");
                    result.Add(a);
                }

                return (vote(result), scontext.WithSelector(Unit.Value));
            };
        }
    }
}