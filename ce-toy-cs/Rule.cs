﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    interface IRule
    {
        (int, RuleContext) Eval(RuleContext context);
        IEnumerable<string> GetKeys();
    }

    record RuleLogEntry
    {
        public string RuleName { get; init; }
        public int RequestedAmount { get; init; }
        public int GrantedAmount { get; init; }
    }

    record RuleContext
    {
        public MRuleExprContext RuleExprContext { get; init; }
        public ImmutableList<RuleLogEntry> Log { get; init; }
    }

    class AtomicRule : IRule
    {
        public AtomicRule(Expression<Func<RuleExprAst<int, MRuleExprContext>>> expression)
        {
            Name = ((MethodCallExpression)expression.Body).Method.Name;
            var expr = expression.Compile()();
            Expr = expr.Compile();
            Keys = ImmutableList.ToImmutableList(expr.GetKeys());
        }

        public AtomicRule(string name, RuleExprAst<int, MRuleExprContext> expr)
        {
            Name = name;
            Expr = expr.Compile();
            Keys = ImmutableList.ToImmutableList(expr.GetKeys());
        }

        public string Name { get; }
        public RuleExpr<int, MRuleExprContext> Expr { get; }
        public IImmutableList<string> Keys { get; }

        public (int, RuleContext) Eval(RuleContext context)
        {
            var (amountOption,ruleExprContext) = Expr(context.RuleExprContext);
            int amount;
            if (!amountOption.IsSome(out amount)) 
                amount = ruleExprContext.Amount;
            return
                (
                    amount,
                    context with {
                        Log = context.Log.Add(new RuleLogEntry { RuleName = Name, RequestedAmount = context.RuleExprContext.Amount, GrantedAmount = amount }),
                        RuleExprContext = ruleExprContext with { Amount = amount }
                    }
                );
        }

        public IEnumerable<string> GetKeys() => Keys;
    }

    class AndThenRule : IRule
    {
        private readonly IRule r1;
        private readonly IRule r2;

        public AndThenRule(IRule r1, IRule r2)
        {
            this.r1 = r1;
            this.r2 = r2;
        }

        public (int, RuleContext) Eval(RuleContext context)
        {
            var (a, context2) = r1.Eval(context);
            var context3 = context2 with { RuleExprContext = context2.RuleExprContext with { Amount = a } };
            return r2.Eval(context3);
        }

        public IEnumerable<string> GetKeys()
        {
            foreach (var key in r1.GetKeys())
                yield return key;
            foreach (var key in r2.GetKeys())
                yield return key;
        }
    }

    class RuleBuilder
    {
        private IRule _current;

        public RuleBuilder()
        {

        }

        public RuleBuilder Add(IRule rule)
        {
            if (_current == null)
                _current = rule;
            else
                _current = new AndThenRule(_current, rule);

            return this;
        }

        public RuleBuilder Add(Expression<Func<RuleExprAst<int, MRuleExprContext>>> expression)
        {
            return Add(new AtomicRule(expression));
        }

        public IRule Build() => _current;
    }

}