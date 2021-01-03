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
        public ImmutableDictionary<string, object> KeyValueMap { get; init; }
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

        public static RuleExprAst<IEnumerable<T>> GetValues<T>(string key)
        {
            return GetValues<T>(key, x => true);
        }

        public static RuleExprAst<IEnumerable<T>> GetValues<T>(string key, Predicate<Applicant> predicate)
        {
            return
                from applicants in GetApplicants()
                from values in (from applicantId in applicants.Keys where predicate(applicants[applicantId]) select GetValue<T>(applicantId, key))
                select values;
        }

        private static RuleExprAst<T> GetValue<T>(string applicantId, string key)
        {
            return
                new RuleExprAst<T>
                {
                    Expression = context => GetValueImpl<T>(applicantId, key)(context)
                };
        }

        private static RuleExprAst<ImmutableDictionary<string, Applicant>> GetApplicants()
        {
            return
                new RuleExprAst<ImmutableDictionary<string, Applicant>>
                {
                    Expression = (context => new Tuple<ImmutableDictionary<string, Applicant>, RuleExprContext>(context.Applicants, context).ToValueTuple())
                };
        }


        private static RuleExpr<T> GetValueImpl<T>(string applicantId, string key)
        {

            return context =>
            {
                if (!context.Applicants.TryGetValue(applicantId, out var applicant))
                    throw new Exception($"Applicant {applicantId} not found");

                if (applicant.KeyValueMap.TryGetValue(key, out var value))
                {
                    if (!(value is T))
                        throw new Exception($"Failed to retrieve value for key {key} for applicant {applicantId} due to type mismatch. Got {value.GetType().Name}, expected {typeof(T).Name}");

                    return ((T)value, context);
                }

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

                return GetValueImpl<T>(applicantId, key)(newContext);
            };
        }

        private static RuleExprAst<T> Wrap<T>(T value)
        {
            return new RuleExprAst<T> { Expression = context => new Tuple<T, RuleExprContext>(value, context).ToValueTuple() };
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

        public static RuleExprAst<U> Select<T, U>(this RuleExprAst<T> expr, Expression<Func<T, U>> convert)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var valueAndNewContext = Expression.Invoke(expr.Expression, context);
            var value = Expression.Field(valueAndNewContext, "Item1");
            var newContext = Expression.Field(valueAndNewContext, "Item2");
            var convertedValue = Expression.Invoke(convert, value);
            var returnValueTuple = MkTuple<U, RuleExprContext>(convertedValue, newContext);
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

            var returnValueTuple = MkTuple<V, RuleExprContext>(projectedValue, finalContext);
            var resultFunc = Expression.Lambda<RuleExpr<V>>(returnValueTuple, context);
            return new RuleExprAst<V> { Expression = resultFunc };

            //return context =>
            //{
            //    var (a, context2) = expr(context);
            //    var (b, context3) = selector(a)(context2);
            //    return (projector(a, b), context3);
            //};
        }

        private static RuleExprAst<IEnumerable<V>> SelectMany<T, U, V>(this RuleExprAst<T> expr, Expression<Func<T, IEnumerable<RuleExprAst<U>>>> selector, Expression<Func<T, IEnumerable<U>, IEnumerable<V>>> projector)
        {
            var context = Expression.Parameter(typeof(RuleExprContext), "context");
            var intermediateValueAndContext = Expression.Invoke(expr.Expression, context);
            var intermediateValue = Expression.Field(intermediateValueAndContext, "Item1");
            var intermediateContext = Expression.Field(intermediateValueAndContext, "Item2");
            var selectorResult = Expression.Invoke(selector, intermediateValue);

            var sequencer = MkSequencer<U>();
            var sequencedResult = Expression.Invoke(sequencer, selectorResult);
            var sequencedResultExpression = Expression.Property(sequencedResult, "Expression");

            var finalValueAndContext = Expression.Invoke(sequencedResultExpression, intermediateContext);

            var finalValue = Expression.Field(finalValueAndContext, "Item1");
            var finalContext = Expression.Field(finalValueAndContext, "Item2");
            var projectedValue = Expression.Invoke(projector, intermediateValue, finalValue);

            var returnValueTuple = MkTuple<IEnumerable<V>, RuleExprContext>(projectedValue, finalContext);
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

        private delegate RuleExprAst<ImmutableList<T>> SequencerDelegate<T>(IEnumerable<RuleExprAst<T>> input);

        private static Expression<SequencerDelegate<U>> MkSequencer<U>()
        {
            Expression<
                Func<
                    Func<IEnumerable<RuleExprAst<U>>,RuleExprAst<ImmutableList<U>>>,
                    Func<IEnumerable<RuleExprAst<U>>,RuleExprAst<ImmutableList<U>>>
                >
            > sequenceNonRecursive = f => xs => 
                    !xs.Any() 
                        ?
                            Wrap(ImmutableList<U>.Empty) 
                        :
                            xs.First().SelectMany(t => f(xs.Skip(1)), (t, ts) => ts.Add(t));

            var fixExpression = YCombinator<IEnumerable<RuleExprAst<U>>, RuleExprAst<ImmutableList<U>>>.Fix;
            var sequence = Expression.Invoke(fixExpression, sequenceNonRecursive);
             
            var arg = Expression.Parameter(typeof(IEnumerable<RuleExprAst<U>>), "arg");
            return Expression.Lambda<SequencerDelegate<U>>(Expression.Invoke(sequence, arg), arg);
        }
    }
}