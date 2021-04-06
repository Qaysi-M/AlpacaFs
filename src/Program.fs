


module Program

//let aapl = getBars "2021-03-01T09:30:00.00-05:00" "2021-03-03T20:00:00.00-05:00" "AAPL"
//let msft = getBars "2021-03-01T09:30:00.00-05:00" "2021-03-03T20:00:00.00-05:00" "MSFT"
//let amcPosition =  Order.place "AMC" 1 "buy" "market" "gtc"
//let orders = Order.list

open System
open AlpacaFs
open Config


[<EntryPoint>]
let main argv =
   
    let stream = new Data.Real.StreamBuilder(Data.Real.create())
    
    stream {
        yield! Data.Real.connect()
        yield! Data.Real.authenticate()
        let! msg =  Data.Real.subscribe()
        printfn "%s" msg
    }
    
    let rec loop (k: ConsoleKeyInfo) = 
        match k with 
        |k when k.Key.Equals(ConsoleKey.Enter) -> 
            stream {
                let! msg = Data.Real.listen ()
                msg
                |> printfn "%A"    
            }
            
        | _ ->  
            stream {
                let! msg = Data.Real.listen ()
                msg
                |> printfn "%A"    
            } |> ignore
            loop k
    let keyStroke = Console.ReadKey()
    loop keyStroke
    0