using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    static class RuleExprLinq
    {
        public static RuleExprAst<T, RuleExprContext> Wrap<T, RuleExprContext>(T value)
        {
            return new RuleExprAst<T, RuleExprContext> { Expression = context => new Tuple<T, RuleExprContext>(value, context).ToValueTuple() };
        }

        private static Expression MkTuple<T1, T2>(Expression t1, Expression t2)
        {
            var tupleConstructor = typeof(Tuple<T1, T2>).GetConstructor(new[] { typeof(T1), typeof(T2) });
            var returnTuple = Expression.New(tupleConstructor, new Expression[] { t1, t2 });
            var toValueTupleInfo = typeof(TupleExtensions).GetMethodExt("ToValueTuple", new[] { typeof(Tuple<,>) });
            var toValueTuple = toValueTupleInfo.MakeGenericMethod(typeof(T1), typeof(T2));
            var returnValueTuple = Expression.Call(null, toValueTuple, returnTuple);
            return returnValueTuple;
        }

        public static RuleExprAst<U, RuleExprContext> Select<T, U, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T, U>> convert)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var valueAndNewContext = Expression.Invoke(expr.Expression, context);
            var value = Expression.Field(valueAndNewContext, "Item1");
            var newContext = Expression.Field(valueAndNewContext, "Item2");
            var convertedValue = Expression.Invoke(convert, value);
            var returnValueTuple = MkTuple<U, RuleExprContext>(convertedValue, newContext);
            var resultFunc = Expression.Lambda<RuleExpr<U, RuleExprContext>>(returnValueTuple, context);
            return new RuleExprAst<U, RuleExprContext> { Expression = resultFunc };
            //return context =>
            //{
            //    var (a, context2) = expr(context);
            //    return (convert(a), context2);
            //};
        }

        public static RuleExprAst<V, RuleExprContext> SelectMany<T, U, V, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T, RuleExprAst<U, RuleExprContext>>> selector, Expression<Func<T, U, V>> projector)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var intermediateValueAndContext = Expression.Invoke(expr.Expression, context);
            var intermediateValue = Expression.Field(intermediateValueAndContext, "Item1");
            var intermediateContext = Expression.Field(intermediateValueAndContext, "Item2");
            var selectorResult = Expression.Invoke(selector, intermediateValue);
            var selectorResultExpression = Expression.Property(selectorResult, "Expression");
            var finalValueAndContext = Expression.Invoke(selectorResultExpression, intermediateContext);
            var finalValue = Expression.Field(finalValueAndContext, "Item1");
            var finalContext = Expression.Field(finalValueAndContext, "Item2");
            var projectedValue = Expression.Invoke(projector, intermediateValue, finalValue);

            var returnValueTuple = MkTuple<V, RuleExprContext>(projectedValue, finalContext);
            var resultFunc = Expression.Lambda<RuleExpr<V, RuleExprContext>>(returnValueTuple, context);
            return new RuleExprAst<V, RuleExprContext> { Expression = resultFunc };

            //return context =>
            //{
            //    var (a, context2) = expr(context);
            //    var (b, context3) = selector(a)(context2);
            //    return (projector(a, b), context3);
            //};
        }

        public static RuleExprAst<IEnumerable<V>, RuleExprContext> SelectMany<T, U, V, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T, IEnumerable<RuleExprAst<U, RuleExprContext>>>> selector, Expression<Func<T, IEnumerable<U>, IEnumerable<V>>> projector)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var intermediateValueAndContext = Expression.Invoke(expr.Expression, context);
            var intermediateValue = Expression.Field(intermediateValueAndContext, "Item1");
            var intermediateContext = Expression.Field(intermediateValueAndContext, "Item2");
            var selectorResult = Expression.Invoke(selector, intermediateValue);

            var sequencer = MkSequencer<U, RuleExprContext>();
            var sequencedResult = Expression.Invoke(sequencer, selectorResult);
            var sequencedResultExpression = Expression.Property(sequencedResult, "Expression");

            var finalValueAndContext = Expression.Invoke(sequencedResultExpression, intermediateContext);

            var finalValue = Expression.Field(finalValueAndContext, "Item1");
            var finalContext = Expression.Field(finalValueAndContext, "Item2");
            var projectedValue = Expression.Invoke(projector, intermediateValue, finalValue);

            var returnValueTuple = MkTuple<IEnumerable<V>, RuleExprContext>(projectedValue, finalContext);
            var resultFunc = Expression.Lambda<RuleExpr<IEnumerable<V>, RuleExprContext>>(returnValueTuple, context);
            return new RuleExprAst<IEnumerable<V>, RuleExprContext> { Expression = resultFunc };

            //return context =>
            //{
            //    var (a, context1) = expr(context0);
            //    var fs = selector(a);                    
            //    var (bs, contextn) = sequence fs      // sequence :: IEnumerable<RuleExprAst<U>> -> RuleExprAst<IEnumerable<U>>
            //    return (projector(a, bs), contextn);
            //};
        }

        private delegate RuleExprAst<ImmutableList<T>, RuleExprContext> SequencerDelegate<T, RuleExprContext>(IEnumerable<RuleExprAst<T, RuleExprContext>> input);

        private static Expression<SequencerDelegate<U, RuleExprContext>> MkSequencer<U, RuleExprContext>()
        {
            Expression<
                Func<
                    Func<IEnumerable<RuleExprAst<U, RuleExprContext>>,RuleExprAst<ImmutableList<U>, RuleExprContext>>,
                    Func<IEnumerable<RuleExprAst<U, RuleExprContext>>,RuleExprAst<ImmutableList<U>, RuleExprContext>>
                >
            > sequenceNonRecursive = f => xs => 
                    !xs.Any() 
                        ?
                            Wrap<ImmutableList<U>, RuleExprContext>(ImmutableList<U>.Empty) 
                        :
                            xs.First().SelectMany(t => f(xs.Skip(1)), (t, ts) => ts.Add(t));

            var fixExpression = YCombinator<IEnumerable<RuleExprAst<U, RuleExprContext>>, RuleExprAst<ImmutableList<U>, RuleExprContext>>.Fix;
            var sequence = Expression.Invoke(fixExpression, sequenceNonRecursive);
             
            var arg = Expression.Parameter(typeof(IEnumerable<RuleExprAst<U, RuleExprContext>>), "arg");
            return Expression.Lambda<SequencerDelegate<U, RuleExprContext>>(Expression.Invoke(sequence, arg), arg);
        }
    }
}