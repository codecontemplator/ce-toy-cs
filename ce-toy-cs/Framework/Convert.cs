using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text;

namespace ce_toy_cs.Framework
{
    public static class Convert
    {
        public static Rule CompileToRule(this RuleExprAst<int, MRuleExprContext> expr)
        {
            return new Rule
            {
                RuleExpr = expr.Compile(),
                Keys = expr.GetKeys().ToImmutableList()
            };
        }

        public static RuleExprAst<int, MRuleExprContext> ToRuleExprAst(Expression<Func<RuleExprAst<int, MRuleExprContext>>> ruleDefintion)
        {
            var ruleName = GetRuleName((MethodCallExpression)ruleDefintion.Body);
            var ruleImplementation = ruleDefintion.Compile()();
            var loggingRuleImplementation = ruleImplementation.WithLogging(ruleName);
            return loggingRuleImplementation;
        }

        private static string GetRuleName(MethodCallExpression mce)
        {
            var sb = new StringBuilder();
            var methodInfo = mce.Method;
            sb.Append(methodInfo.Name);
            var parameterInfo = methodInfo.GetParameters();
            sb.Append("(");
            int i = 0;
            foreach (var arg in mce.Arguments)
            {
                var carg = (ConstantExpression)arg;
                if (i > 0)
                    sb.Append(",");
                sb.Append(parameterInfo[i].Name).Append("=");
                sb.Append(carg.Value);
                i++;
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}
