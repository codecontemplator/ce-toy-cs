using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ce_toy_cs.Framework
{
    public static class RuleExprUtils
    {
        public static RuleExprAst<int, RuleExprContext> Join<RuleExprContext>(this IEnumerable<RuleExprAst<int, RuleExprContext>> ruleExprAsts) where RuleExprContext : IRuleExprContext
        {
            var result = ruleExprAsts.First();
            foreach(var next in ruleExprAsts.Skip(1))
                result = result.AndThen(next);
            return result;
        }
    }
}
