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

    static class Dsl
    {
        public static Expression<RuleExpr<int>> GetAmount() => context => new Tuple<int,RuleExprContext>(context.Amount, context).ToValueTuple();

        //public static Expression<RuleExpr<int>> GetValue(string key)
        //{

        //    //return context =>
        //    //{
        //    //    if (context.KeyValueMap.TryGetValue(key, out var value))
        //    //        return (value, context);

        //    //    if (!context.Loaders.Any())
        //    //        throw new Exception("Failed to load value for key " + key);

        //    //    return GetValue(key)(context with
        //    //    {
        //    //        Loaders = context.Loaders.Skip(0),
        //    //        KeyValueMap = context.Loaders.First().Load(key, context.KeyValueMap)
        //    //    });
        //    //};
        //}

        //        public static Expression<RuleExpr<U>> Select<T, U>(this Expression<RuleExpr<T>> expr, Expression<Func<T, U>> convert)

        public static Expression<RuleExpr<U>> Select<T, U>(this Expression<RuleExpr<T>> expr, Expression<Func<T, U>> convert)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var valueAndNewContext = Expression.Invoke(expr, context);
            var value = Expression.Field(valueAndNewContext, "Item1");
            var newContext = Expression.Field(valueAndNewContext, "Item2");
            var convertedValue = Expression.Invoke(convert, value);
            var returnTuple = Expression.New(typeof(Tuple<U, RuleExprContext>).GetConstructor(new[] { typeof(U), typeof(RuleExprContext) }), new Expression[] { convertedValue, newContext });
            var toValueTupleInfo = typeof(TupleExtensions).GetMethodExt("ToValueTuple", new[] { typeof(Tuple<,>) });
            var toValueTuple = toValueTupleInfo.MakeGenericMethod(typeof(U), typeof(RuleExprContext));
            var returnValueTuple = Expression.Call(null, toValueTuple, returnTuple);
            var resultFunc = Expression.Lambda<RuleExpr<U>>(returnValueTuple, context);
            return resultFunc;
            //return context =>
            //{
            //    var (a, context2) = expr(context);
            //    return (convert(a), context2);
            //};
        }

        //public static RuleExpr<V> SelectMany<T, U, V>(this Expression<RuleExpr<T>> expr, Expression<Func<T, RuleExpr<U>>> selector, Expression<Func<T, U, V>> projector)
        //{
        //    return context =>
        //    {
        //        var (a, context2) = expr(context);
        //        var (b, context3) = selector(a)(context2);
        //        return (projector(a, b), context3);
        //    };
        //}
    }
}
