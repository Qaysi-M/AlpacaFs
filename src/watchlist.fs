namespace AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl

 
// ---- TO DO -----
// get update etc by name instead of id

type Watchlist = {account_id: string; assets: Asset list option; created_at: DateTime; id: string; name: string; updated_at: DateTime option} with
    static member Zero = {account_id = ""; assets = None; created_at = DateTime.MinValue; id = ""; name = ""; updated_at = None}

[<RequireQualifiedAccess>]
module Watchlist =

    let private WATCHLISTS_POINT = Url.Combine(BASE_POINT, "/watchlists" )
    let private WATCHLIST_POINT id = Url.Combine(WATCHLISTS_POINT, id)
    let private WATCHLIST_POINT_BY_NAME = Url.Combine(BASE_POINT, "/watchlists:by_name" )


    let create (name: string) (symbols: string list) = 
        let body = 
            {| name = name; symbols = symbols |}
            |> Json.serialize
        fun () -> 
            Http.Request( WATCHLISTS_POINT,
                        httpMethod = "POST",
                        body = TextRequest body,
                        headers = HEADERS)
        |> handleResponse<Watchlist> 

    let getByID id = 
        fun () ->
            Http.Request( WATCHLIST_POINT id,
                            httpMethod = "GET",
                            headers = HEADERS)
        |> handleResponse<Watchlist> 
    let getBySymbol symbol =  
        fun () -> 
            Http.Request( WATCHLIST_POINT_BY_NAME,
                        httpMethod = "GET",
                        query = ["name", symbol],
                        headers = HEADERS)
        |> handleResponse<Watchlist> 

    let delete (watchlist: Watchlist) = 
        fun () -> 
            Http.Request( WATCHLIST_POINT watchlist.id,
                        httpMethod = "DELETE",
                        headers = HEADERS)
        |>  handleDeleteResponse<String>  


    let list () =  
        fun () -> 
            Http.Request( WATCHLISTS_POINT,
                        httpMethod = "GET",
                        headers = HEADERS)
        |> handleResponse<Watchlist list> 
    
    let updateName (name: string) (watchlist: Watchlist) = 
        let body = 
            {| name = name |}
            |> Json.serialize
        
        fun () -> 
            Http.Request( WATCHLIST_POINT watchlist.id,
                        httpMethod = "PUT",
                        body = TextRequest body,
                        headers = HEADERS)
        |> handleResponse<Watchlist> 


    let updateAssets (symbols: string list) (watchlist: Watchlist) = 
        let body = 
            {| symbols = symbols |}
            |> Json.serialize 
        fun () -> 
            Http.Request( WATCHLIST_POINT watchlist.id,
                        httpMethod = "PUT",
                        body = TextRequest body,
                        headers = HEADERS)
        |> handleResponse<Watchlist> 

    let addAsset (symbol: string) (watchlist: Watchlist) = 
        let body = 
            {| symbol = symbol |}
            |> Json.serialize
        fun () -> 
            Http.Request( WATCHLIST_POINT watchlist.id,
                        httpMethod = "POST",
                        body = TextRequest body,
                        headers = HEADERS)
        |> handleResponse<Watchlist>     

    let removeAsset (symbol: string) (watchlist: Watchlist) = 
        let ASSET_POINT symbol watchlistPoint = Url.Combine(watchlistPoint, symbol) 
        fun () -> 
            Http.Request( WATCHLIST_POINT watchlist.id |> ASSET_POINT symbol,
                        httpMethod = "DELETE",
                        headers = HEADERS)
        |> handleResponse<Watchlist> 