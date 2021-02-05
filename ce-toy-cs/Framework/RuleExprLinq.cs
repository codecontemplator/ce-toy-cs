using ce_toy_cs.Framework.Details;
using ce_toy_cs.Framework.Functional;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace ce_toy_cs.Framework
{
    static class RuleExprLinq
    {
        public static RuleExprAst<T, RuleExprContext> Wrap<T, RuleExprContext>(T value)
        {
            return new RuleExprAst<T, RuleExprContext> { Expression = context => new Tuple<Option<T>, RuleExprContext>(Option<T>.Some(value), context).ToValueTuple() };
        }

        private static Expression MkTuple<T1, T2>(Expression t1, Expression t2)
        {
            Expression<Func<T1, T2, (T1, T2)>> toTuple = (x, y) => new Tuple<T1, T2>(x, y).ToValueTuple();
            return Expression.Invoke(toTuple, t1, t2);
        }

        private static Expression WrapSome<T>(Expression value)
        {
            Expression<Func<T, Option<T>>> toSome = value => Option<T>.Some(value);
            return Expression.Invoke(toSome, value);
        }

        private static Expression GetNoneValue<T>()
        {
            Expression<Func<Option<T>>> getNoneValue = () => Option<T>.None;
            return Expression.Invoke(getNoneValue);
        }

        private static Expression CreateLogEntry(Expression messageExpr, Expression preContextExpr, Expression postContextExpr, Expression valueExpr)
        {
            Expression<Func<string, RuleExprContextBase, RuleExprContextBase, object, LogEntry>> createLogEntry = (message, preContext, postContext, value) => 
                new LogEntry { Message = message, PreContext = preContext, PostContext = postContext, Value = value };
            return Expression.Invoke(createLogEntry, messageExpr, preContextExpr, postContextExpr, valueExpr);
        }

        public static RuleExprAst<Option<T>, RuleExprContext> ExceptionContext<T, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");

            var valueOptionAndContextAVar = Expression.Variable(typeof((Option<T>, RuleExprContext)), "valueOptionAndContextAVar");
            var valueOptionAVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            var contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

            var functionImplementation =
                Expression.Block(
                    Expression.Assign(valueOptionAndContextAVar, Expression.Invoke(expr.Expression, context)),
                    Expression.Assign(valueOptionAVar, Expression.Field(valueOptionAndContextAVar, "Item1")),
                    Expression.Assign(contextAVar, Expression.Field(valueOptionAndContextAVar, "Item2")),
                    MkTuple<Option<Option<T>>, RuleExprContext>(
                        WrapSome<Option<T>>(valueOptionAVar),
                        contextAVar
                    )
                );

            var functionBody =
                Expression.Block(
                    new[] { valueOptionAndContextAVar, valueOptionAVar, contextAVar },
                    functionImplementation
                );

            var function = Expression.Lambda<RuleExpr<Option<T>, RuleExprContext>>(functionBody, context);

            return new RuleExprAst<Option<T>, RuleExprContext> { Expression = function };
        }

        // Previously known as WithLogging
        public static RuleExprAst<T, RuleExprContext> LogContext<T, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, string message)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");

            var valueOptionAndContextAVar = Expression.Variable(typeof((Option<T>, RuleExprContext)), "valueOptionAndContextAVar");
            var valueOptionAVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            var contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

            var m = typeof(RuleExprContext).GetMethod("WithLogging");
            var functionImplementation =
                Expression.Block(
                    Expression.Assign(valueOptionAndContextAVar, Expression.Invoke(expr.Expression, context)),
                    Expression.Assign(valueOptionAVar, Expression.Field(valueOptionAndContextAVar, "Item1")),
                    Expression.Assign(contextAVar, Expression.Field(valueOptionAndContextAVar, "Item2")),
                    MkTuple<Option<T>, RuleExprContext>(
                        valueOptionAVar,
                        //Expression.Convert(
                            Expression.Call(
                                contextAVar,
                                typeof(RuleExprContext).GetMethod("WithLogging"),
                                CreateLogEntry(
                                    Expression.Constant(message),
                                    context,
                                    contextAVar,
                                    Expression.Convert(valueOptionAVar, typeof(object))
                                    )
                            )//,
                        //    typeof(RuleExprContext)
                        //)
                    )
                );

            var functionBody =
                Expression.Block(
                    new[] { valueOptionAndContextAVar, valueOptionAVar, contextAVar },
                    functionImplementation
                );

            var function = Expression.Lambda<RuleExpr<T, RuleExprContext>>(functionBody, context);

            return new RuleExprAst<T, RuleExprContext> { Expression = function };
        }

        /*
        public static RuleExprAst<Decision, RuleExprContext> Join<RuleExprContext>(this RuleExprAst<Decision, RuleExprContext> expr, RuleExprAst<Decision, RuleExprContext> exprNext) where RuleExprContext : IRuleExprContext
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");

            var valueOptionAndContextAVar = Expression.Variable(typeof((Option<Decision>, RuleExprContext)), "valueOptionAndContextAVar");
            var valueOptionAVar = Expression.Variable(typeof(Option<Decision>), "valueOptionAVar");
            var contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

            var functionImplementation =
                Expression.Block(
                    Expression.Assign(valueOptionAndContextAVar, Expression.Invoke(expr.Expression, context)),
                    Expression.Assign(valueOptionAVar, Expression.Field(valueOptionAndContextAVar, "Item1")),
                    Expression.Assign(contextAVar, Expression.Field(valueOptionAndContextAVar, "Item2")),
                    Expression.Condition(
                        Expression.AndAlso(
                            Expression.Equal(Expression.Field(valueOptionAVar, "isSome"), Expression.Constant(true)),
                            Expression.Equal(Expression.Field(valueOptionAVar, "value"), Expression.Constant(Decision.Reject))),
                        valueOptionAndContextAVar,
                        Expression.Invoke(
                            exprNext.Expression,
                            Expression.Convert(
                                Expression.Call(
                                    contextAVar,
                                    typeof(IRuleExprContext).GetMethod("WithNewAmount"),
                                    Expression.Condition(
                                        Expression.AndAlso(
                                            Expression.Equal(Expression.Field(valueOptionAVar, "isSome"), Expression.Constant(true)),
                                            Expression.Equal(Expression.Property(Expression.Field(valueOptionAVar, "value"), "Type"), Expression.Constant(DecisionType.AcceptLoweredAmount))),
                                        Expression.Property(Expression.Property(Expression.Field(valueOptionAVar, "value"), "Amount"), "Value"),
                                        Expression.Property(contextAVar, "Amount")
                                    )
                                ),
                                typeof(RuleExprContext)
                            )
                        )
                    )
                );

            var functionBody =
                Expression.Block(
                    new[] { valueOptionAndContextAVar, valueOptionAVar, contextAVar },
                    functionImplementation
                );

            var function = Expression.Lambda<RuleExpr<Decision, RuleExprContext>>(functionBody, context);

            return new RuleExprAst<Decision, RuleExprContext> { Expression = function };
        }
        */

        public static RuleExprAst<U, RuleExprContext> Select<T, U, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T, U>> convert)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");

            var valueOptionAndContextAVar = Expression.Variable(typeof((Option<T>, RuleExprContext)), "valueOptionAndContextAVar");
            var valueOptionAVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            var contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

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

            var valueOptionAndContextAVar = Expression.Variable(typeof((Option<T>, RuleExprContext)), "valueOptionAndContextAVar");
            var valueOptionAVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            var contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

            var valueOptionAndContextBVar = Expression.Variable(typeof((Option<U>, RuleExprContext)), "valueOptionAndContextBVar");
            var valueOptionBVar = Expression.Variable(typeof(Option<U>), "valueOptionBVar");
            var contextBVar = Expression.Variable(typeof(RuleExprContext), "contextBVar");

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
                                MkTuple<Option<V>, RuleExprContext>(
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
                    new[] { valueOptionAndContextAVar, valueOptionAVar, contextAVar, valueOptionAndContextBVar, valueOptionBVar, contextBVar },
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

            var valueOptionAndContextAVar = Expression.Variable(typeof((Option<T>, RuleExprContext)), "valueOptionAndContextAVar");
            var valueOptionAVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            var contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

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

            var valueOptionAndContextAVar = Expression.Variable(typeof((Option<T>, RuleExprContext)), "valueOptionAndContextAVar");
            var valueOptionAVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            var contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

            var valueOptionAndContextBVar = Expression.Variable(typeof((Option<ImmutableList<U>>, RuleExprContext)), "valueOptionAndContextBVar");
            var valueOptionBVar = Expression.Variable(typeof(Option<ImmutableList<U>>), "valueOptionBVar");
            var contextBVar = Expression.Variable(typeof(RuleExprContext), "contextBVar");

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
                                    Sequence<U, RuleExprContext>(Expression.Invoke(selector, Expression.Field(valueOptionAVar, "value"))),
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

        // Argument type: Expression<IEnumerable<RuleExprAst<T, RuleExprContext>>>
        // Return type: Expression<RuleExpr<ImmutableList<T>, RuleExprContext>>   -- note: RuleExpr not RuleExprAst
        private static Expression Sequence<T, RuleExprContext>(Expression fsVar)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");

            var valueOptionAndContextVar = Expression.Variable(typeof((Option<T>, RuleExprContext)), "valueOptionAndContextAVar");
            var valueOptionVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            var contextVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");
            var values = Expression.Variable(typeof(ImmutableList<T>));
            var abortedVar = Expression.Variable(typeof(bool));
            var fVar = Expression.Parameter(typeof(RuleExprAst<T, RuleExprContext>), "f(loopVar)");
            var breakLabel = Expression.Label("LoopBreak");
            var loopBody = Expression.Block(
                    Expression.Assign(valueOptionAndContextVar, Expression.Invoke(Expression.Property(fVar, "Expression"), contextVar)),
                    Expression.Assign(valueOptionVar, Expression.Field(valueOptionAndContextVar, "Item1")),
                    Expression.Assign(contextVar, Expression.Field(valueOptionAndContextVar, "Item2")),
                    Expression.IfThenElse(
                        Expression.Equal(Expression.Field(valueOptionVar, "isSome"), Expression.Constant(true)),
                        Expression.Assign(values, Expression.Call(values, typeof(ImmutableList<>).MakeGenericType(typeof(T)).GetMethod("Add"), Expression.Field(valueOptionVar, "value"))),
                        Expression.Block(
                            Expression.Assign(abortedVar, Expression.Constant(true)),
                            Expression.Break(breakLabel)
                        )
                    )
                );
            var loop = ExpressionEx.ForEach(fsVar, fVar, loopBody, breakLabel);

            var functionBody =
                Expression.Block(
                    new[] { valueOptionAndContextVar, valueOptionVar, contextVar, values, abortedVar },
                    Expression.Assign(contextVar, context),
                    Expression.Assign(values, Expression.Constant(ImmutableList<T>.Empty)),
                    Expression.Assign(abortedVar, Expression.Constant(false)),
                    loop,
                    Expression.Condition(
                        Expression.Equal(abortedVar, Expression.Constant(false)),
                        MkTuple<Option<ImmutableList<T>>, RuleExprContext>(WrapSome<ImmutableList<T>>(values), contextVar),
                        MkTuple<Option<ImmutableList<T>>, RuleExprContext>(GetNoneValue<ImmutableList<T>>(), contextVar)
                    )
                );

            return Expression.Lambda<RuleExpr<ImmutableList<T>, RuleExprContext>>(functionBody, context);

            // context =>
            //   var as = []
            //   var aborted = false;
            //   foreach(var f in fs) 
            //   {
            //      (a, context') = f(contex);
            //      context = context';
            //      if (!a) { aborted = true; break; }
            //      as.add(a);
            //   }
            //   return aborted ? (None, context) : (Just as, context)
        }

        public static RuleExprAst<Unit, RuleExprContext> Join<RuleExprContext>(this RuleExprAst<Unit, RuleExprContext> expr, RuleExprAst<Unit, RuleExprContext> exprNext)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");

            var valueOptionAndContextAVar = Expression.Variable(typeof((Option<Unit>, RuleExprContext)), "valueOptionAndContextAVar");
            var valueOptionAVar = Expression.Variable(typeof(Option<Unit>), "valueOptionAVar");
            var contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");

            var functionImplementation =
                Expression.Block(
                    Expression.Assign(valueOptionAndContextAVar, Expression.Invoke(expr.Expression, context)),
                    Expression.Assign(valueOptionAVar, Expression.Field(valueOptionAndContextAVar, "Item1")),
                    Expression.Assign(contextAVar, Expression.Field(valueOptionAndContextAVar, "Item2")),
                    Expression.Condition(
                        Expression.Equal(Expression.Field(valueOptionAVar, "isSome"), Expression.Constant(true)),
                        Expression.Invoke(exprNext.Expression, contextAVar),
                        MkTuple<Option<Unit>, RuleExprContext>(
                            GetNoneValue<Unit>(),
                            contextAVar
                        )
                    )
                );

            var functionBody =
                Expression.Block(
                    new[] { valueOptionAndContextAVar, valueOptionAVar, contextAVar },
                    functionImplementation
                );

            var function = Expression.Lambda<RuleExpr<Unit, RuleExprContext>>(functionBody, context);
            return new RuleExprAst<Unit, RuleExprContext> { Expression = function };
        }

        public static RuleExprAst<Unit, RuleExprContext> Apply<T, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr) where T : IRuleContextApplicable
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");

            var valueOptionAndContextAVar = Expression.Variable(typeof((Option<T>, RuleExprContext)), "valueOptionAndContextAVar");
            var valueOptionAVar = Expression.Variable(typeof(Option<T>), "valueOptionAVar");
            var contextAVar = Expression.Variable(typeof(RuleExprContext), "contextAVar");
            
            var functionImplementation =
                Expression.Block(
                    Expression.Assign(valueOptionAndContextAVar, Expression.Invoke(expr.Expression, context)), 
                    Expression.Assign(valueOptionAVar, Expression.Field(valueOptionAndContextAVar, "Item1")),
                    Expression.Assign(contextAVar, Expression.Field(valueOptionAndContextAVar, "Item2")),
                    Expression.Condition(
                        Expression.Equal(Expression.Field(valueOptionAVar, "isSome"), Expression.Constant(true)),
                        MkTuple<Option<Unit>, RuleExprContext>(
                            WrapSome<Unit>(Expression.Constant(Unit.Value)),
                            Expression.Call(
                                Expression.Field(valueOptionAVar, "value"),
                                typeof(IRuleContextApplicable).GetMethod("ApplyTo").MakeGenericMethod(typeof(RuleExprContext)),
                                contextAVar
                            )
                        ),
                        MkTuple<Option<Unit>, RuleExprContext>(
                            GetNoneValue<Unit>(),
                            contextAVar
                        )
                    )
                );

            var functionBody =
                Expression.Block(
                    new[] { valueOptionAndContextAVar, valueOptionAVar, contextAVar },
                    functionImplementation
                );

            var function = Expression.Lambda<RuleExpr<Unit, RuleExprContext>>(functionBody, context);
            return new RuleExprAst<Unit, RuleExprContext> { Expression = function };
        }

        public static RuleExprAst<Unit, RuleExprContext> Join<RuleExprContext>(this IEnumerable<RuleExprAst<Unit, RuleExprContext>> ruleExprAsts)
        {
            var result = ruleExprAsts.First();
            foreach (var next in ruleExprAsts.Skip(1))
                result = result.Join(next);
            return result;
        }

        public static RuleExprAst<Unit, RuleExprContext> RejectIf<T,RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Expression<Func<T,bool>> predicate, string message)
        {
            return expr.Where(predicate).Select(_ => PassUnit.Value).Apply().LogContext(message);
        }
    }
}