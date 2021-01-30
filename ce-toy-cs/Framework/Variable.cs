using ce_toy_cs.Framework.Functional;
using System.Collections.Generic;

namespace ce_toy_cs.Framework
{
    public interface IVariable
    {
        string Name { get; }
    }

    public abstract class Variable<T> : IVariable
    {
        public abstract string Name { get; }
        public static implicit operator string(Variable<T> v) => v.Name;
        public RuleExprAst<T, RuleExprContext<string>> Value => Dsl.GetValue<T>(Name);
        public RuleExprAst<IEnumerable<T>, RuleExprContext<Unit>> Values => Dsl.GetValues<T>(Name);
    }
}
