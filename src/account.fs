
namespace AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl


type Account = {
        account_blocked: bool
        account_number: string
        buying_power: string
        cash: string
        created_at: DateTime
        currency: string
        daytrade_count: int
        daytrading_buying_power: string
        equity: string
        id: string
        initial_margin: string
        last_equity: string
        last_maintenance_margin: string
        long_market_value: string
        maintenance_margin: string
        multiplier: string
        pattern_day_trader: bool
        portfolio_value: string
        regt_buying_power: string
        short_market_value: string
        shorting_enabled: bool
        sma: string
        status: string
        trade_suspended_by_user: bool
        trading_blocked: bool
        transfers_blocked: bool
    }
[<RequireQualifiedAccess>]
module Account = 
    let ACCOUNT_POINT = Url.Combine(BASE_POINT, "/account" )
    
    let get () = 
        Http.RequestString( ACCOUNT_POINT,
                            httpMethod = "GET",
                            headers = HEADERS)
        |> Json.deserialize<Account>
    type Configuration = {
            dtbp_check: string
            no_shorting: bool
            suspend_trade: bool
            trade_confirm_email: string
        }
    [<RequireQualifiedAccess>]
    module Configuration = 
        let CONFIGURATION_POINT = Url.Combine(ACCOUNT_POINT, "/configurations" )

        let get () = 
            Http.RequestString( CONFIGURATION_POINT,
                                httpMethod = "GET",
                                headers = HEADERS)
            |> Json.deserialize<Configuration>

        
        let update (configuration: Configuration) =
            let body = 
                configuration
                |> Json.serialize
            Http.RequestString( CONFIGURATION_POINT,
                                httpMethod = "PATCH",
                                body = TextRequest body,
                                headers = HEADERS)
            |> Json.deserialize<Configuration>
    
    type Activity = Trade of Trade | NonTrade of NonTrade
    and Trade = {
            activity_type:  string option
            cum_qty: Decimal option
            id:  string option
            leaves_qty:  string option
            price:  Decimal option
            qty:  string option
            side:  string option
            symbol:  string  option
            transaction_time:  string option
            order_id:  string option
            type':  string option
        }
    and NonTrade = {
            activity_type: string option
            id: string option
            date: string option
            net_amount: string option
            symbol: string option
            [<JsonField(Transform=typeof<JsonTransforms.DecimalTransform>)>] qty: Decimal option
            per_share_amount: string option
        }
            
    [<RequireQualifiedAccess>]
    module Activity =  
        [<RequireQualifiedAccess>]
        type Type' = 
            FILL | TRANS | MISC | ACATC | ACATS | CSD | CSR | DIV | DIVCGL | DIVCGS | DIVFEE | DIVFT | DIVNRA | DIVROC | DIVTW
            | DIVTXEX| INT| INTNRA| INTTW| JNL| JNLC| JNLS| MA| NC| OPASN| OPEXP| OPXRC| PTC| PTR| REORG| SC| SSO 
        let private ACTIVITIES_POINT = Url.Combine(ACCOUNT_POINT, "/activities")
        let get (activityType: Type') =
            let ACTIVITY_POINT activity_type = Url.Combine(ACTIVITIES_POINT, activity_type)
            let jsonConfig = JsonConfig.create(allowUntyped = true)
            let activityType = string activityType
            Http.RequestString( ACTIVITY_POINT activityType,
                                httpMethod = "GET",
                                query = ["activity_type", activityType],
                                headers = HEADERS)
            |> Json.deserializeEx<obj list>  jsonConfig
            |> List.map (Json.serializeEx jsonConfig)
            |> List.map 
                (fun activity -> 
                    try
                        activity |> Json.deserialize<Trade> |> Trade
                    with
                    | :? (JsonDeserializationError) as err1 -> 
                        try
                            activity |> Json.deserialize<NonTrade> |> NonTrade
                        with 
                        | :? (JsonDeserializationError) as err2 ->
                            failwithf "possible errors: \n first: %s \n second: %s" err1.Message err2.Message)
            
        let list (activityTypes: Type' list) =
            let activityTypes = activityTypes |>List.map string
            let activityTypes = activityTypes |> List.reduce (fun a b -> a + "," + b)
            let jsonConfig = JsonConfig.create(allowUntyped = true)
            Http.RequestString( ACTIVITIES_POINT,
                                httpMethod = "GET",
                                query = ["activity_types", activityTypes],
                                headers = HEADERS)
            |> Json.deserializeEx<obj list>  jsonConfig
            |> List.map (Json.serializeEx jsonConfig)
            |> List.map 
                (fun activity -> 
                    try
                        activity |> Json.deserialize<Trade> |> Trade
                    with
                    | :? (JsonDeserializationError) as err1 -> 
                        try
                            activity |> Json.deserialize<NonTrade> |> NonTrade
                        with 
                        | :? (JsonDeserializationError) as err2 ->
                            failwithf "possible errors: \n first: %s \n second: %s" err1.Message err2.Message)

    [<RequireQualifiedAccess>]
    module Portfolio = 
        let private PORTFOLIO_POINT = Url.Combine(ACCOUNT_POINT, "/portfolio" )
        type History = {
            timestamp: DateTime list
            equity: double list
            profit_loss: double list
            profit_loss_pct: double list
            base_value: double
            timeframe: string
        }
        [<RequireQualifiedAccess>]
        module History = 
            let private HISTORY_POINT = Url.Combine(PORTFOLIO_POINT, "/history")

            let list period timeframe date_end extended_hours = 
                let query = ["period", period; "timeframe", timeframe; "date_end", date_end; "extended_hours", extended_hours]
                Http.RequestString( HISTORY_POINT,
                                    httpMethod = "GET",
                                    query = query,
                                    headers = HEADERS)
                |> Json.deserialize<History>  




