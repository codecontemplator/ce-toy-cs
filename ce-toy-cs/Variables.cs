using ce_toy_cs.Framework;

namespace ce_toy_cs
{
    namespace VariableTypes
    {
        public enum Roles
        {
            Primary, Other
        }

        public class Address
        {
            public string Street { get; init; }
            public string PostalCode { get; init; }
            public bool IsValid => !string.IsNullOrEmpty(Street);
        }
    }

    public static class Variables
    {
        public class CreditADef : Variable<int>
        {
            private CreditADef() { }
            public override string Name => nameof(CreditA);
            public static CreditADef Instance { get; } = new CreditADef();
        }

        public class CreditBDef : Variable<int>
        {
            private CreditBDef() { }
            public override string Name => nameof(CreditB);
            public static CreditBDef Instance { get; } = new CreditBDef();
        }

        public class SalaryDef : Variable<int>
        {
            private SalaryDef() { }
            public override string Name => nameof(Salary);
            public static SalaryDef Instance { get; } = new SalaryDef();
        }

        public class RoleDef : Variable<VariableTypes.Roles>
        {
            private RoleDef() { }
            public override string Name => nameof(Role);
            public static RoleDef Instance { get; } = new RoleDef();
        }

        public class AddressDef : Variable<VariableTypes.Address>
        {
            private AddressDef() { }
            public override string Name => nameof(Address);
            public static AddressDef Instance { get; } = new AddressDef();
        }

        public class CreditScoreDef : Variable<double>
        {
            private CreditScoreDef() { }
            public override string Name => nameof(CreditScore);
            public static CreditScoreDef Instance { get; } = new CreditScoreDef();
        }

        public class AgeDef : Variable<int>
        {
            private AgeDef() { }
            public override string Name => nameof(Age);
            public static AgeDef Instance { get; } = new AgeDef();
        }

        public class DeceasedDef : Variable<bool>
        {
            private DeceasedDef() { }
            public override string Name => nameof(Deceased);
            public static DeceasedDef Instance { get; } = new DeceasedDef();
        }

        public class FlagsDef : Variable<int>
        {
            private FlagsDef() { }
            public override string Name => nameof(Flags);
            public static FlagsDef Instance { get; } = new FlagsDef();
        }

        public static CreditADef CreditA => CreditADef.Instance;
        public static CreditBDef CreditB => CreditBDef.Instance;
        public static SalaryDef Salary => SalaryDef.Instance;
        public static RoleDef Role => RoleDef.Instance;
        public static AddressDef Address => AddressDef.Instance;
        public static CreditScoreDef CreditScore => CreditScoreDef.Instance;
        public static AgeDef Age => AgeDef.Instance;
        public static DeceasedDef Deceased => DeceasedDef.Instance;
        public static FlagsDef Flags => FlagsDef.Instance;
    }
}
