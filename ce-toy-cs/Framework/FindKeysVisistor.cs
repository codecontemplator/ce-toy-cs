using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace ce_toy_cs.Framework
{
    public class FindKeysVisistor : ExpressionVisitor
    {
        public HashSet<string> FoundKeys = new HashSet<string>();
        private int _depth = 0;

        //[return: NotNullIfNotNull("node")]
        //public override Expression Visit(Expression node)
        //{
        //    if (node != null)
        //    {
        //        Debug.WriteLine(node.NodeType + " : " + node.ToString());
        //    }
        //    return base.Visit(node);
        //}

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node != null && node.ToString().StartsWith("Variables."))
            {
                var key = node.ToString().Substring(node.ToString().IndexOf(".") + 1);
                key = key.Replace(".Values", "").Replace(".Value", "");
                //Debug.WriteLine(key);
                FoundKeys.Add(key);
                return node;
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
//            Debug.WriteLine(new string(' ', _depth * 2) + node.Method.Name);

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
            if (node.Method.Name == "GetValuesImpl")
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
            else if (node.Method.Name == "LiftImpl")
            {
                var keysArg = node.Arguments[2] as MemberExpression;
                var keysArgExpression = keysArg.Expression as ConstantExpression;
                var sKeysFieldInfo = keysArgExpression.Value.GetType().GetField("sKeys");
                var keys = sKeysFieldInfo.GetValue(keysArgExpression.Value) as IEnumerable<string>;
                foreach (var key in keys)
                    FoundKeys.Add(key);
                return node;
            }
            else
            {
                _depth++;
                var result = base.VisitMethodCall(node);
                _depth--;
                return result;
            }
        }
    }
}
