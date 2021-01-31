using ce_toy_cs.Framework;
using ce_toy_cs.Framework.Functional;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace ce_toy_cs
{
    class Program
    {
        static void Main(string[] args)
        {
            var process = Process.GetProcess();

            Console.WriteLine("# Process created");
            Console.WriteLine($"Required keys: {string.Join(',', process.Keys)}");
            Console.WriteLine();

            Console.WriteLine("# Creating applicants");
            var applicants = new []
            {
                CreateApplicant("applicant1", process),
                CreateApplicant("applicant2", process),
            };
            foreach(var applicant in applicants) 
                Console.WriteLine($"{applicant.Id}: a priori keys={string.Join(',', applicant.KeyValueMap.Keys)} loaders={string.Join(',', applicant.Loaders.Select(x => x.Name))}");
            Console.WriteLine();

            var requestedAmount = 170;
            var result = process.RuleExpr(new RuleExprContext<Unit>
            {
                Log = ImmutableList<LogEntry>.Empty,
                Amount = requestedAmount,
                Applicants = applicants.ToDictionary(x => x.Id).ToImmutableDictionary()
            });

            Console.WriteLine($"# Evaluation");
            Console.WriteLine($"Requested amount: {requestedAmount}");
            Console.WriteLine($"Granted amount: {(result.Item1.isSome ? result.Item2.Amount : 0)}");
            foreach (var applicant in result.Item2.Applicants.Values)
                Console.WriteLine($"{applicant.Id}: a posteriori keys={string.Join(',', applicant.KeyValueMap.Keys)} loaders={string.Join(',', applicant.Loaders.Select(x => x.Name))}");
            Console.WriteLine();

            Console.WriteLine($"# Evaluation log");
            Console.WriteLine($"{"Message",-45} | {"Amount",10} | { "Value", 10}");
            Console.WriteLine(new string('-', 45 + 10 + 10 + 6));
            foreach(var logRow in result.Item2.Log)
            {
                Console.WriteLine($"{logRow.Message,-45} | { logRow.PreContext.Amount, 10} | { logRow.Value, 10}");
            }
        }

        private static Applicant CreateApplicant(string applicantId, Framework.Process process)
        {
            var aprioriInfo = ApplicantDatabase.Instance.AprioriInfo[applicantId];
            var availableLoaders = new ILoader[] { AddressLoader.Instance, CreditLoader.Instance, CreditScoreCalculator.Instance };
            var knownKeys = aprioriInfo.Keys.ToImmutableHashSet();
            var requiredKeys = process.Keys.ToImmutableHashSet();
            var selectedLoaders = LoadersSelector.PickOptimizedSet(availableLoaders, knownKeys, requiredKeys).ToList();
            return new Applicant
            {
                Id = applicantId,
                KeyValueMap = aprioriInfo,
                Loaders = selectedLoaders
            };
        }
    }
}
    