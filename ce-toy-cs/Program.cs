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

            var loaders = new ILoader[] { new CreitLoader() }.ToImmutableList();

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
                                    { "Salary", 10 },
                                    { "Role", "Primary" },
                                    { "Address", "Street 1" }
                                }.ToImmutableDictionary(),
                                Loaders = loaders
                            }
                        },
                        {
                            "applicant2",
                            new Applicant
                            {
                                Id = "applicant2",
                                KeyValueMap = new Dictionary<string, object>
                                {
                                    { "Salary", 41 },
                                    { "Role", "Secondary" },
                                    { "Address", "" }
                                }.ToImmutableDictionary(),
                                Loaders = loaders
                            }
                        }
                    }.ToImmutableDictionary(),
                }
            });

            Console.WriteLine($"Evaluation result (granted amount): {result.Item1}");
            Console.WriteLine($"Evaluation log: {string.Join(',',result.Item2.Log)}");
        }
    }

    class CreitLoader : ILoader
    {
        public string Name => "CreditLoader";

        public int Cost => 2;

        public IImmutableSet<string> Keys => new[] { "CreditA", "CreditB" }.ToImmutableHashSet();

        public ImmutableDictionary<string, object> Load(string applicantId, string key, ImmutableDictionary<string, object> input)
        {
            return
                applicantId switch
                {
                    "applicant1" =>
                        input.AddRange(new KeyValuePair<string, object>[]
                        {
                            new KeyValuePair<string, object>("CreditA", 20.0),
                            new KeyValuePair<string, object>("CreditB", 29.0)
                        }),
                    "applicant2" =>
                        input.AddRange(new KeyValuePair<string, object>[]
                        {
                            new KeyValuePair<string, object>("CreditA", 10.0),
                            new KeyValuePair<string, object>("CreditB", 39.0)
                        }),
                    _ => throw new KeyNotFoundException($"No data for applicant {applicantId}")
                };
        }
    }
}
    