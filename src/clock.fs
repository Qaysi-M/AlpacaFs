namespace AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl

type Clock = {
        timestamp: DateTime
        is_open: bool
        next_open: DateTime
        next_close: DateTime
    }

[<RequireQualifiedAccessAttribute>]
module rec Clock = 

    let CLOCK_POINT = Url.Combine(BASE_POINT, "/clock" )
    
    let get () = 
        fun () ->
            Http.Request( CLOCK_POINT,
                    httpMethod = "GET",
                    headers = HEADERS)
        |> handleResponse<Clock> 