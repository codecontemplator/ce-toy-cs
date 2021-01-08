using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ce_toy_cs
{
    class Program
    {
        static void Main(string[] args)
        {
            var process = Process.GetProcess();

            Console.WriteLine($"Process uses keys: {string.Join(',', process.GetKeys())}");

            var result = process.Eval(new RuleContext
            {
                Log = ImmutableList<RuleLogEntry>.Empty,
                RuleExprContext = new MRuleExprContext
                {
                    Amount = 170,
                    Applicants = new Dictionary<string, Applicant>()
                    {
                        {
                            "applicant1",
                            new Applicant
                            {
                                Id = "applicant1",
                                KeyValueMap = new Dictionary<string, object>
                                {
                                    { "CreditA", 20.0 },
                                    { "CreditB", 29.0 },
                                    { "Salary", 10 },
                                    { "Role", "Primary" },
                                    { "Address", "Street 1" }
                                }.ToImmutableDictionary(),
                                Loaders = ImmutableList<ILoader>.Empty
                            }
                        },
                        {
                            "applicant2",
                            new Applicant
                            {
                                Id = "applicant2",
                                KeyValueMap = new Dictionary<string, object>
                                {
                                    { "CreditA", 10.0 },
                                    { "CreditB", 39.0 },
                                    { "Salary", 41 },
                                    { "Role", "Secondary" },
                                    { "Address", "" }
                                }.ToImmutableDictionary(),
                                Loaders = ImmutableList<ILoader>.Empty
                            }
                        }
                    }.ToImmutableDictionary(),
                }
            });

            Console.WriteLine($"Evaluation result (granted amount): {result.Item1}");
            Console.WriteLine($"Evaluation log: {string.Join(',',result.Item2.Log)}");
        }
    }
}
    