using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
        public static RuleExpr<int> GetAmount() => context => (context.Amount, context);

        public static RuleExpr<int> GetValue(string key)
        {

            return context =>
            {
                if (context.KeyValueMap.TryGetValue(key, out var value))
                    return (value, context);

                if (!context.Loaders.Any())
                    throw new Exception("Failed to load value for key " + key);
                
                return GetValue(key)(context with
                {
                    Loaders = context.Loaders.Skip(0),
                    KeyValueMap = context.Loaders.First().Load(key, context.KeyValueMap)
                });
            };
        }

        public static RuleExpr<U> Select<T, U>(this RuleExpr<T> expr, Func<T, U> convert)
        {
            return context =>
            {
                var (a, context2) = expr(context);
                return (convert(a), context2);
            };
        }

        public static RuleExpr<V> SelectMany<T, U, V>(this RuleExpr<T> expr, Func<T, RuleExpr<U>> selector, Func<T, U, V> projector)
        {
            return context =>
            {
                var (a, context2) = expr(context);
                var (b, context3) = selector(a)(context2);
                return (projector(a, b), context3);
            };
        }
    }
}
