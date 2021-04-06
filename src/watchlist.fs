namespace AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl

 
// ---- TO DO -----
// get update etc by name instead of id

type Watchlist = {
        account_id: string
        assets: Asset list option
        created_at: DateTime
        id: string
        name: string
        updated_at: DateTime option
    }


[<RequireQualifiedAccess>]
module Watchlist =

    let WATCHLISTS_POINT = Url.Combine(BASE_POINT, "/watchlists" )
    let WATCHLIST_POINT id = Url.Combine(WATCHLISTS_POINT, id)
    let WATCHLIST_POINT_BY_NAME = Url.Combine(BASE_POINT, "/watchlists:by_name" )

    type NameOrID = Name of string | ID of string

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

    let get = function
        | ID id ->  
            fun () ->
                Http.Request( WATCHLIST_POINT id,
                                httpMethod = "GET",
                                headers = HEADERS)
            |> handleResponse<Watchlist> 
        | Name name ->  
            fun () -> 
                Http.Request( WATCHLIST_POINT_BY_NAME,
                            httpMethod = "GET",
                            query = ["name", name],
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