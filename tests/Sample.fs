module Tests

open System
open Expecto
open AlpacaFs


// [<Tests>]
// let assetTests =
//   testList "Asset Tests" [
//     testCase "Get Apple asset by ID" <|
//       fun _ ->
//         let apple = Asset.get (Asset.IDOrSymbol.ID "b0b6dd9d-8b9b-48a9-ba46-b9d54906e415")
//         Expect.isOk apple "apple should be returned as OK"

//     testCase "Get MSFT asset by symbol" <|
//       fun _ ->
//         let msft = Asset.get (Asset.IDOrSymbol.Symbol "MSFT")
//         Expect.isOk msft "msft should be returned as OK"

//     testCase "list all assets" <| 
//       fun _ -> 
//         let all = Asset.list (Asset.Status.Active) (Asset.Class'.USEquity)
//         Expect.isOk all "all should be returned as ok"
//   ]


// [<Tests>]
// let watchlistTest = 
//   testList "Watchlist Tests" [
    
//   ]





// [<Tests>]
// let calendarTest = 
//   testList "Calendar Tests" [
//     testCase "Get market days dates" <| 
//       fun _ -> 
//         let marketDay = Calendar.list (new DateTime(2021, 02, 28)) (new DateTime(2021, 02, 28))
//         Expect.isOk marketDay "It should return an ok market day"
//   ]

// [<Tests>]
// let clockTest = 
//   testList "Clock Tests" [
//     testCase "Get clock for tooday" <| 
//       fun _ -> 
//         let clock = Clock.get ()
//         Expect.isOk clock "It should return an ok clock"
//   ]