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
            if (node.Method.Name == "GetValues")
            {
                var keyArg = node.Arguments.Single() as ConstantExpression;  // TODO: handle predicate overload 
                var key = keyArg.Value as string;
                FoundKeys.Add(key);
                return node;
            }
            else if (node.Method.Name == "Lift")
            {
                return base.VisitMethodCall(node);  // TODO
            }
            else
                return base.VisitMethodCall(node);
        }
    }
}
