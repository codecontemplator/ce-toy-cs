using System.Collections.Immutable;

namespace ce_toy_cs
{
    public interface ILoader
    {
        string Name { get; }
        int Cost { get; }
        IImmutableSet<string> Keys { get; }
        ImmutableDictionary<string, int> Load(string key, ImmutableDictionary<string, int> input);
    }

}
