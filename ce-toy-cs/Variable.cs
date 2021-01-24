using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ce_toy_cs
{
    public interface IVariable
    {
        string Name { get; }
    }

    public abstract class Variable<T> : IVariable
    {
        public abstract string Name { get; }
        public static implicit operator string(Variable<T> v) => v.Name;
        public RuleExprAst<T, SRuleExprContext> Value => SDsl.GetValue<T>(Name);
        public RuleExprAst<IEnumerable<T>, MRuleExprContext> Values => MDsl.GetValues<T>(Name);
    }
}
