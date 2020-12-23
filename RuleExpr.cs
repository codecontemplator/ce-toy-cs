using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    public delegate (T, RuleExprContext) RuleExpr<T>(RuleExprContext input);

    public record Applicant
    {
        public IEnumerable<ILoader> Loaders { get; init; }
        public ImmutableDictionary<string, int> KeyValueMap { get; init; }
    }

    public record RuleExprContext
    {
        public int Amount { get; init; }
        public ImmutableDictionary<string, Applicant> Applicants { get; init; }
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

        public static RuleExprAst<int> GetValue(string applicantId, string key)
        {
            return
                new RuleExprAst<int>
                {
                    Expression = context => GetValueImpl(applicantId, key)(context)
                };
        }

        public static RuleExprAst<ImmutableDictionary<string, Applicant>> GetApplicants()
        {
            return
                new RuleExprAst<ImmutableDictionary<string, Applicant>>
                {
                    Expression = (context => new Tuple<ImmutableDictionary<string, Applicant>, RuleExprContext>(context.Applicants, context).ToValueTuple())
                };
        }

        public static RuleExprAst<IEnumerable<int>> GetValues(string key)
        {
            return
                from applicants in GetApplicants()
                from values in (from applicantId in applicants.Keys select GetValue(applicantId, key))
                select values;
        }

        private static RuleExpr<int> GetValueImpl(string applicantId, string key)
        {

            return context =>
            {
                if (!context.Applicants.TryGetValue(applicantId, out var applicant))
                    throw new Exception($"Applicant {applicantId} not found");

                if (applicant.KeyValueMap.TryGetValue(key, out var value))
                    return (value, context);

                if (!applicant.Loaders.Any())
                    throw new Exception($"Failed to load value for key {key} for applicant {applicantId}");

                var newContext = context with
                {
                    Applicants = context.Applicants.SetItem(applicantId, applicant with
                    {
                        Loaders = applicant.Loaders.Skip(1),
                        KeyValueMap = applicant.Loaders.First().Load(key, applicant.KeyValueMap)
                    })
                };

                return GetValueImpl(applicantId, key)(newContext);
            };
        }

        public static RuleExprAst<T> Wrap<T>(T value)
        {
            return new RuleExprAst<T> { Expression = context => new Tuple<T, RuleExprContext>(value, context).ToValueTuple() };
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

        public static RuleExprAst<IEnumerable<V>> SelectMany<T, U, V>(this RuleExprAst<T> expr, Expression<Func<T, IEnumerable<RuleExprAst<U>>>> selector, Expression<Func<T, IEnumerable<U>, IEnumerable<V>>> projector)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var intermediateValueAndContext = Expression.Invoke(expr.Expression, context);
            var intermediateValue = Expression.Field(intermediateValueAndContext, "Item1");
            var intermediateContext = Expression.Field(intermediateValueAndContext, "Item2");
            var selectorResult = Expression.Invoke(selector, intermediateValue);

            Expression<
                Func<
                    IEnumerable<RuleExprAst<U>>,
                    RuleExprAst<ImmutableList<U>>
                >
            > sequencer = x => Sequence(x);

            var sequencedResult = Expression.Invoke(sequencer, selectorResult);
            var sequencedResultExpression = Expression.Property(sequencedResult, "Expression");

            var finalValueAndContext = Expression.Invoke(sequencedResultExpression, context);

            var finalValue = Expression.Field(finalValueAndContext, "Item1");
            var finalContext = Expression.Field(finalValueAndContext, "Item2");
            var projectedValue = Expression.Invoke(projector, intermediateValue, finalValue);

            var tupleConstructor = typeof(Tuple<IEnumerable<V>, RuleExprContext>).GetConstructor(new[] { typeof(IEnumerable<V>), typeof(RuleExprContext) });
            var returnTuple = Expression.New(tupleConstructor, new Expression[] { projectedValue, finalContext });
            var toValueTupleInfo = typeof(TupleExtensions).GetMethodExt("ToValueTuple", new[] { typeof(Tuple<,>) });
            var toValueTuple = toValueTupleInfo.MakeGenericMethod(typeof(IEnumerable<V>), typeof(RuleExprContext));
            var returnValueTuple = Expression.Call(null, toValueTuple, returnTuple);
            var resultFunc = Expression.Lambda<RuleExpr<IEnumerable<V>>>(returnValueTuple, context);
            return new RuleExprAst<IEnumerable<V>> { Expression = resultFunc };

            //return context =>
            //{
            //    var (a, context1) = expr(context0);
            //    var fs = selector(a);                    
            //    var (bs, contextn) = sequence fs      // sequence :: IEnumerable<RuleExprAst<U>> -> RuleExprAst<IEnumerable<U>>
            //    return (projector(a, bs), contextn);
            //};
        }

        private static RuleExprAst<ImmutableList<T>> Sequence<T>(IEnumerable<RuleExprAst<T>> x)
        {
            if (!x.Any())
                return Wrap(ImmutableList<T>.Empty);

            return x.First().SelectMany(t => Sequence(x.Skip(1)), (t, ts) => ts.Add(t));
        }
    }
}