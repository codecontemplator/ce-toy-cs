using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ce_toy_cs.Framework
{
    public class Unit : IRuleExprContextApplicable
    {
        private Unit()
        {

        }

        public static Unit Value { get; } = new Unit();

        public IRuleExprContext ApplyTo(IRuleExprContext ctx)
        {
            return ctx;
        }

        public override bool Equals(object obj)
        {
            return true;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}
