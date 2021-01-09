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
            if (node.Method.Name == "GetValue")
            {
                if (node.Arguments.Count == 1)
                {
                    var keyArg = node.Arguments[0] as ConstantExpression;
                    var key = keyArg.Value as string;
                    FoundKeys.Add(key);
                    return node;
                }
                else
                {
                    var keysArg = node.Arguments[1] as MemberExpression;
                    var keysArgExpression = keysArg.Expression as ConstantExpression;
                    var sKeysFieldInfo = keysArgExpression.Value.GetType().GetField("key");
                    var key = sKeysFieldInfo.GetValue(keysArgExpression.Value) as string;
                    FoundKeys.Add(key);
                    return node;
                }
            }
            if (node.Method.Name == "GetValueImpl")
            {
                var keysArg = node.Arguments[0] as MemberExpression;
                var keysArgExpression = keysArg.Expression as ConstantExpression;
                var sKeysFieldInfo = keysArgExpression.Value.GetType().GetField("key");
                var key = sKeysFieldInfo.GetValue(keysArgExpression.Value) as string;
                FoundKeys.Add(key);
                return node;
            }
            else if (node.Method.Name == "SEval")
            {
                var keysArg = node.Arguments[2] as MemberExpression;
                var keysArgExpression = keysArg.Expression as ConstantExpression;
                var sKeysFieldInfo = keysArgExpression.Value.GetType().GetField("sKeys");
                var keys = sKeysFieldInfo.GetValue(keysArgExpression.Value) as IEnumerable<string>;
                foreach (var key in keys)
                    FoundKeys.Add(key);
                return node;
            }
            else if (node.Method.Name == "Select")
            {
                return base.VisitMethodCall(node);

            }
            else
                return base.VisitMethodCall(node);
        }
    }
}
