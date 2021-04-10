namespace AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl


// -- TO DO : side could be made to its own type -- 

type Position = {
        asset_id: string
        symbol: string
        exchange: Exchange option
        [<JsonField(Transform=typeof<Asset.ClassTransform>)>] asset_class: Asset.Class' option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] avg_entry_price: decimal option 
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] qty: decimal option 
        side: string option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] market_value: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] cost_basis: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] unrealized_pl: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] unrealized_plpc: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] unrealized_intraday_pl: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] unrealized_intraday_plpc: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] current_price: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] lastday_price: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] change_today: decimal option
    }



[<RequireQualifiedAccess>]
module Position = 
    
    type IDOrSymbol = 
        | ID of string
        | Symbol of string
    
    let private POSITIONS_POINT = Url.Combine(BASE_POINT, "/positions" )


    let getByID (id: string) = 
        let POSITION_POINT id = Url.Combine(POSITIONS_POINT, id)
        fun () ->
            Http.Request( POSITION_POINT id,
                            httpMethod = "GET",
                            headers = HEADERS)
        |> handleResponse<Position> 

    let getBySymbol (symbol: string) =
        let POSITION_POINT symbol = Url.Combine(POSITIONS_POINT, symbol)
        fun () ->
            Http.Request( POSITION_POINT symbol,
                            httpMethod = "GET",
                            headers = HEADERS)
        |> handleResponse<Position> 

    let list () =
        fun () ->
            Http.Request( POSITIONS_POINT,
                    httpMethod = "GET",
                    headers = HEADERS)
        |> handleResponse<Position list> 


    let close qty (position: Position) =
        let POSITION_POINT id = Url.Combine(POSITIONS_POINT, id)
        fun () ->
            Http.Request( POSITION_POINT position.asset_id,
                            query = ["qty", qty],
                            httpMethod = "DELETE",
                            headers = HEADERS)
        |> handleResponse<Position> 

    let closeAll () =
        fun () ->
            Http.Request( POSITIONS_POINT,
                        httpMethod = "DELETE",
                        headers = HEADERS)  
        |> handleResponse<Position list> 

    