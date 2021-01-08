using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ce_toy_cs.Details
{
    static class ExpressionEx
    {
        // Ref: https://stackoverflow.com/questions/27175558/foreach-loop-using-expression-trees/27193081
        // Ref: https://dotnetfiddle.net/Pl89Gr
        public static Expression ForEach(Expression collection, ParameterExpression loopVar, Expression loopContent, LabelTarget breakLabel = null)
        {
            var elementType = loopVar.Type;
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);

            var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
            var getEnumeratorCall = Expression.Call(collection, enumerableType.GetMethod("GetEnumerator"));
            var enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);

            var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod("MoveNext"));

            breakLabel = breakLabel ?? Expression.Label("LoopBreak");

            var ifThenElseExpr = Expression.IfThenElse(
                Expression.Equal(moveNextCall, Expression.Constant(true)),
                Expression.Block(new[] { loopVar },
                    Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")),
                    loopContent
                ),
                Expression.Break(breakLabel)
            );

            var loop = Expression.Loop(ifThenElseExpr, breakLabel);

            var block = Expression.Block(new[] { enumeratorVar },
                enumeratorAssign,
                loop
            );

            return block;
        }
    }
}
