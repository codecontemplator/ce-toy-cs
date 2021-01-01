open FsDepedencies
open Model

let absoluteMaxAmount env amountLimit =
  let amount  = env |> getAmount

  min amount amountLimit

let maxTotalDebt env debtLimit =
  let amount      = env |> getAmount
  let creditA     = env |> getCreditA
  let creditB     = env |> getCreditB
  let totalCredit = creditA + creditB

  if totalCredit > debtLimit then 0.0M else amount

let printDependencies q = Core.printDependencies domainTypes q
let invoke            q = Core.invoke domainTypes Env q

[<EntryPoint>]
let main argv =

  printDependencies <@ absoluteMaxAmount env 100.0M @>
  printDependencies <@ maxTotalDebt env 100.0M @>

  printfn "Result: %A" <| invoke <@ maxTotalDebt env 100.0M @>

  0
