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
                RuleExprContext = new RuleExprContext
                {
                    Amount = 70,
                    Applicants = new Dictionary<string, Applicant>()
                    {
                        {
                            "applicant1",
                            new Applicant
                            {
                                KeyValueMap = new Dictionary<string, object>
                                {
                                    { "CreditA", 100.0 },
                                    { "CreditB", 2.2 },
                                    { "Salary", 10 },
                                }.ToImmutableDictionary(),
                                Loaders = ImmutableList<ILoader>.Empty
                            }
                        },
                        {
                            "applicant2",
                            new Applicant
                            {
                                KeyValueMap = new Dictionary<string, object>
                                {
                                    { "CreditA", 10.2 },
                                    { "CreditB", 0.0 },
                                    { "Salary", 41 },
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
