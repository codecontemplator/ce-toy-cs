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
            Console.WriteLine($"Required keys: {string.Join(',', process.GetKeys())}");
            Console.WriteLine();

            Console.WriteLine("# Creating applicants");
            var applicants = new []
            {
                CreateApplicant("applicant1", process),
                CreateApplicant("applicant2", process),
            };
            Console.WriteLine();

            var requestedAmount = 170;
            var result = process.Eval(new RuleContext
            {
                Log = ImmutableList<RuleLogEntry>.Empty,
                RuleExprContext = new MRuleExprContext
                {
                    Amount = requestedAmount,
                    Applicants = applicants.ToDictionary(x => x.Id).ToImmutableDictionary()
                }
            });

            Console.WriteLine($"# Evaluation");
            Console.WriteLine($"Requested amount: {requestedAmount}");
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
            var availableLoaders = new ILoader[] { AddressLoader.Instance, CreditLoader.Instance, CreditScoreCalculator.Instance };
            var knownKeys = aprioreInfo.Keys.ToImmutableHashSet();
            var requiredKeys = process.GetKeys().ToImmutableHashSet();
            var selectedLoaders = LoadersSelector.PickOptimizedSet(availableLoaders, knownKeys, requiredKeys).ToList();
            Console.WriteLine($"{applicantId}: apriore keys={string.Join(',',aprioreInfo.Keys)} loaders={string.Join(',',selectedLoaders.Select(x => x.Name))}");
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
                { "CreditA", 10.0 },
                { "CreditB", 39.0 },
                { "Salary", 10 },
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
        public IImmutableSet<string> LoadedKeys => new[] { "Salary", "CreditA", "CreditB" }.ToImmutableHashSet();

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
    