using ce_toy_cs.Framework.Functional;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ce_toy_cs.Framework
{
    public record Process
    {
        public string Name { get; init; }
        public RuleExpr<Unit, RuleExprContext<Unit>> RuleExpr { get; init; }
        public IImmutableList<string> Keys { get; init; }
    }

    public static class RuleExtensions
    {
        public static Process CompileToProcess(this IEnumerable<RuleExprAst<Result, RuleExprContext<Unit>>> ruleAsts, string name)
        {
            var ruleAst = ruleAsts.Join();
            return new Process
            {
                Name = name,
                Keys = ruleAst.GetKeys().ToImmutableList(),
                RuleExpr = ruleAst.Compile()
            };
        }
    }
}
