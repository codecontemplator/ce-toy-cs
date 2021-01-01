namespace FsDepedencies

module Core =
  open FSharp.Linq.RuntimeHelpers
  open FSharp.Quotations
  open FSharp.Quotations.Patterns

  open System
  open System.Collections.Generic

  type [<Sealed>] VariableAttribute() = inherit Attribute()

  type [<AbstractClass>] MetaVariableEvaluator () =
    class
    end

  type [<AbstractClass>] MetaVariableEvaluator<'VT> () =
    class
      inherit MetaVariableEvaluator ()
      abstract Eval : unit -> 'VT
    end

  type [<AbstractClass>] MetaVariable (domainType : Type, valueType : Type, name : String) =
    class
      member x.DomainType = domainType
      member x.ValueType  = valueType
      member x.Name       = name

      abstract UntypedEvaluator : unit -> MetaVariableEvaluator

      override x.ToString () = sprintf "{MetaVariable %s %s '%s'}" domainType.Name valueType.Name name
    end

  type [<AbstractClass>] MetaVariableEvaluator<'DT, 'VT> (mv : MetaVariable<'DT, 'VT>) =
    class
      inherit MetaVariableEvaluator<'VT> ()
    end

  and [<AbstractClass>] MetaVariable<'DT, 'VT> (name : String) =
    class
      inherit MetaVariable (typeof<'DT>, typeof<'VT>, name)

      abstract Evaluator : unit -> MetaVariableEvaluator<'DT, 'VT>

      override x.UntypedEvaluator () = upcast x.Evaluator ()
    end

  type [<Sealed>] ConstMetaVariable<'DT, 'VT> (name : String, v: 'VT) =
    class
      inherit MetaVariable<'DT, 'VT> (name)

      override x.Evaluator () =
        { new MetaVariableEvaluator<'DT, 'VT> (x) with
            override x.Eval () = v
        }
    end
  let constVariable<'DT, 'VT> name (v : 'VT) = ConstMetaVariable<'DT, 'VT> (name, v)

  type DomainTypes = IDictionary<Type, MetaVariable>

  let findDependencies (domainTypes : DomainTypes) (q : Expr<'T>) : MetaVariable array =
    match q with
    | Call (x, mi, args) ->
      let ps = mi.GetParameters ()
      if ps.Length > 0 then
        if mi.IsGenericMethod then
          let gmi   = mi.GetGenericMethodDefinition ()
          let gas   = gmi.GetGenericArguments ()
          let ga    = gas.[0]
          let gpcs  = ga.GetGenericParameterConstraints ()
          gpcs |> Array.map (fun gpc -> domainTypes.[gpc])
        else
          let p  = ps.[0]
          let mv = domainTypes.[p.ParameterType]
          [|mv|]
      else
        failwithf "Method '%s' is expected to have at least one parameter but had 0" mi.Name
    | _ ->
      failwithf "printDependencies invoked with the wrong shape. Expected shape like this: <@f env args@>"
    
  let invoke (domainTypes : DomainTypes) cenv (q : Expr<'T>) : 'T =
    let mvs = findDependencies domainTypes q
    let env = 
      mvs
      |> Array.map (fun mv -> mv.DomainType, mv.UntypedEvaluator ())
      |> dict
      |> cenv

    let nq = 
      match q with
      | Call (x, mi, args) ->
        let (_::t) = args
        let a = (Expr.Value env)::t
        match x with
        | Some x  -> Expr.Call (x, mi, a)
        | None    -> Expr.Call (mi, a)
        
      | _ ->
        failwithf "printDependencies invoked with the wrong shape. Expected shape like this: <@f env args@>"

    LeafExpressionConverter.EvaluateQuotation nq :?> 'T

  let printDependencies (domainTypes : DomainTypes) (q : Expr<'T>) =
    let mvs = findDependencies domainTypes q
    printfn "Method %d dependencies" mvs.Length
    for mv in mvs do
      printfn "  MetaVariable: %A" mv
