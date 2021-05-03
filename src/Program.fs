


module internal Program

//let aapl = getBars "2021-03-01T09:30:00.00-05:00" "2021-03-03T20:00:00.00-05:00" "AAPL"
//let msft = getBars "2021-03-01T09:30:00.00-05:00" "2021-03-03T20:00:00.00-05:00" "MSFT"
//let amcPosition =  Order.place "AMC" 1 "buy" "market" "gtc"
//let orders = Order.list

open System
open AlpacaFs
open Config


[<EntryPoint>]
let main argv =
    let stream = new Data.RealTime.StreamBuilder()
    
    let onMessage (trades:Trade array) = printfn "%A" trades

    stream {
        yield! Data.RealTime.connect()
        yield! Data.RealTime.authenticate()
        let! msg =  Data.RealTime.subscribe ["PLTR"; "AAPL"] [] []
        yield! Data.RealTime.onMessage(onMessage)
        return ""
    } |> Stream.run (Data.RealTime.create()) |> ignore
   
    0