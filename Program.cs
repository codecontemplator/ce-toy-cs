using System;
using System.Collections.Immutable;

namespace ce_toy_cs
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = Process.AbsoluteMaxAmount(1000);
            Console.WriteLine(x.ToString());
            var xx = x.Compile();
            var xxx = xx(new RuleExprContext()
            {
                Amount = 1500
            });


            var z = Process.MaxTotalDebt(1000);
            Console.WriteLine(x.ToString());
            var zz = z.Compile();

            var builder = ImmutableDictionary.CreateBuilder<string, int>();
            builder.Add("CreditA", 100);
            builder.Add("CreditB", 2000);

            var zzz = zz(new RuleExprContext()
            {
                Amount = 1500,
                KeyValueMap = builder.ToImmutable(),
                Loaders = ImmutableList<ILoader>.Empty,
            });

            //var process = Process.GetProcess();

            //var builder = ImmutableDictionary.CreateBuilder<string, int>();
            //builder.Add("CreditA", 100);
            //builder.Add("CreditB", 2);

            //var result = process.Eval(new RuleContext
            //{
            //    Log = ImmutableList<RuleLogEntry>.Empty,
            //    RuleExprContext = new RuleExprContext
            //    {
            //        Amount = 70,
            //        Loaders = ImmutableList<ILoader>.Empty,
            //        KeyValueMap = builder.ToImmutable()
            //    }
            //});

            //Console.WriteLine(result.Item1);
        }
    }
}
