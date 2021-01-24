using ce_toy_cs.VariableTypes;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ce_toy_cs
{
    class ApplicantDatabase
    {
        private static ApplicantDatabase _instance = new ApplicantDatabase();

        private ApplicantDatabase()
        {
            var addressInfo = new Dictionary<string, ImmutableDictionary<string, object>>();
            addressInfo["applicant1"] = new Dictionary<string, object>
            {
                { Variables.Address, new Address { Street = "Street 1", PostalCode = "12345" } }
            }.ToImmutableDictionary();
            addressInfo["applicant2"] = new Dictionary<string, object>
            {
                { Variables.Address, new Address { Street = "" } }
            }.ToImmutableDictionary();

            var creditInfo = new Dictionary<string, ImmutableDictionary<string, object>>();
            creditInfo["applicant1"] = new Dictionary<string, object>
            {
                { Variables.CreditA, 20 },
                { Variables.CreditB, 29 },
                { Variables.Salary, 10 },
                { Variables.Flags, 1 }
            }.ToImmutableDictionary();
            creditInfo["applicant2"] = new Dictionary<string, object>
            {
                { Variables.CreditA, 10 },
                { Variables.CreditB, 39 },
                { Variables.Salary, 41 },
                { Variables.Flags, 0 }
            }.ToImmutableDictionary();

            var aprioreInfo = new Dictionary<string, ImmutableDictionary<string, object>>();
            aprioreInfo["applicant1"] = new Dictionary<string, object>
            {
                { Variables.Role, Roles.Primary },
                { Variables.CreditA, 20 },
                { Variables.CreditB, 29 },
                { Variables.Salary, 10 },
                { Variables.Age, 50 },
                { Variables.Deceased, false }
            }.ToImmutableDictionary();
            aprioreInfo["applicant2"] = new Dictionary<string, object>
            {
                { Variables.Role, Roles.Other },
                { Variables.Age, 59 },
                { Variables.Deceased, false }
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

}
    