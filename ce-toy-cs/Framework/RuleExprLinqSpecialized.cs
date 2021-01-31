using ce_toy_cs.Framework.Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static ce_toy_cs.Framework.Details.ExpressionEx;
using static ce_toy_cs.Framework.DslSpecialized;

namespace ce_toy_cs.Framework
{

    public static class RuleExprLinqSpecialized
    {
        public static RuleExprAst<UnitType, RuleExprContext<Selector>> Apply<Selector, UnitType>(this RuleExprAst<Result, RuleExprContext<Selector>> expr)
        {
            var context = Expression.Parameter(typeof(RuleExprContext<Selector>), "context");

            var valueOptionAndContextAVar = Expression.Variable(typeof((Option<Result>, RuleExprContext<Selector>)), "valueOptionAndContextAVar");
            var valueOptionAVar = Expression.Variable(typeof(Option<Result>), "valueOptionAVar");
            var contextAVar = Expression.Variable(typeof(RuleExprContext<Selector>), "contextAVar");

            var valueProp = typeof(UnitType).GetProperty("Value", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var functionImplementation =
                Expression.Block(
                    Expression.Assign(valueOptionAndContextAVar, Expression.Invoke(expr.Expression, context)),
                    Expression.Assign(valueOptionAVar, Expression.Field(valueOptionAndContextAVar, "Item1")),
                    Expression.Assign(contextAVar, Expression.Field(valueOptionAndContextAVar, "Item2")),
                    Expression.Condition(
                        Expression.Equal(Expression.Field(valueOptionAVar, "isSome"), Expression.Constant(true)),
                        MkTuple<Option<Unit>, RuleExprContext<Selector>>(
                            WrapSome<Unit>(Expression.Property(null, valueProp)),
                            Expression.Call(
                                Expression.Field(valueOptionAVar, "value"),
                                typeof(Result).GetMethod("Apply").MakeGenericMethod(typeof(Selector)),
                                contextAVar
                            )
                        ),
                        MkTuple<Option<Unit>, RuleExprContext<Selector>>(
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

            var function = Expression.Lambda<RuleExpr<UnitType, RuleExprContext<Selector>>>(functionBody, context);
            return new RuleExprAst<UnitType, RuleExprContext<Selector>> { Expression = function };
        }

        public static RuleExprAst<T2, RuleExprContext<Selector>> Join<Selector, T1, T2>(this RuleExprAst<T1, RuleExprContext<Selector>> expr, RuleExprAst<T2, RuleExprContext<Selector>> exprNext)  // T1 must be applicalbe
        {
            return expr.Apply<Selector, Unit>().SelectMany(_ => exprNext, (_,a) => a);
            //var context = Expression.Parameter(typeof(RuleExprContext<Selector>), "context");

            //var valueOptionAndContextAVar = Expression.Variable(typeof((Option<T1>, RuleExprContext<Selector>)), "valueOptionAndContextAVar");
            //var valueOptionAVar = Expression.Variable(typeof(Option<T1>), "valueOptionAVar");
            //var contextAVar = Expression.Variable(typeof(RuleExprContext<Selector>), "contextAVar");
            
            //var functionImplementation =
            //    Expression.Block(
            //        Expression.Assign(valueOptionAndContextAVar, Expression.Invoke(expr.Expression, context)),
            //        Expression.Assign(valueOptionAVar, Expression.Field(valueOptionAndContextAVar, "Item1")),
            //        Expression.Assign(contextAVar, Expression.Field(valueOptionAndContextAVar, "Item2")),
            //        Expression.Condition(
            //            Expression.Equal(Expression.Field(valueOptionAVar, "isSome"), Expression.Constant(true)),
            //            Expression.Invoke(exprNext.Expression,
            //                Expression.Call(
            //                    Expression.Field(valueOptionAVar, "value"),
            //                    typeof(T1).GetMethod("Apply").MakeGenericMethod(typeof(Selector)),
            //                    contextAVar
            //                )
            //            ),
            //            MkTuple<Option<T2>, RuleExprContext<Selector>>(
            //                GetNoneValue<T1>(),
            //                contextAVar
            //            )
            //        )
            //    );

            //var functionBody =
            //    Expression.Block(
            //        new[] { valueOptionAndContextAVar, valueOptionAVar, contextAVar },
            //        functionImplementation
            //    );

            //var function = Expression.Lambda<RuleExpr<T2, RuleExprContext<Selector>>>(functionBody, context);
            //return new RuleExprAst<T2, RuleExprContext<Selector>> { Expression = function };
        }

        public static RuleExprAst<UnitType, RuleExprContext<Selector>> Join<Selector, UnitType, T>(this IEnumerable<RuleExprAst<T, RuleExprContext<Selector>>> ruleExprAsts)
        {
            RuleExprAst<T, RuleExprContext<Selector>> result = ruleExprAsts.First();
            foreach (var next in ruleExprAsts.Skip(1))
                result = result.Join(next);
            return result.Apply<Selector,UnitType>();
        }

        //public static RuleExprAst<UnitType, RuleExprContext<Selector>> ApplicativeJoin<Selector, UnitType>(this IEnumerable<RuleExprAst<Result, RuleExprContext<Selector>>> ruleExprAsts)
        //{
        //    var result = ruleExprAsts.First();
        //    foreach (var next in ruleExprAsts.Skip(1))
        //        result = result.Apply<Selector, UnitType>().Join(next);
        //    return result.Apply<Selector, UnitType>();
        //}

        public static RuleExprAst<PolicyRejectType, RuleExprContext> RejectPolicy<T, RuleExprContext>(this RuleExprAst<T, RuleExprContext> expr, Func<T, bool> predicate, string message)
        {
            return expr.Where(x => !predicate(x)).Select(_ => PolicyRejectType.Value).LogContext(message);
        }
    }
}
