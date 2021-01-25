using ce_toy_cs;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ce_toy_cs.Framework
{
    static class SDsl
    {
        public static RuleExprAst<Credit, SRuleExprContext> GetCredit()
        {
            throw new NotImplementedException();
        }

        public static RuleExprAst<int, SRuleExprContext> GetAmount()
        {
            return
                new RuleExprAst<int, SRuleExprContext>
                {
                    Expression = context => new Tuple<Option<int>, SRuleExprContext>(Option<int>.Some(context.Amount), context).ToValueTuple()
                };
        }

        public static RuleExprAst<T, SRuleExprContext> GetValue<T>(string key)
        {
            return
                new RuleExprAst<T, SRuleExprContext>
                {
                    Expression = context => GetValueImpl<T>(key)(context)
                };
        }

        private static RuleExpr<T, SRuleExprContext> GetValueImpl<T>(string key)
        {

            return context =>
            {
                var applicant = context.Applicant;
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
                    Applicant = applicant with
                    {
                        Loaders = applicant.Loaders.Skip(1),
                        KeyValueMap = applicant.Loaders.First().Load(applicant.Id, key, applicant.KeyValueMap)
                    }
                };

                return GetValueImpl<T>(key)(newContext);
            };
        }
    }

    static class MDsl
    {
        public static RuleExprAst<Credit, MRuleExprContext> GetCredit()
        {
            throw new NotImplementedException();
        }

        public static RuleExprAst<int, MRuleExprContext> GetAmount()
        {
            return
                new RuleExprAst<int, MRuleExprContext>
                {
                    Expression = context => new Tuple<Option<int>, MRuleExprContext>(Option<int>.Some(context.Amount), context).ToValueTuple()
                };
        }

        public static RuleExprAst<IEnumerable<T>, MRuleExprContext> GetValues<T>(string key)
        {
            return GetValues<T>(key, x => true);
        }

        public static RuleExprAst<IEnumerable<T>, MRuleExprContext> GetValues<T>(string key, Predicate<Applicant> predicate)
        {
            return
                from applicants in GetApplicants()
                from values in from applicantId in applicants.Keys where predicate(applicants[applicantId]) select GetValue<T>(applicantId, key)
                select values;
        }

        private static RuleExprAst<T, MRuleExprContext> GetValue<T>(string applicantId, string key)
        {
            return
                new RuleExprAst<T, MRuleExprContext>
                {
                    Expression = context => GetValueImpl<T>(applicantId, key)(context)
                };
        }

        public static RuleExprAst<ImmutableDictionary<string, Applicant>, MRuleExprContext> GetApplicants()
        {
            return
                new RuleExprAst<ImmutableDictionary<string, Applicant>, MRuleExprContext>
                {
                    Expression = context => new Tuple<Option<ImmutableDictionary<string, Applicant>>, MRuleExprContext>(Option<ImmutableDictionary<string, Applicant>>.Some(context.Applicants), context).ToValueTuple()
                };
        }

        private static RuleExpr<T, MRuleExprContext> GetValueImpl<T>(string applicantId, string key)
        {

            return context =>
            {
                if (!context.Applicants.TryGetValue(applicantId, out var applicant))
                    throw new Exception($"Applicant {applicantId} not found");

                if (applicant.KeyValueMap.TryGetValue(key, out var value))
                {
                    if (!(value is T))
                        throw new Exception($"Failed to retrieve value for key {key} for applicant {applicantId} due to type mismatch. Got {value.GetType().Name}, expected {typeof(T).Name}");

                    return (Option<T>.Some((T)value), context);
                }

                if (!applicant.Loaders.Any())
                    throw new Exception($"Failed to load value for key {key} for applicant {applicantId}");

                var newContext = context with
                {
                    Applicants = context.Applicants.SetItem(applicantId, applicant with
                    {
                        Loaders = applicant.Loaders.Skip(1),
                        KeyValueMap = applicant.Loaders.First().Load(applicant.Id, key, applicant.KeyValueMap)
                    })
                };

                return GetValueImpl<T>(applicantId, key)(newContext);
            };
        }
    }
}
