using System;
using System.Collections.Immutable;

namespace ce_toy_cs
{
    class Program
    {
        static void Main(string[] args)
        {
            var process = Process.GetProcess();

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

            Console.WriteLine(result.Item1);
        }
    }
}
