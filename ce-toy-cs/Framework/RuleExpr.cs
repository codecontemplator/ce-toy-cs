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
        public RuleExprContextBase PreContext { get; init; }
        public RuleExprContextBase PostContext { get; init; }
        public object Value { get; init; }
    }

    /*
    public interface IRuleExprContext
    {
        int Amount { get; }
        ImmutableList<LogEntry> Log { get; }
        IRuleExprContext WithNewAmount(int amount);
        IRuleExprContext WithLogging(LogEntry entry);
    }
    */

    public abstract record RuleExprContextBase
    {
        public int Amount { get; init; }
        public ImmutableDictionary<string, Applicant> Applicants { get; init; }
        public ImmutableList<LogEntry> Log { get; init; }
    }

    public record RuleExprContext<SelectorType> : RuleExprContextBase
    {
        public SelectorType Selector { get; init; }
        //public IRuleExprContext WithNewAmount(int amount) => this with { Amount = amount };
        public RuleExprContext<SelectorType> WithLogging(LogEntry entry) => this with { Log = Log.Add(entry) };
        public RuleExprContext<NewSelectorType> WithSelector<NewSelectorType>(NewSelectorType newSelector) =>
            new RuleExprContext<NewSelectorType> { Amount = Amount, Applicants = Applicants, Log = Log, Selector = newSelector };
    }

    public class Result
    {
        public int? Amount { get; init; }

        public static Result Empty { get; } = new Result();
        public static Result NewAmount(int newAmount) => new Result { Amount = newAmount };

        public RuleExprContext<SelectorType> Apply<SelectorType>(RuleExprContext<SelectorType> ctx)
        {
            return ctx with
            {
                Amount = Amount ?? ctx.Amount
            };
        }
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