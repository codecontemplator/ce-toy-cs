using ce_toy_cs.Framework;
using System.Collections.Immutable;

namespace ce_toy_cs
{
    class AddressLoader : ILoader
    {
        public string Name => "AddressLoader";

        public int Cost => 1;

        public IImmutableSet<string> RequiredKeys => ImmutableHashSet<string>.Empty;

        public IImmutableSet<string> LoadedKeys => new[] { Variables.Address.Name }.ToImmutableHashSet();

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
        public IImmutableSet<string> LoadedKeys => new[] {
            Variables.Salary.Name,
            Variables.CreditA.Name,
            Variables.CreditB.Name,
            Variables.Flags.Name
        }.ToImmutableHashSet();

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

        public IImmutableSet<string> RequiredKeys => new[] {
            Variables.CreditA.Name,
            Variables.CreditB.Name,
            Variables.Address.Name
        }.ToImmutableHashSet();

        public IImmutableSet<string> LoadedKeys => new[] { Variables.CreditScore.Name }.ToImmutableHashSet();

        public ImmutableDictionary<string, object> Load(string applicantId, string key, ImmutableDictionary<string, object> input)
        {
            return input.Add(Variables.CreditScore, CalculateCreditScore((int)input[Variables.CreditA], (int)input[Variables.CreditB], (Address)input[Variables.Address]));
        }

        private double CalculateCreditScore(double creditA, double creditB, Address address)
        {
            var result = creditA > 10.0 ? 5.0 : 0.0;
            result += creditB > 2.0 ? 5.0 : 0.0;
            result += address.IsValid ? 0.0 : 15.0;
            result /= 5.0 + 5.0 + 15.0;
            return result;
        }

        public static CreditScoreCalculator Instance => _instance;
        private static CreditScoreCalculator _instance = new CreditScoreCalculator();
    }
}
