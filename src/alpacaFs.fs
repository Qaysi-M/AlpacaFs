namespace AlpacaFs
open FSharp.Data
open FSharp.Json
open FSharp.Reflection
open System

type Exchange = AMEX | ARCA | BATS | NYSE | NASDAQ | NYSEARCA with    
    static member private string exchange = 
           nameof(exchange)


type TimeInForce = DAY | GTC | OPG | CLS | IOC | FOK with    
    static member private string tif = 
           nameof(tif).ToLower()


type TimeFrame = Min | Hour | Day with    
    static member internal string  = function 
       | Min -> "1Min" | Hour -> "1Hour" | Day -> "1Day"

[<AutoOpenAttribute>]
module internal Helpers = 
    let handleResponse<'T> (request: unit -> HttpResponse) =  
            try 
                match request().Body with
                    | Text text -> 
                        try 
                            text |> Json.deserialize<'T> |> Ok
                        with   
                            | :? FSharp.Json.JsonDeserializationError as ex -> ex.Message |> Error
                    | _ -> Error ("Not Text")
            with
                | :? System.Net.WebException as ex -> ex.Message |> Error
            

    let handleDeleteResponse<'T> (request: unit -> HttpResponse) = 
            try 
                match request().Body with
                    | Text text -> text |> Ok
                    | _ -> Error ("Not Text")
            with
                | :? System.Net.WebException as ex -> ex.Message |> Error          
   
            
    let toUpperUnion<'a> (s: string) =
        match FSharpType.GetUnionCases typeof<'a> |> Array.filter(fun case -> case.Name = s.ToUpper()) with
        | [|case|] -> Some(FSharpValue.MakeUnion(case, [||]) :?> 'a)
        | _ ->  None


    let dateToString (date: DateTime) = 
        System.Text.Json.JsonSerializer.Deserialize<string>(System.Text.Json.JsonSerializer.Serialize(date))