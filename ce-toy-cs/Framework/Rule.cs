using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text;

namespace ce_toy_cs.Framework
{
    public record Rule
    {
        public RuleExpr<int, MRuleExprContext> RuleExpr { get; init; }
        public IImmutableList<string> Keys { get; init; }
    }

    class RuleBuilder
    {
        private RuleExprAst<int, MRuleExprContext> _current;

        public RuleBuilder()
        {
        }

        public RuleBuilder Add(Expression<Func<RuleExprAst<int, MRuleExprContext>>> ruleDefintion)
        {
            var ruleName = GetRuleName((MethodCallExpression)ruleDefintion.Body);
            var ruleImplementation = ruleDefintion.Compile()();
            var loggingRuleImplementation = ruleImplementation.WithLogging(ruleName);
            if (_current == null)
                _current = loggingRuleImplementation;
            else
                _current = _current.AndThen(loggingRuleImplementation);
            return this;
        }

        private string GetRuleName(MethodCallExpression mce)
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

        public Rule Build()
        {
            return new Rule
            {
                RuleExpr = _current.Compile(),
                Keys = _current.GetKeys().ToImmutableList()
            };
        }
    }
}
