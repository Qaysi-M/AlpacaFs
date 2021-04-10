namespace AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl

type Clock = {timestamp: DateTime; is_open: bool; next_open: DateTime; next_close: DateTime} with 
    static member Zero = {timestamp = DateTime.Now; is_open = false; next_open = DateTime.Now; next_close = DateTime.Now}

[<RequireQualifiedAccessAttribute>]
module rec Clock = 

    let private CLOCK_POINT = Url.Combine(BASE_POINT, "/clock" )
    
    /// <summary> Returns whether the market is close or open, the next open date,  and the next close date</summary>
    let get () = 
        fun () ->
            Http.Request( CLOCK_POINT,
                    httpMethod = "GET",
                    headers = HEADERS)
        |> handleResponse<Clock> 