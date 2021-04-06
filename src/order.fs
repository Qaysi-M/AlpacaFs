
namespace rec AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl

type Order = {
        id: string 
        client_order_id: string option 
        created_at: DateTime
        updated_at: DateTime option
        submitted_at: DateTime option
        filled_at: DateTime option
        expired_at: DateTime option
        canceled_at: DateTime option
        failed_at: DateTime option
        replaced_at: DateTime option
        replaced_by: string option
        replaces: string option
        asset_id: string
        symbol: string
        [<JsonField(Transform=typeof<Asset.ClassTransform>)>] asset_class: Asset.Class'
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] qty: decimal 
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] filled_qty: decimal option
        type': Order.Type' option
        side: string option
        time_in_force: TimeInForce option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] limit_price: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] stop_price: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] filled_avg_price: decimal option
        status: Order.Status option
        extended_hours: bool
        legs: Order list option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] trail_price: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] trail_percent: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] hwm: decimal option
    }

[<RequireQualifiedAccess>]
module Order =

    let ORDERS_POINT = Url.Combine(BASE_POINT, "/orders" )

    let list (status:Status) (limit:int) = 
        let query = ["status",  (string status).ToLower(); "limit", string limit]
        fun () ->
            Http.Request( ORDERS_POINT,
                    query = query,
                    httpMethod = "GET",
                    headers = HEADERS)
        |> handleResponse<Order>
    
    let get id = 
        let ORDER_POINT id = Url.Combine(ORDERS_POINT, id)
        fun () ->
            Http.Request( ORDER_POINT id,
                    httpMethod = "GET",
                    headers = HEADERS)
        |> handleResponse<Order>        
    
    let getByClientOrderID id = 
        let CLIENT_ORDERS_POINT = Url.Combine(BASE_POINT, "/orders:by_client_order_id" )
        fun () ->
            Http.Request( CLIENT_ORDERS_POINT,
                            query = ["client_order_id", id],
                            httpMethod = "GET",
                            headers = HEADERS)
        |> handleResponse<Order>    

    type OrderBodyParam = {symbol: string; qty: int; side: string; [<JsonField("type")>] type': string; time_in_force: string}
    let place symbol qty side type' tif = 
        let data = 
            {symbol = symbol; qty = qty; side = side; type' = type'; time_in_force = tif }
            |> Json.serialize 
        fun () ->
            Http.Request( ORDERS_POINT, 
                        httpMethod = "POST",
                        body = TextRequest data,
                        headers = HEADERS
                        )
        |> handleResponse<Order>

    let replace qty id = 
        let ORDER_POINT id = Url.Combine(ORDERS_POINT, id)
        let data = 
            {| qty = qty |}
            |> Json.serialize
        fun () ->
            Http.Request( ORDER_POINT id,
                            httpMethod = "PATCH",
                            body = TextRequest data,
                            headers = HEADERS)
        |> handleResponse<Order> 

    let cancel id = 
        let ORDER_POINT id = Url.Combine(ORDERS_POINT, id)
        fun () ->
            Http.Request( ORDER_POINT id,
                            httpMethod = "DELETE",
                            headers = HEADERS)
        |> handleDeleteResponse

    let cancelAll () =
        fun () ->
            Http.Request( ORDERS_POINT,
                            httpMethod = "DELETE",
                            headers = HEADERS)
        |> handleDeleteResponse
    

    // -- Modeling --
    
    type Status =  Open | Closed  | All

    type Direction = ASC | DSC

    type Type' =  Market | Limit | Stop | Stop_limit | Trailing_stop
    with    
        static member private string exchange = 
           nameof(AMEX).ToLower()