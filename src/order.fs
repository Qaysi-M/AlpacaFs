
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
        type': string option
        side: string option
        time_in_force: string option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] limit_price: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] stop_price: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] filled_avg_price: decimal option
        status: string option
        extended_hours: bool
        legs: string list option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] trail_price: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] trail_percent: decimal option
        [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] hwm: decimal option
    } with 
    static member Zero = {
        id = ""
        client_order_id = None  
        created_at = DateTime.MinValue
        updated_at = None 
        submitted_at = None 
        filled_at = None
        expired_at = None
        canceled_at = None 
        failed_at = None
        replaced_at = None
        replaced_by = None 
        replaces = None
        asset_id = ""
        symbol = ""
        asset_class = Asset.Class'.USEquity
        qty = decimal 0
        filled_qty = None 
        type' = None 
        side = None
        time_in_force = None
        limit_price = None
        stop_price = None
        filled_avg_price = None
        status = None
        extended_hours = false
        legs = None
        trail_price = None
        trail_percent = None
        hwm = None 
    }

[<RequireQualifiedAccess>]
module Order =

    let private ORDERS_POINT = Url.Combine(BASE_POINT, "/orders" )

    [<RequireQualifiedAccess>]
    type Status =  Open | Closed  | All with 
        override this.ToString() = 
           nameof(this).ToLower()

    [<RequireQualifiedAccess>]
    type Direction = ASC | DSC

    [<RequireQualifiedAccess>]
    type Type' =  Market | Limit | Stop | Stop_limit | Trailing_stop with    
        override this.ToString() = 
           nameof(this).ToLower()

    [<RequireQualifiedAccess>]
    type Side = Buy | Sell with 
        override this.ToString() = 
           nameof(this).ToLower()

   // -- Functions --

    let list (status:Status) (limit:int) = 
        let query = ["status",  status.ToString(); "limit", string limit]
        fun () ->
            Http.Request( ORDERS_POINT,
                    query = query,
                    httpMethod = "GET",
                    headers = HEADERS)
        |> handleResponse<Order list>
    
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

    let place (symbol: string) (qty: int) (side: Side) (type': Type') (tif: TimeInForce) = 
        let data = 
            {|symbol = symbol; qty = string qty; side = side.ToString(); type' = type'.ToString(); time_in_force = tif.ToString()|}
            |> Json.serialize 
        fun () ->
            Http.Request( ORDERS_POINT, 
                        httpMethod = "POST",
                        body = TextRequest data,
                        headers = HEADERS
                        )
        |> handleResponse<Order>

    let replace qty (order: Order) = 
        let ORDER_POINT id = Url.Combine(ORDERS_POINT, id)
        let data = 
            {| qty = qty |}
            |> Json.serialize
        fun () ->
            Http.Request( ORDER_POINT order.id,
                            httpMethod = "PATCH",
                            body = TextRequest data,
                            headers = HEADERS)
        |> handleResponse<Order> 

    let cancel (order: Order) = 
        let ORDER_POINT id = Url.Combine(ORDERS_POINT, id)
        fun () ->
            Http.Request( ORDER_POINT order.id,
                            httpMethod = "DELETE",
                            headers = HEADERS)
        |> handleDeleteResponse

    let cancelAll () =
        fun () ->
            Http.Request( ORDERS_POINT,
                            httpMethod = "DELETE",
                            headers = HEADERS)
        |> handleDeleteResponse
    

    