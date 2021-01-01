// -----------------------------------------------------------------------------
// THIS FILE IS GENERATED FROM: FsModel.tt
//  Changes to this file will be lost when the files is regenerated
// -----------------------------------------------------------------------------

namespace FsDepedencies


module Model =
  open System
  open System.Collections.Generic

  open Core

  type [<Interface; Variable>] IAmountVariable = abstract Amount : decimal
  let metaAmount = constVariable<IAmountVariable, decimal> "Amount" 0.0M
  let getAmount (env : #IAmountVariable) = env.Amount
  type [<Interface; Variable>] ICreditAVariable = abstract CreditA : decimal
  let metaCreditA = constVariable<ICreditAVariable, decimal> "CreditA" 0.0M
  let getCreditA (env : #ICreditAVariable) = env.CreditA
  type [<Interface; Variable>] ICreditBVariable = abstract CreditB : decimal
  let metaCreditB = constVariable<ICreditBVariable, decimal> "CreditB" 0.0M
  let getCreditB (env : #ICreditBVariable) = env.CreditB
  type [<Interface; Variable>] ICreditCVariable = abstract CreditC : decimal
  let metaCreditC = constVariable<ICreditCVariable, decimal> "CreditC" 0.0M
  let getCreditC (env : #ICreditCVariable) = env.CreditC

  let metaVariables : MetaVariable array =
    [|
      metaAmount
      metaCreditA
      metaCreditB
      metaCreditC
    |]

  let domainTypes =
    metaVariables
    |> Array.map (fun mv -> mv.DomainType, mv)
    |> dict

  type Env (evaluators : IDictionary<Type, MetaVariableEvaluator>) =
    class
      let lookup (mv : MetaVariable<'DT, 'VT>) : 'VT =
        let name  = mv.Name
        let dt    = mv.DomainType
        let b, ue = evaluators.TryGetValue dt
        if b then
          match ue with
          | (:? MetaVariableEvaluator<'DT, 'VT> as e) -> e.Eval ()
          | v                                         ->
            let lt = v.GetType()
            let et = typeof<'VT>
            failwithf "Variable '%s' loaded but type mismatch. Loaded: %s, requested: %s" name lt.Name et.Name
        else
          failwithf "Variable '%s' not loaded" name
      interface IAmountVariable with
        member x.Amount = lookup metaAmount
      end
      interface ICreditAVariable with
        member x.CreditA = lookup metaCreditA
      end
      interface ICreditBVariable with
        member x.CreditB = lookup metaCreditB
      end
      interface ICreditCVariable with
        member x.CreditC = lookup metaCreditC
      end
    end
  let env = Env (dict [||])
