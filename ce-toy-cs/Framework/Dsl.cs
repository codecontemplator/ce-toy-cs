using ce_toy_cs.Framework.Functional;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ce_toy_cs.Framework
{
    static class Dsl
    {
        public static RuleExprAst<int, RuleExprContext<Selector>> GetAmount<Selector>()
        {
            return
                new RuleExprAst<int, RuleExprContext<Selector>>
                {
                    Expression = context => new Tuple<Option<int>, RuleExprContext<Selector>>(Option<int>.Some(context.Amount), context).ToValueTuple()
                };
        }

        public static RuleExprAst<T, RuleExprContext<string>> GetValue<T>(string key)
        {
            return
                new RuleExprAst<T, RuleExprContext<string>>
                {
                    Expression = context => GetValueImpl<T>(key)(context)
                };
        }

        private static RuleExpr<T, RuleExprContext<string>> GetValueImpl<T>(string key)
        {
            return context =>
            {
                var applicant = context.Applicants[context.Selector];
                if (applicant.KeyValueMap.TryGetValue(key, out var value))
                {
                    if (!(value is T))
                        throw new Exception($"Failed to retrieve value for key {key} for applicant {applicant.Id} due to type mismatch. Got {value.GetType().Name}, expected {typeof(T).Name}");

                    return (Option<T>.Some((T)value), context);
                }

                if (!applicant.Loaders.Any())
                    throw new Exception($"Failed to load value for key {key} for applicant {applicant.Id}");

                var newContext = context with
                {
                    Applicants = context.Applicants.SetItem(context.Selector, applicant with
                    {
                        Loaders = applicant.Loaders.Skip(1),
                        KeyValueMap = applicant.Loaders.First().Load(applicant.Id, key, applicant.KeyValueMap)
                    })
                };

                return GetValueImpl<T>(key)(newContext);
            };
        }

        public static RuleExprAst<IEnumerable<T>, RuleExprContext<Unit>> GetValues<T>(string key)
        {
            return
                new RuleExprAst<IEnumerable<T>, RuleExprContext<Unit>>
                {
                    Expression = context => GetValuesImpl<T>(key)(context)
                };
        }

        private static RuleExpr<IEnumerable<T>, RuleExprContext<Unit>> GetValuesImpl<T>(string key)
        {
            return context =>
            {
                var result = new List<T>();
                var scontext = context.WithSelector<string>(null);
                foreach (var applicantId in context.Applicants.Keys)
                {
                    scontext = scontext.WithSelector(applicantId);
                    var (maybeValue, newSContext) = GetValueImpl<T>(key)(scontext);
                    if (!maybeValue.IsSome(out var value))
                        throw new Exception("Internal error. GetValue should never return nothing (it should throw instead).");
                    result.Add(value);
                }

                return (Option<IEnumerable<T>>.Some(result), scontext.WithSelector(Unit.Value));
            };
        }
    }
}
