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

        //private static Expression MkResult<T, RuleExprContext>(Expression newValueOptionAndContext, LambdaExpression transform)
        //{
        //    var (newValueOption, newContext) = DeconstructTuple(newValueOptionAndContext);

        //    return
        //        Expression.Condition(
        //            Expression.Equal(Expression.Field(newValueOption, "isSome"), Expression.Constant(true)),
        //            MkTuple<T, RuleExprContext>(Expression.Invoke(transform, Expression.Field(newValueOption, "value")), newContext),
        //            MkTuple<T, RuleExprContext>(GetNoneValue<T>(), newContext));
        //}

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

            ParameterExpression valueOptionAndContextAVar = Expression.Variable(typeof((Option<T>, RuleExprContext)), "valueOptionAndContextAVar");
            ParameterExpression valueOptionAVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            ParameterExpression contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

            var functionImplementation =
                Expression.Block(
                    Expression.Assign(valueOptionAndContextAVar, Expression.Invoke(expr.Expression, context)),
                    Expression.Assign(valueOptionAVar, Expression.Field(valueOptionAndContextAVar, "Item1")),
                    Expression.Assign(contextAVar, Expression.Field(valueOptionAndContextAVar, "Item2")),
                    Expression.Condition(
                        Expression.Equal(Expression.Field(valueOptionAVar, "isSome"), Expression.Constant(true)),
                        MkTuple<Option<U>, RuleExprContext>(
                            WrapSome<U>(Expression.Invoke(convert, Expression.Field(valueOptionAVar, "value"))),
                            contextAVar
                        ),
                        MkTuple<Option<U>, RuleExprContext>(GetNoneValue<U>(), contextAVar)
                    )
                );


            var functionBody =
                Expression.Block(
                    new[] { valueOptionAndContextAVar, valueOptionAVar, contextAVar },
                    functionImplementation
                );

            var function = Expression.Lambda<RuleExpr<U, RuleExprContext>>(functionBody, context);

            return new RuleExprAst<U, RuleExprContext> { Expression = function };

            //return context =>
            //{
            //    var (a, context2) = expr(context);
            //    return (convert(a), context2);
            //};
        }

        public static RuleExprAst<V, RuleExprContext> SelectMany<T, U, V, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T, RuleExprAst<U, RuleExprContext>>> selector, Expression<Func<T, U, V>> projector)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");            

            ParameterExpression valueOptionAndContextAVar = Expression.Variable(typeof((Option<T>,RuleExprContext)), "valueOptionAndContextAVar");
            ParameterExpression valueOptionAVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            ParameterExpression contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

            ParameterExpression valueOptionAndContextBVar = Expression.Variable(typeof((Option<U>, RuleExprContext)), "valueOptionAndContextBVar");
            ParameterExpression valueOptionBVar = Expression.Variable(typeof(Option<U>), "valueOptionBVar");
            ParameterExpression contextBVar = Expression.Variable(typeof(RuleExprContext), "contextBVar");

            var functionImplementation =
                Expression.Block(
                    Expression.Assign(valueOptionAndContextAVar, Expression.Invoke(expr.Expression, context)),
                    Expression.Assign(valueOptionAVar, Expression.Field(valueOptionAndContextAVar, "Item1")),
                    Expression.Assign(contextAVar, Expression.Field(valueOptionAndContextAVar, "Item2")),
                    Expression.Condition(
                        Expression.Equal(Expression.Field(valueOptionAVar, "isSome"), Expression.Constant(true)),
                        Expression.Block(
                            Expression.Assign(valueOptionAndContextBVar,
                                Expression.Invoke(
                                    Expression.Property(Expression.Invoke(selector, Expression.Field(valueOptionAVar, "value")), "Expression"),
                                    contextAVar
                                )),
                            Expression.Assign(valueOptionBVar, Expression.Field(valueOptionAndContextBVar, "Item1")),
                            Expression.Assign(contextBVar, Expression.Field(valueOptionAndContextBVar, "Item2")),
                            Expression.Condition(
                                Expression.Equal(Expression.Field(valueOptionBVar, "isSome"), Expression.Constant(true)),
                                MkTuple<Option<V>,RuleExprContext>(
                                    WrapSome<V>(Expression.Invoke(projector, Expression.Field(valueOptionAVar, "value"), Expression.Field(valueOptionBVar, "value"))),
                                    contextBVar
                                ),
                                MkTuple<Option<V>, RuleExprContext>(GetNoneValue<V>(), contextBVar)
                            )
                        ),
                        MkTuple<Option<V>, RuleExprContext>(GetNoneValue<V>(), contextAVar)
                    )
                );

            var functionBody =
                Expression.Block(
                    new[] { valueOptionAndContextAVar, valueOptionAVar, contextAVar, valueOptionAndContextBVar, valueOptionBVar, contextBVar},
                    functionImplementation
                );
            var function = Expression.Lambda<RuleExpr<V, RuleExprContext>>(functionBody, context);
            return new RuleExprAst<V, RuleExprContext> { Expression = function };

            //return context =>
            //{
            //    var (a, context2) = expr(context);            
            //    var (b, context3) = selector(a)(context2);
            //    return (projector(a, b), context3);
            //};
        }

        public static RuleExprAst<T, RuleExprContext> Where<T, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T, bool>> predicate)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");

            ParameterExpression valueOptionAndContextAVar = Expression.Variable(typeof((Option<T>, RuleExprContext)), "valueOptionAndContextAVar");
            ParameterExpression valueOptionAVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            ParameterExpression contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

            var functionImplementation =
                Expression.Block(
                    Expression.Assign(valueOptionAndContextAVar, Expression.Invoke(expr.Expression, context)),
                    Expression.Assign(valueOptionAVar, Expression.Field(valueOptionAndContextAVar, "Item1")),
                    Expression.Assign(contextAVar, Expression.Field(valueOptionAndContextAVar, "Item2")),
                    MkTuple<Option<T>, RuleExprContext>(
                        Expression.Condition(
                            Expression.Equal(Expression.Field(valueOptionAVar, "isSome"), Expression.Constant(true)),
                            Expression.Condition(
                                Expression.Equal(Expression.Invoke(predicate, Expression.Field(valueOptionAVar, "value")), Expression.Constant(true)),
                                WrapSome<T>(Expression.Field(valueOptionAVar, "value")),
                                GetNoneValue<T>()
                            ),
                            GetNoneValue<T>()
                        ),
                        contextAVar
                    )
                );

            var functionBody =
                Expression.Block(
                    new[] { valueOptionAndContextAVar, valueOptionAVar, contextAVar },
                    functionImplementation
                );

            var function = Expression.Lambda<RuleExpr<T, RuleExprContext>>(functionBody, context);
            return new RuleExprAst<T, RuleExprContext> { Expression = function };

            //  return context => 
            //  {
            //      var (a, context') = expr(context);
            //      return (predicate(a) ? Some(a) : None, context');
            //  }
        }

        public static RuleExprAst<IEnumerable<V>, RuleExprContext> SelectMany<T, U, V, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T, IEnumerable<RuleExprAst<U, RuleExprContext>>>> selector, Expression<Func<T, IEnumerable<U>, IEnumerable<V>>> projector)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");

            ParameterExpression valueOptionAndContextAVar = Expression.Variable(typeof((Option<T>, RuleExprContext)), "valueOptionAndContextAVar");
            ParameterExpression valueOptionAVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            ParameterExpression contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

            ParameterExpression valueOptionAndContextBVar = Expression.Variable(typeof((Option<ImmutableList<U>>, RuleExprContext)), "valueOptionAndContextBVar");
            ParameterExpression valueOptionBVar = Expression.Variable(typeof(Option<ImmutableList<U>>), "valueOptionBVar");
            ParameterExpression contextBVar = Expression.Variable(typeof(RuleExprContext), "contextBVar");

            var functionImplementation =
                Expression.Block(
                    Expression.Assign(valueOptionAndContextAVar, Expression.Invoke(expr.Expression, context)),
                    Expression.Assign(valueOptionAVar, Expression.Field(valueOptionAndContextAVar, "Item1")),
                    Expression.Assign(contextAVar, Expression.Field(valueOptionAndContextAVar, "Item2")),
                    Expression.Condition(
                        Expression.Equal(Expression.Field(valueOptionAVar, "isSome"), Expression.Constant(true)),
                        Expression.Block(
                            Expression.Assign(valueOptionAndContextBVar,
                                Expression.Invoke(
                                    Expression.Property(Sequence<U, RuleExprContext>(Expression.Invoke(selector, Expression.Field(valueOptionAVar, "value"))), "Expression"),
                                    contextAVar
                                )),
                            Expression.Assign(valueOptionBVar, Expression.Field(valueOptionAndContextBVar, "Item1")),
                            Expression.Assign(contextBVar, Expression.Field(valueOptionAndContextBVar, "Item2")),
                            Expression.Condition(
                                Expression.Equal(Expression.Field(valueOptionBVar, "isSome"), Expression.Constant(true)),
                                MkTuple<Option<IEnumerable<V>>, RuleExprContext>(
                                    WrapSome<IEnumerable<V>>(Expression.Invoke(projector, Expression.Field(valueOptionAVar, "value"), Expression.Field(valueOptionBVar, "value"))),
                                    contextBVar
                                ),
                                MkTuple<Option<IEnumerable<V>>, RuleExprContext>(GetNoneValue<IEnumerable<V>>(), contextBVar)
                            )
                        ),
                        MkTuple<Option<IEnumerable<V>>, RuleExprContext>(GetNoneValue<IEnumerable<V>>(), contextAVar)
                    )
                );

            var functionBody =
                Expression.Block(
                    new[] { valueOptionAndContextAVar, valueOptionAVar, contextAVar, valueOptionAndContextBVar, valueOptionBVar, contextBVar },
                    functionImplementation
                );
            var function = Expression.Lambda<RuleExpr<IEnumerable<V>, RuleExprContext>>(functionBody, context);
            return new RuleExprAst<IEnumerable<V>, RuleExprContext> { Expression = function };

            //return context =>
            //{
            //    var (a, context1) = expr(context0);
            //    var fs = selector(a);                    
            //    var (bs, contextn) = (sequence fs)(context1)      // sequence :: IEnumerable<RuleExprAst<U>> -> RuleExprAst<IEnumerable<U>>
            //    return (projector(a, bs), contextn);
            //};
        }

        private static Expression Sequence<T, RuleExprContext>(Expression fs)
        {
            return Expression.Invoke(MkSequencer<T, RuleExprContext>(), fs);
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