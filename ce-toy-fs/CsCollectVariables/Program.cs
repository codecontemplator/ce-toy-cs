﻿namespace CsCollectVariables
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public interface IVariableEvaluator
  {
    IVariableDefinition UntypedDefinition { get; }
  }

  public interface IVariableDefinition
  {
    string Name      { get; }
    Type   TagType   { get; }
    Type   ValueType { get; }

    IVariableEvaluator CreateUntypedEvaluator();
  }

  public interface IRule<T>
  {
    IVariableDefinition[] RequiredVariables { get; }
    T                     Zero              { get; }
    T Execute(Environment env);
  }

  public record VariableEvaluator<TTag, T>(VariableDefinition<TTag, T> Definition) : IVariableEvaluator
  {
    public IVariableDefinition UntypedDefinition => Definition;

    public T Evaluate => Definition.Zero;
  }


  public record VariableDefinition<TTag, T>(string Name, T Zero) : IRule<T>, IVariableDefinition
  {
    public Type TagType   => typeof(TTag);
    public Type ValueType => typeof(T);

    public IVariableDefinition[] RequiredVariables => new [] {this};
    public T Execute(Environment env)
    {
      return env.Evaluate(this);
    }

    public IVariableEvaluator CreateUntypedEvaluator() => CreateEvaluator();

    public VariableEvaluator<TTag, T> CreateEvaluator()
    {
      return new VariableEvaluator<TTag, T>(this);
    }
  }

  public sealed class EvaluateException : Exception
  {
    public EvaluateException(string message)
      : this(message, null)
    {
    }

    public EvaluateException(string message, Exception innerException)
      : base(message, innerException)
    {
    }
  }

  public record Environment(Dictionary<Type, IVariableEvaluator> evaluators)
  {
    public T Evaluate<TTag, T>(VariableDefinition<TTag, T> variable)
    {
      var key = typeof(VariableDefinition<TTag, T>);
      if (evaluators.TryGetValue(key, out IVariableEvaluator ue))
      {
        var e = ue as VariableEvaluator<TTag, T>;
        if (e != null)
        {
          return e.Evaluate;
        }
        else
        {
          throw new EvaluateException($"Evaluator found for ${key.Name} but not matching expected type");
        }
      }
      else
      {
        throw new EvaluateException($"No evaluator matching ${key.Name} found");
      }
    }
  }

  public static class Rule
  {
    sealed record SelectRule<T, U>(IRule<T> Rule, Func<T, U> Selector) : IRule<U>
    {
      readonly U zero = Selector(Rule.Zero);

      public IVariableDefinition[]  RequiredVariables => Rule.RequiredVariables;
      public U                      Zero              => zero;

      public U Execute(Environment env)
      {
        return Selector(Rule.Execute(env));
      }
    }

    public static IRule<U> Select<T, U>(this IRule<T> rule, Func<T, U> selector)
    {
      return new SelectRule<T, U>(rule, selector);
    }

    sealed record SelectManyRule<T, C, U>(IRule<T> Rule, Func<T, IRule<C>> Collect, Func<T, C, U> Selector) : IRule<U>
    {
      readonly U zero = Selector(Rule.Zero, Collect(Rule.Zero).Zero);
      readonly IVariableDefinition[] merged = Rule.RequiredVariables.Concat(Collect(Rule.Zero).RequiredVariables).ToArray();

      public IVariableDefinition[]  RequiredVariables => merged;
      public U                      Zero              => zero;

      public U Execute(Environment env)
      {
        var tv = Rule.Execute(env);
        var cr = Collect(tv);
        var cv = cr.Execute(env);
        return Selector(tv, cv);
      }
    }

    public static IRule<U> SelectMany<T, C, U>(this IRule<T> rule, Func<T, IRule<C>> collect, Func<T, C, U> selector)
    {
      return new SelectManyRule<T, C ,U>(rule, collect, selector);
    }

    public static T Invoke<T>(this IRule<T> rule)
    {
      var vds = rule.RequiredVariables;
      var evs = vds
        .Select(vd => vd.CreateUntypedEvaluator())
        .ToDictionary(ve => ve.UntypedDefinition.GetType())
        ;
      var env = new Environment(evs);
      return rule.Execute(env);
    }

  }

  // The variable model is generated by CsModel.tt
  public static partial class Variables
  {
  }

  class Program
  {
    static IRule<decimal> MaxTotalAmount(decimal debtLimit) =>
      from amount in Variables.Amount
      from creditA in Variables.CreditA
      from creditB in Variables.CreditB
      let totalCredit = creditA + creditB
      select totalCredit > debtLimit ? 0.0M : amount
      ;

    static void Main(string[] args)
    {
      var r = MaxTotalAmount(100.0M);
      foreach (var v in r.RequiredVariables)
      {
        Console.WriteLine($"Required: {v.Name}");
      }

      var result = r.Invoke();
      Console.WriteLine($"Result: {result}");

    }
  }
}