﻿using System;
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
            Console.WriteLine($"Used keys: {string.Join(',', process.GetKeys())}");
            Console.WriteLine();

            var applicants = new []
            {
                CreateApplicant("applicant1", process),
                CreateApplicant("applicant2", process),
            };

            var requestedAmount = 170;

            Console.WriteLine($"# Evaluating process started");
            Console.WriteLine($"Requested amount: {requestedAmount}");
            Console.WriteLine();

            var result = process.Eval(new RuleContext
            {
                Log = ImmutableList<RuleLogEntry>.Empty,
                RuleExprContext = new MRuleExprContext
                {
                    Amount = requestedAmount,
                    Applicants = applicants.ToDictionary(x => x.Id).ToImmutableDictionary()
                }
            });

            Console.WriteLine($"# Evaluation finished");
            Console.WriteLine($"Granted amount: {result.Item1}");
            Console.WriteLine();

            Console.WriteLine($"# Evaluation log");
            Console.WriteLine($"{"Step",-45} | {"Requested",10} | { "Granted", 10}");
            Console.WriteLine(new string('-', 45 + 10 + 10 + 6));
            foreach(var logRow in result.Item2.Log)
            {
                Console.WriteLine($"{logRow.RuleName,-45} | { logRow.RequestedAmount, 10} | { logRow.GrantedAmount, 10}");
            }
        }

        private static Applicant CreateApplicant(string applicantId, IRule process)
        {
            var aprioreInfo = ApplicantDatabase.Instance.AprioreInfo[applicantId];
            var availableLoaders = new ILoader[] { BaseLoader.Instance, CreditLoader.Instance };
            var requiredKeys = process.GetKeys().ToImmutableHashSet().Except(aprioreInfo.Keys);
            var selectedLoaders = new ILoader[] { availableLoaders.First(x => x.Keys.IsSupersetOf(requiredKeys)) };
            return new Applicant
            {
                Id = applicantId,
                KeyValueMap = aprioreInfo,
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
            }.ToImmutableDictionary();
            creditInfo["applicant2"] = new Dictionary<string, object>
            {
                { "CreditA", 10.0 },
                { "CreditB", 39.0 },
                { "Salary", 41 },
            }.ToImmutableDictionary();

            var aprioreInfo = new Dictionary<string, ImmutableDictionary<string, object>>();
            aprioreInfo["applicant1"] = new Dictionary<string, object>
            {
                { "Role", "Primary" },
                { "Address", "Street 1" }
            }.ToImmutableDictionary();
            aprioreInfo["applicant2"] = new Dictionary<string, object>
            {
                { "Role", "Secondary" },
            }.ToImmutableDictionary();

            AddressInfo = addressInfo.ToImmutableDictionary();
            CreditInfo = creditInfo.ToImmutableDictionary();
            AprioreInfo = aprioreInfo.ToImmutableDictionary();
        }

        public static ApplicantDatabase Instance => _instance;
        public ImmutableDictionary<string, ImmutableDictionary<string, object>> AddressInfo { get; init; }
        public ImmutableDictionary<string, ImmutableDictionary<string, object>> CreditInfo { get; init; }
        public ImmutableDictionary<string, ImmutableDictionary<string, object>> AprioreInfo { get; init; }
    }

    class BaseLoader : ILoader
    {
        public string Name => "BaseLoader";

        public int Cost => 1;

        public IImmutableSet<string> Keys => new[] { "Address" }.ToImmutableHashSet();

        public ImmutableDictionary<string, object> Load(string applicantId, string key, ImmutableDictionary<string, object> input)
        {
            return input.AddRange(ApplicantDatabase.Instance.CreditInfo[applicantId]);
        }

        public static BaseLoader Instance => _instance;
        private static BaseLoader _instance = new BaseLoader();
    }

    class CreditLoader : ILoader
    {
        public string Name => "CreditLoader";

        public int Cost => 2;

        public IImmutableSet<string> Keys => new[] { "Salary", "CreditA", "CreditB" }.ToImmutableHashSet().Union(BaseLoader.Instance.Keys);

        public ImmutableDictionary<string, object> Load(string applicantId, string key, ImmutableDictionary<string, object> input)
        {
            var baseInfo = BaseLoader.Instance.Load(applicantId, key, input);
            var creditInfo = ApplicantDatabase.Instance.CreditInfo[applicantId];
            return input.AddRange(baseInfo).AddRange(creditInfo);
        }

        public static CreditLoader Instance => _instance;
        private static CreditLoader _instance = new CreditLoader();
    }
}
    