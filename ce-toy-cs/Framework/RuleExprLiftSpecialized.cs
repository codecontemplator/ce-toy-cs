using ce_toy_cs.Framework.Functional;
using System.Collections.Generic;
using System.Linq;
using static ce_toy_cs.Framework.DslSpecialized;

namespace ce_toy_cs.Framework
{
    public static class RuleExprLiftSpecialized
    {
        public static RuleExprAst<Result, RuleExprContext<Unit>> LiftPolicy(this RuleExprAst<PolicyPassType, RuleExprContext<string>> sRuleExprAst)
        {
            return sRuleExprAst.Lift(AllShouldPass).Select(_ => Result.Empty);
        }

        public static RuleExprAst<Result, RuleExprContext<Unit>> LiftPolicy(this RuleExprAst<PolicyRejectType, RuleExprContext<string>> sRuleExprAst)
        {
            return sRuleExprAst.Lift(NoneShouldPass).Select(_ => Result.Empty);
        }

        public static RuleExprAst<Result, RuleExprContext<Unit>> LiftPolicies(this IEnumerable<RuleExprAst<PolicyPassType, RuleExprContext<string>>> sRuleExprAst)
        {
            return sRuleExprAst.Join<string, PolicyPassType, PolicyPassType>().Lift(AllShouldPass).Select(_ => Result.Empty);
        }

        public static RuleExprAst<Result, RuleExprContext<Unit>> LiftPolicies(this IEnumerable<RuleExprAst<PolicyRejectType, RuleExprContext<string>>> sRuleExprAst)
        {
            return sRuleExprAst.Join<string, PolicyRejectType, PolicyRejectType>().Lift(NoneShouldPass).Select(_ => Result.Empty);
        }

        private static Option<PolicyPassType> AllShouldPass(IEnumerable<Option<PolicyPassType>> input) => 
            input.Any(x => !x.isSome) ? Option<PolicyPassType>.None : Option<PolicyPassType>.Some(PolicyPassType.Value);

        private static Option<PolicyRejectType> NoneShouldPass(IEnumerable<Option<PolicyRejectType>> input) => 
            input.Any(x => x.isSome) ? Option<PolicyRejectType>.None : Option<PolicyRejectType>.Some(PolicyRejectType.Value);
    }
}
