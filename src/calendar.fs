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
} with 
    static member Zero = {date = DateTime.MinValue; open' = DateTime.MinValue; close = DateTime.MinValue}

[<RequireQualifiedAccessAttribute>]
module Calendar = 
    let private CALENDAR_POINT = Url.Combine(BASE_POINT, "/calendar" )

    /// <summary> Return market days</summary>
    /// <param name="start"> start ∈ [1970, 2029] </param>
    /// <param name="end'"> end' ∈ [1970, 2029] </param>
    let list (start: DateTime) (end': DateTime) =
        fun () -> 
            Http.Request( CALENDAR_POINT,
                    httpMethod = "GET",
                    query = ["start", string start; "end", string end'],
                    headers = HEADERS)
        |> handleResponse<MarketDay list> 


   

    
    