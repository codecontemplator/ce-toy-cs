using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    public class FindKeysVisistor : ExpressionVisitor
    {
        public HashSet<string> FoundKeys = new HashSet<string>();

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.AndAlso)
            {
                Expression left = this.Visit(b.Left);
                Expression right = this.Visit(b.Right);

                // Make this binary expression an OrElse operation instead of an AndAlso operation.  
                return Expression.MakeBinary(ExpressionType.OrElse, left, right, b.IsLiftedToNull, b.Method);
            }

            return base.VisitBinary(b);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "GetValue")
            {
                var keyArg = node.Arguments.Single() as ConstantExpression;
                var key = keyArg.Value as string;
                FoundKeys.Add(key);
            }

            return base.VisitMethodCall(node);
        }
    }
}
