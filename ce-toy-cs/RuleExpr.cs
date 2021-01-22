using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    public delegate (Option<T>, RuleExprContext) RuleExpr<T, RuleExprContext>(RuleExprContext input);

    public record Applicant
    {
        public string Id { get; init; }
        public IEnumerable<ILoader> Loaders { get; init; }
        public ImmutableDictionary<string, object> KeyValueMap { get; init; }
    }

    public record LogEntry
    {
        public string Message { get; init; }
        public int Amount { get; init; }
        public object Value { get; init; }
    }

    public interface IRuleExprContext
    {
        int Amount { get; }
        ImmutableList<LogEntry> Log { get; }
        IRuleExprContext WithNewAmount(int amount);
        IRuleExprContext WithLogging(LogEntry entry);
    }

    public record MRuleExprContext : IRuleExprContext
    {
        public int Amount { get; init; }
        public ImmutableDictionary<string, Applicant> Applicants { get; init; }
        public ImmutableList<LogEntry> Log { get; init; }
        public IRuleExprContext WithNewAmount(int amount) => this with { Amount = amount };
        public IRuleExprContext WithLogging(LogEntry entry) => this with { Log = Log.Add(entry) };
    }

    public record SRuleExprContext : IRuleExprContext
    {
        public int Amount { get; init; }
        public Applicant Applicant { get; init; }
        public ImmutableList<LogEntry> Log { get; init; }
        public IRuleExprContext WithNewAmount(int amount) => this with { Amount = amount };
        public IRuleExprContext WithLogging(LogEntry entry) => this with { Log = Log.Add(entry) };
    }

    public record RuleExprAst<T, RuleExprContext>
    {
        public Expression<RuleExpr<T, RuleExprContext>> Expression { get; init; }
        public RuleExpr<T, RuleExprContext> Compile() => Expression.Compile();
        public IEnumerable<string> GetKeys()
        {
            var findKeysVisitor = new FindKeysVisistor();
            findKeysVisitor.Visit(Expression);
            return findKeysVisitor.FoundKeys;
        }
    }
}