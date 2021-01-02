using System.Collections.Generic;
using System.Collections.Immutable;

namespace ce_toy_cs
{
    class RuleExprKeyInterpreter
    {
        private class Loader : ILoader
        {
            public string Name => string.Empty;

            public int Cost => 0;

            public IImmutableSet<string> Keys => ImmutableHashSet<string>.Empty;

            public ImmutableDictionary<string, int> Load(string key, ImmutableDictionary<string, int> input) => input.Add(key, 0);
        }

        public static IEnumerable<string> GetUsedKeys(RuleExpr<int> expr)
        {
            var loader = new Loader();

            IEnumerable<ILoader> Loaders()
            {
                while (true)
                    yield return loader;
            }

            var (_, context) = expr(new RuleExprContext
            {
                Amount = 1200,
                Loaders = Loaders(),
                KeyValueMap = ImmutableDictionary<string, int>.Empty
            });

            return context.KeyValueMap.Keys;
        }
    }
}
