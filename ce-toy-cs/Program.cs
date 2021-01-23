using System;
using System.Collections.Generic;
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
            var result = process.RuleExpr(new MRuleExprContext
            {
                Log = ImmutableList<LogEntry>.Empty,
                Amount = requestedAmount,
                Applicants = applicants.ToDictionary(x => x.Id).ToImmutableDictionary()
            });

            Console.WriteLine($"# Evaluation");
            Console.WriteLine($"Requested amount: {requestedAmount}");
            Console.WriteLine($"Granted amount: {result.Item1}");
            foreach (var applicant in result.Item2.Applicants.Values)
                Console.WriteLine($"{applicant.Id}: a posteriori keys={string.Join(',', applicant.KeyValueMap.Keys)} loaders={string.Join(',', applicant.Loaders.Select(x => x.Name))}");
            Console.WriteLine();

            Console.WriteLine($"# Evaluation log");
            Console.WriteLine($"{"Message",-45} | {"Amount",10} | { "Value", 10}");
            Console.WriteLine(new string('-', 45 + 10 + 10 + 6));
            foreach(var logRow in result.Item2.Log)
            {
                Console.WriteLine($"{logRow.Message,-45} | { logRow.Amount, 10} | { logRow.Value, 10}");
            }
        }

        private static Applicant CreateApplicant(string applicantId, Rule process)
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

    class ApplicantDatabase
    {
        private static ApplicantDatabase _instance = new ApplicantDatabase();

        private ApplicantDatabase()
        {
            var addressInfo = new Dictionary<string, ImmutableDictionary<string, object>>();
            addressInfo["applicant1"] = new Dictionary<string, object>
            {
                { "Address", "Street 1" }
            }.ToImmutableDictionary();
            addressInfo["applicant2"] = new Dictionary<string, object>
            {
                { "Address", "" }
            }.ToImmutableDictionary();

            var creditInfo = new Dictionary<string, ImmutableDictionary<string, object>>();
            creditInfo["applicant1"] = new Dictionary<string, object>
            {
                { "CreditA", 20.0 },
                { "CreditB", 29.0 },
                { "Salary", 10 },
                { "Flags", 1 }
            }.ToImmutableDictionary();
            creditInfo["applicant2"] = new Dictionary<string, object>
            {
                { "CreditA", 10.0 },
                { "CreditB", 39.0 },
                { "Salary", 41 },
                { "Flags", 0 }
            }.ToImmutableDictionary();

            var aprioreInfo = new Dictionary<string, ImmutableDictionary<string, object>>();
            aprioreInfo["applicant1"] = new Dictionary<string, object>
            {
                { "Role", "Primary" },
                { "CreditA", 20.0 },
                { "CreditB", 29.0 },
                { "Salary", 10 },
                { "Age", 50 },
                { "Deceased", false }
            }.ToImmutableDictionary();
            aprioreInfo["applicant2"] = new Dictionary<string, object>
            {
                { "Role", "Secondary" },
                { "Age", 59 },
                { "Deceased", false }
            }.ToImmutableDictionary();

            AddressInfo = addressInfo.ToImmutableDictionary();
            CreditInfo = creditInfo.ToImmutableDictionary();
            AprioriInfo = aprioreInfo.ToImmutableDictionary();
        }

        public static ApplicantDatabase Instance => _instance;
        public ImmutableDictionary<string, ImmutableDictionary<string, object>> AddressInfo { get; init; }
        public ImmutableDictionary<string, ImmutableDictionary<string, object>> CreditInfo { get; init; }
        public ImmutableDictionary<string, ImmutableDictionary<string, object>> AprioriInfo { get; init; }
    }

    class AddressLoader : ILoader
    {
        public string Name => "AddressLoader";

        public int Cost => 1;

        public IImmutableSet<string> RequiredKeys => ImmutableHashSet<string>.Empty;

        public IImmutableSet<string> LoadedKeys => new[] { "Address" }.ToImmutableHashSet();

        public ImmutableDictionary<string, object> Load(string applicantId, string key, ImmutableDictionary<string, object> input)
        {
            var addressInfo = ApplicantDatabase.Instance.AddressInfo[applicantId];
            return input.RemoveRange(addressInfo.Keys).AddRange(addressInfo);
        }

        public static AddressLoader Instance => _instance;
        private static AddressLoader _instance = new AddressLoader();
    }

    class CreditLoader : ILoader
    {
        public string Name => "CreditLoader";

        public int Cost => 2;

        public IImmutableSet<string> RequiredKeys => ImmutableHashSet<string>.Empty;
        public IImmutableSet<string> LoadedKeys => new[] { "Salary", "CreditA", "CreditB", "Flags" }.ToImmutableHashSet();

        public ImmutableDictionary<string, object> Load(string applicantId, string key, ImmutableDictionary<string, object> input)
        {
            var creditInfo = ApplicantDatabase.Instance.CreditInfo[applicantId];
            return input.RemoveRange(creditInfo.Keys).AddRange(creditInfo);
        }

        public static CreditLoader Instance => _instance;
        private static CreditLoader _instance = new CreditLoader();
    }

    class CreditScoreCalculator : ILoader
    {
        public string Name => "CreditScoreCalculator";

        public int Cost => 0;

        public IImmutableSet<string> RequiredKeys => new[] { "CreditA", "CreditB", "Address" }.ToImmutableHashSet();

        public IImmutableSet<string> LoadedKeys => new[] { "CreditScore" }.ToImmutableHashSet();

        public ImmutableDictionary<string, object> Load(string applicantId, string key, ImmutableDictionary<string, object> input)
        {
            return input.Add("CreditScore", CalculateCreditScore((double)input["CreditA"], (double)input["CreditB"], (string)input["Address"]));
        }

        private double CalculateCreditScore(double creditA, double creditB, string address)
        {
            var result = creditA > 10.0 ? 5.0 : 0.0;
            result += creditB > 2.0 ? 5.0 : 0.0;
            result += string.IsNullOrEmpty(address) ? 15.0 : 0.0;
            result /= 5.0 + 5.0 + 15.0;
            return result;
        }

        public static CreditScoreCalculator Instance => _instance;
        private static CreditScoreCalculator _instance = new CreditScoreCalculator();
    }
}
    