using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ce_toy_cs.Framework.Details
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

        public static Expression MkTuple<T1, T2>(Expression t1, Expression t2)
        {
            Expression<Func<T1, T2, (T1, T2)>> toTuple = (x, y) => new Tuple<T1, T2>(x, y).ToValueTuple();
            return Expression.Invoke(toTuple, t1, t2);
        }

        public static Expression WrapSome<T>(Expression value)
        {
            Expression<Func<T, Option<T>>> toSome = value => Option<T>.Some(value);
            return Expression.Invoke(toSome, value);
        }

        public static Expression GetNoneValue<T>()
        {
            Expression<Func<Option<T>>> getNoneValue = () => Option<T>.None;
            return Expression.Invoke(getNoneValue);
        }

        public static Expression CreateLogEntry(Expression messageExpr, Expression preContextExpr, Expression postContextExpr, Expression valueExpr)
        {
            Expression<Func<string, RuleExprContextBase, RuleExprContextBase, object, LogEntry>> createLogEntry = (message, preContext, postContext, value) =>
                new LogEntry { Message = message, PreContext = preContext, PostContext = postContext, Value = value };
            return Expression.Invoke(createLogEntry, messageExpr, preContextExpr, postContextExpr, valueExpr);
        }
    }
}
