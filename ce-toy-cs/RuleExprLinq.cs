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
            return new RuleExprAst<T, RuleExprContext> { Expression = context => new Tuple<Option<T>, RuleExprContext>(Option<T>.Some(value), context).ToValueTuple() };
        }

        private static Expression MkTuple<T1, T2>(Expression t1, Expression t2)
        {
            var tupleConstructor = typeof(Tuple<T1, T2>).GetConstructor(new[] { typeof(T1), typeof(T2) }) ?? throw new Exception("Constructor not found");
            var returnTuple = Expression.New(tupleConstructor, new Expression[] { t1, t2 });
            var toValueTupleInfo = typeof(TupleExtensions).GetMethodExt("ToValueTuple", new[] { typeof(Tuple<,>) });
            var toValueTuple = toValueTupleInfo.MakeGenericMethod(typeof(T1), typeof(T2));
            var returnValueTuple = Expression.Call(null, toValueTuple, returnTuple);
            return returnValueTuple;
        }

        private static (MemberExpression, MemberExpression) DeconstructTuple(Expression tuple)
        {
            return (Expression.Field(tuple, "Item1"), Expression.Field(tuple, "Item2"));
        }

        private static Expression MkResult<T, RuleExprContext>(Expression newValueOptionAndContext, LambdaExpression transform)
        {
            var (newValueOption, newContext) = DeconstructTuple(newValueOptionAndContext);

            return
                Expression.IfThenElse(
                    Expression.Equal(
                        Expression.Field(newValueOption, "isSome"),
                        Expression.Constant(true)),
                    MkTuple<T, RuleExprContext>(
                        Expression.Invoke(
                            transform,
                            Expression.Field(newValueOption, "value")),
                        newContext),
                    MkTuple<T, RuleExprContext>(
                        GetNoneValue<T>(),
                        newContext));
        }

        private static Expression WrapSome<T>(Expression value)
        {
            Expression<Func<T, Option<T>>> toSome = value => Option<T>.Some(value);
            return Expression.Invoke(toSome, value);
        }

        private static Expression GetNoneValue<T>()
        {
            return Expression.Call(typeof(Option<T>).GetProperty("None").GetGetMethod());
        }

        public static RuleExprAst<U, RuleExprContext> Select<T, U, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T, U>> convert)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var newValueOptionAndContext = Expression.Invoke(expr.Expression, context);

            var value = Expression.Parameter(typeof(T), "value");
            var convertedValue = WrapSome<U>(Expression.Invoke(convert, value));
            var convertValue = Expression.Lambda(convertedValue, value);

            var returnValueTuple = MkResult<U, RuleExprContext>(newValueOptionAndContext, convertValue);
            var resultFunc = Expression.Lambda<RuleExpr<U, RuleExprContext>>(returnValueTuple, context);
            
            return new RuleExprAst<U, RuleExprContext> { Expression = resultFunc };

            //return context =>
            //{
            //    var (a, context2) = expr(context);    | var newValueOptionAndContext = expr(context)
            //    return (convert(a), context2);        | return mkResult(newValueOptionAndContext, \value -> Some(convert(a))
            //};
        }

        public static RuleExprAst<V, RuleExprContext> SelectMany<T, U, V, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T, RuleExprAst<U, RuleExprContext>>> selector, Expression<Func<T, U, V>> projector)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var valueOptionAndContextA = Expression.Invoke(expr.Expression, context);
            var (_, contextA) = DeconstructTuple(valueOptionAndContextA);

            var valueAParam = Expression.Parameter(typeof(T), "valueA");
            var valueBParam = Expression.Parameter(typeof(U), "valueB");
            var projectBDef = Expression.Lambda(Expression.Invoke(projector, valueAParam, valueBParam));
            var projectB = Expression.Lambda(projectBDef, valueBParam);            
            var projectADef = 
                MkResult<U, RuleExprContext>(
                    Expression.Invoke(
                        Expression.Invoke(selector, valueAParam), 
                        contextA), 
                    projectB);
            var projectA = Expression.Lambda(projectADef, valueAParam);
            var returnValueTuple = MkResult<V, RuleExprContext>(valueOptionAndContextA, projectA);

            var resultFunc = Expression.Lambda<RuleExpr<V, RuleExprContext>>(returnValueTuple, context);
            return new RuleExprAst<V, RuleExprContext> { Expression = resultFunc };

            //return context =>
            //{
            //    var (a, context2) = expr(context);            | 
            //    var (b, context3) = selector(a)(context2);    | return mkResult(valueOptionAndContext, \a -> { var valueOptionAndContext2 = selector(a)
            //    return (projector(a, b), context3);           |    };
            //};

            // return context => 
            // {
            //    var valueOptionAndContextA = expr(context);
            //    return mkResult(valueOptionAndContextA, a => { 
            //       return mkResult(selector(a)(valueOptionAndContextA.Context), b => projector(a,b));
            //    }
            // }
        }

        public static RuleExprAst<T, RuleExprContext> Where<T, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T, bool>> filter)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var newValueOptionAndContext = Expression.Invoke(expr.Expression, context);

            var value = Expression.Parameter(typeof(T), "value");
            var convertedValue =
                Expression.IfThenElse(
                    Expression.Equal(
                        Expression.Invoke(filter, value),
                        Expression.Constant(true)),
                    WrapSome<T>(value),
                    GetNoneValue<T>());
            var convertValue = Expression.Lambda(convertedValue, value);

            var returnValueTuple = MkResult<T, RuleExprContext>(newValueOptionAndContext, convertValue);
            var resultFunc = Expression.Lambda<RuleExpr<T, RuleExprContext>>(returnValueTuple, context);
            return new RuleExprAst<T, RuleExprContext> { Expression = resultFunc };

            //  return context => 
            //  {
            //      var newValueOptionAndContext = expr(context);
            //      return mkResult(newValueOptionAndContext, \value -> filter(value) ? Some(value) : Option.None);
            //  }
        }

        public static RuleExprAst<IEnumerable<V>, RuleExprContext> SelectMany<T, U, V, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T, IEnumerable<RuleExprAst<U, RuleExprContext>>>> selector, Expression<Func<T, IEnumerable<U>, IEnumerable<V>>> projector)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var (intermediateValue, intermediateContext) = DeconstructTuple(Expression.Invoke(expr.Expression, context));
            var selectorResult = Expression.Invoke(selector, intermediateValue);

            var sequencedResult = Expression.Invoke(MkSequencer<U, RuleExprContext>(), selectorResult);
            var sequencedResultExpression = Expression.Property(sequencedResult, "Expression");

            var (finalValue, finalContext) = DeconstructTuple(Expression.Invoke(sequencedResultExpression, intermediateContext));
            var projectedValue = Expression.Invoke(projector, intermediateValue, finalValue);

            var returnValueTuple = MkTuple<IEnumerable<V>, RuleExprContext>(projectedValue, finalContext);
            var resultFunc = Expression.Lambda<RuleExpr<IEnumerable<V>, RuleExprContext>>(returnValueTuple, context);
            return new RuleExprAst<IEnumerable<V>, RuleExprContext> { Expression = resultFunc };

            //return context =>
            //{
            //    var (a, context1) = expr(context0);
            //    var fs = selector(a);                    
            //    var (bs, contextn) = (sequence fs)(context1)      // sequence :: IEnumerable<RuleExprAst<U>> -> RuleExprAst<IEnumerable<U>>
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