using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    public delegate (T, RuleExprContext) RuleExpr<T>(RuleExprContext input);

    public record RuleExprContext
    {
        public int Amount { get; init; }
        public IEnumerable<ILoader> Loaders { get; init; }
        public ImmutableDictionary<string,int> KeyValueMap { get; init; }
    }

    public record RuleExprAst<T>
    {
        public Expression<RuleExpr<T>> Expression { get; init; }
        public RuleExpr<T> Compile() => Expression.Compile();
        public IEnumerable<string> GetKeys()
        {
            var findKeysVisitor = new FindKeysVisistor();
            findKeysVisitor.Visit(Expression);
            return findKeysVisitor.FoundKeys;
        }
    }

    static class Dsl
    {
        public static RuleExprAst<int> GetAmount()
        {
            return 
                new RuleExprAst<int>
                {
                    Expression = (context => new Tuple<int, RuleExprContext>(context.Amount, context).ToValueTuple())
                };
        }

        public static RuleExprAst<int> GetValue(string key)
        {
            var getValueImpl = typeof(Dsl).GetMethod("GetValueImpl", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var result = Expression.Call(getValueImpl, Expression.Constant(key));
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var resultFunc = Expression.Lambda<RuleExpr<int>>(Expression.Invoke(result, context), context);
            return new RuleExprAst<int> { Expression = resultFunc };
        }

        private static RuleExpr<int> GetValueImpl(string key)
        {

            return context =>
            {
                if (context.KeyValueMap.TryGetValue(key, out var value))
                    return (value, context);

                if (!context.Loaders.Any())
                    throw new Exception("Failed to load value for key " + key);

                return GetValueImpl(key)(context with
                {
                    Loaders = context.Loaders.Skip(0),
                    KeyValueMap = context.Loaders.First().Load(key, context.KeyValueMap)
                });
            };
        }

        public static RuleExprAst<U> Select<T, U>(this RuleExprAst<T> expr, Expression<Func<T, U>> convert)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var valueAndNewContext = Expression.Invoke(expr.Expression, context);
            var value = Expression.Field(valueAndNewContext, "Item1");
            var newContext = Expression.Field(valueAndNewContext, "Item2");
            var convertedValue = Expression.Invoke(convert, value);
            var returnTuple = Expression.New(typeof(Tuple<U, RuleExprContext>).GetConstructor(new[] { typeof(U), typeof(RuleExprContext) }), new Expression[] { convertedValue, newContext });
            var toValueTupleInfo = typeof(TupleExtensions).GetMethodExt("ToValueTuple", new[] { typeof(Tuple<,>) });
            var toValueTuple = toValueTupleInfo.MakeGenericMethod(typeof(U), typeof(RuleExprContext));
            var returnValueTuple = Expression.Call(null, toValueTuple, returnTuple);
            var resultFunc = Expression.Lambda<RuleExpr<U>>(returnValueTuple, context);
            return new RuleExprAst<U> { Expression = resultFunc };
            //return context =>
            //{
            //    var (a, context2) = expr(context);
            //    return (convert(a), context2);
            //};
        }

        public static RuleExprAst<V> SelectMany<T, U, V>(this RuleExprAst<T> expr, Expression<Func<T, RuleExprAst<U>>> selector, Expression<Func<T, U, V>> projector)
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

            var tupleConstructor = typeof(Tuple<V, RuleExprContext>).GetConstructor(new[] { typeof(V), typeof(RuleExprContext) });
            var returnTuple = Expression.New(tupleConstructor, new Expression[] { projectedValue, finalContext });
            var toValueTupleInfo = typeof(TupleExtensions).GetMethodExt("ToValueTuple", new[] { typeof(Tuple<,>) });
            var toValueTuple = toValueTupleInfo.MakeGenericMethod(typeof(V), typeof(RuleExprContext));
            var returnValueTuple = Expression.Call(null, toValueTuple, returnTuple);
            var resultFunc = Expression.Lambda<RuleExpr<V>>(returnValueTuple, context);
            return new RuleExprAst<V> { Expression = resultFunc };

            //return context =>
            //{
            //    var (a, context2) = expr(context);
            //    var (b, context3) = selector(a)(context2);
            //    return (projector(a, b), context3);
            //};
        }
    }
}
