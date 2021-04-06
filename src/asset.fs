namespace rec AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl

type Asset = {
        id: string
        [<JsonField("class", Transform=typeof<Asset.ClassTransform>)>] class': Asset.Class'
        exchange: Exchange
        symbol: string
        [<JsonField(Transform=typeof<Asset.StatusTransform>)>] status: Asset.Status
        tradable: bool
        marginable: bool
        shortable: bool
        easy_to_borrow: bool
        fractionable: bool
    }

[<RequireQualifiedAccessAttribute>]
module Asset =
    let get =
        function
        | ID (id) ->
            let ASSET_POINT id = Url.Combine(ASSETS_POINT, id)
            let config = JsonConfig.create(jsonFieldNaming = Json.lowerCamelCase)
            fun () -> 
                Http.Request( ASSET_POINT id,
                            httpMethod = "GET",
                            headers = HEADERS)
            |> handleResponse<Asset> 

        | Symbol symbol ->
            let ASSET_POINT symbol = Url.Combine(ASSETS_POINT, symbol)
            fun () -> 
                Http.Request( ASSET_POINT symbol,
                        httpMethod = "GET",
                        headers = HEADERS)
            |> handleResponse<Asset> 

    // -- TO DO : add possibly all status --    
    let list (status: Status) (class': Class') =
        let query = ["status", Status.string status; "asset_class", Class'.string class']
        fun () -> 
            Http.Request( ASSETS_POINT,
                    httpMethod = "GET",
                    query = query,
                    headers = HEADERS)
        |> handleResponse<Asset list> 



    // -- Modeling --

    type Class' = | USEquity with
        static member internal string = function USEquity -> "us_equity"
    and internal ClassTransform() =
            interface ITypeTransform with
                member x.targetType () = typeof<string>
                member x.toTargetType class' = Class'.string (class' :?> Class') :> obj
                member x.fromTargetType (str) = 
                    match (str :?> string) with 
                    | "us_equity" -> USEquity | _ -> USEquity 
                    :> obj

    type Status = Active | Inactive with
        static member internal string = function Active -> "active" | Inactive -> "inactive"
    and internal StatusTransform() =
            interface ITypeTransform with
                member x.targetType () = typeof<string>
                member x.toTargetType status = Status.string (status :?> Status) :> obj
                member x.fromTargetType (str) = 
                    match (str :?> string) with 
                    | "active" -> Active | "inactive" -> Inactive | _ -> Inactive 
                    :> obj

    type IDOrSymbol =
        | ID of string
        | Symbol of string
        
    let private ASSETS_POINT = Url.Combine(BASE_POINT, "/assets" )