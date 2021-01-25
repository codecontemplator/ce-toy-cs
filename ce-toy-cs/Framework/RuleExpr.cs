using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace ce_toy_cs.Framework
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
        public IRuleExprContext PreContext { get; init; }
        public IRuleExprContext PostContext { get; init; }
        public object Value { get; init; }
    }

    public record Credit
    {
        public int Amount { get; init; }
        public decimal? Interest { get; init; }
    }

    public interface IRuleExprContext
    {
        int RequestedAmount { get; }
        Option<Credit> GrantedCredit { get; }
        ImmutableList<LogEntry> Log { get; }
        IRuleExprContext WithNewCredit(Credit credit);
        IRuleExprContext WithLogging(LogEntry entry);
    }

    public record MRuleExprContext : IRuleExprContext
    {
        public int RequestedAmount { get; init; }
        public Option<Credit> GrantedCredit { get; init; }
        public ImmutableDictionary<string, Applicant> Applicants { get; init; }
        public ImmutableList<LogEntry> Log { get; init; }
        public IRuleExprContext WithNewCredit(Credit credit) => this with { GrantedCredit = Option<Credit>.Some(credit) };
        public IRuleExprContext WithLogging(LogEntry entry) => this with { Log = Log.Add(entry) };
    }

    public record SRuleExprContext : IRuleExprContext
    {
        public int RequestedAmount { get; init; }
        public Option<Credit> GrantedCredit { get; init; }
        public Applicant Applicant { get; init; }
        public ImmutableList<LogEntry> Log { get; init; }
        public IRuleExprContext WithNewCredit(Credit credit) => this with { GrantedCredit = Option<Credit>.Some(credit) };
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