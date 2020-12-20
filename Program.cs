using System;
using System.Collections.Immutable;

namespace ce_toy_cs
{
    class Program
    {
        static void Main(string[] args)
        {
            var process = Process.GetProcess();

            Console.WriteLine($"Process uses keys: {string.Join(',', process.GetKeys())}");
            var builder = ImmutableDictionary.CreateBuilder<string, int>();
            builder.Add("CreditA", 100);
            builder.Add("CreditB", 2);

            var result = process.Eval(new RuleContext
            {
                Log = ImmutableList<RuleLogEntry>.Empty,
                RuleExprContext = new RuleExprContext
                {
                    Amount = 70,
                    Loaders = ImmutableList<ILoader>.Empty,
                    KeyValueMap = builder.ToImmutable()
                }
            });

            Console.WriteLine($"Evaluation result: {result.Item1}");
            Console.WriteLine($"Evaluation log: {string.Join(',',result.Item2.Log)}");
        }
    }
}
