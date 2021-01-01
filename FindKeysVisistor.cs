using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    public class FindKeysVisistor : ExpressionVisitor
    {
        public HashSet<string> FoundKeys = new HashSet<string>();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "GetValue")
            {
                var keyArg = node.Arguments.Single() as ConstantExpression;
                var key = keyArg.Value as string;
                FoundKeys.Add(key);
                return node;
            }
            else if (node.Method.Name == "GetValues")
            {
                return base.VisitMethodCall(node);
            }
            else
                return base.VisitMethodCall(node);
        }
    }
}
