namespace rec AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl


type MarketDay = {
    date: DateTime
    [<JsonField("open")>] open': DateTime
    close: DateTime
}

[<RequireQualifiedAccessAttribute>]
module Calendar = 

    let list (start: DateTime) (end': DateTime) =
        fun () -> 
            Http.Request( CALENDAR_POINT,
                    httpMethod = "GET",
                    query = ["start", string start; "end", string end'],
                    headers = HEADERS)
        |> handleResponse<MarketDay list> 


    // -- Modeling --

    let CALENDAR_POINT = Url.Combine(BASE_POINT, "/calendar" )
    