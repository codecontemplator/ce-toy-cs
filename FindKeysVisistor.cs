using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    public class FindKeysVisistor : ExpressionVisitor
    {
        public HashSet<string> FoundKeys = new HashSet<string>();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Debug.WriteLine(node.Method.Name);
            if (node.Method.Name == "GetValue")
            {
                // Never called :(
                var keyArg = node.Arguments.Single() as ConstantExpression;
                var key = keyArg.Value as string;
                FoundKeys.Add(key);
                return node;
            }
            else if (node.Method.Name == "GetValues")
            {
                // Not really a good solution to track calls to getvalues... :(
                var keyArg = node.Arguments.Single() as ConstantExpression;
                var key = keyArg.Value as string;
                FoundKeys.Add(key);
                return node;
            }
            else
                return base.VisitMethodCall(node);
        }
    }
}
