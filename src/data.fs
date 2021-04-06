namespace rec AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl

open System.Threading
open System.Net.WebSockets

// TO DO : pass DateTime instead of string

type Trades = { trades: Trade list; symbol: string; next_page_token: string option}
and Trade = { [<JsonField("S")>] S': string option; t: DateTime; x: string; p: decimal; s: int; c: string list; i: int64; z: string} 
        
type Quotes = {quotes: Quote list; symbol: string; next_page_token: string option}
and Quote = {[<JsonField("S")>] S': string option; t: DateTime; ax: string;  ap: decimal; [<JsonField("as")>] as': int; bx: string; bp: decimal; bs: int; c: string list; z: string option}

type Bars = { bars: Bar list; symbol: string; next_page_token: string option}
and Bar = {[<JsonField("S")>] S': string option; t: DateTime; o: decimal; h: decimal; l: decimal; c: decimal; v: int}

[<RequireQualifiedAccess>]
module Data = 
    type Schema = T | Q | B
    with 
        static member fromString str =
            match toUpperUnion<Schema> str with
            | Some case -> case
            | None -> T

    type SchemaRecord = {[<JsonField(Transform=typeof<JsonTransforms.SchemaTransform>)>] t: Schema}
    
    [<RequireQualifiedAccess>]
    module Historical = 
        let DATA_POINT = "https://data.alpaca.markets/v2"
        
        let tz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
        let EST t = TimeZoneInfo.ConvertTimeFromUtc(t, tz)    
        
        [<RequireQualifiedAccess>]
        module Trades = 
            let TRADES_POINT symbol = Url.Combine(DATA_POINT, "stocks", symbol, "trades")
            let get (start: string) (end': string) (symbol: string) = 
                let response = 
                    fun () ->
                        Http.Request( TRADES_POINT symbol, 
                                        httpMethod = "GET",
                                        query = ["start", start; "end", end'],
                                        headers = HEADERS)
                handleResponse<Trades> response

        [<RequireQualifiedAccess>]
        module Quotes = 
            let QUOTES_POINT symbol = Url.Combine(DATA_POINT, "stocks", symbol, "quotes")

            let get (start: string) (end': string) (symbol: string) (limit: int)= 
                let response = 
                    fun () ->
                        Http.Request( QUOTES_POINT symbol, 
                                            httpMethod = "GET",
                                            query = ["start", start; "end",  end'; "limit", string limit],
                                            headers = HEADERS)
                handleResponse<Quotes> response
                 
        [<RequireQualifiedAccess>]
        module Bars =    
            let BARS_POINT symbol = Url.Combine(DATA_POINT, "stocks", symbol, "bars")                             

            let get (start: string) (end': string) (symbol: string) (limit: int) (timeFrame: TimeFrame) = 
                let reponse =      
                    fun () ->
                        Http.Request( BARS_POINT symbol, 
                                            httpMethod = "GET",
                                            query = ["start", string start; "end", string end'; "limit", "1000"; "timeframe", TimeFrame.string timeFrame ],
                                            headers = HEADERS)
                handleResponse<Bars> reponse  

    [<RequireQualifiedAccess>]
    module Real = 
        type Stream = ClientWebSocket

        let private STREAM_POINT = "wss://stream.data.alpaca.markets/v2/iex"
        //let private STREAM_POINT =  "wss://paper-api.alpaca.markets/stream"

        type State<'s, 'a> = State of ('s -> ('a * 's))

        type Authentication= {action: string ; key: string ; secret: string}

        type Access= {action: string ; trades: string list ; quotes: string list ; bars: string list}

        type Successes = Success list
        and Success = {T: string; msg: string}

        type Errors = Error list
        and Error = {T: string; code: int; msg: string}

        let inline run stream (State f) = 
            f stream
        
        let bind (x, f) =
            fun stream -> 
                let (result, newStream) = run stream x 
                run newStream (result |> f)
            |> State 


        type StreamBuilder(stream: Stream) = 
            member _.Zero () = State(fun s -> ((), s))
            member _.Return x = State(fun s -> (x, s))  
            member inline _.ReturnFrom x = x
            member _.Yield x = State(fun s -> (x, s)) 
            member inline _.YieldFrom x = x

            member _.Bind (x, f) = bind(x, f)
            member _.Delay f = f()
            member _.Combine (x1, x2)=
                fun stream ->
                    let (_, newStream) = run stream x1 
                    run newStream x2
                |> State   
            member _.Run x =
                run stream x
              
        
        let create () : Stream = 
            new ClientWebSocket()
        
        let connect()= 
            fun (stream: Stream) ->
                async {
                    do!  stream.ConnectAsync(STREAM_POINT |> Uri, CancellationToken.None) 
                          |> Async.AwaitTask
                    let buf = Array.init 256 (fun a -> 0uy)
                    do!  
                        stream.ReceiveAsync(buf |> ArraySegment, CancellationToken.None)
                        |> Async.AwaitTask
                        |> Async.Ignore 
                    return
                        (Text.Encoding.Default.GetString(buf), stream)
                } |> Async.RunSynchronously
                 
            |> State
                        
        let authenticate() = 
            let authentication = 
                {action = "auth"; key = AlpacaFs.Config.API_KEY; secret = AlpacaFs.Config.SECRET_KEY}
                |> Json.serialize
            fun (stream: Stream) ->
                async { 
                    let authBuf = Text.Encoding.Default.GetBytes authentication
                    do!
                        stream.SendAsync(authBuf |> ArraySegment, WebSocketMessageType.Text, true, CancellationToken.None)
                        |> Async.AwaitTask

                    let buf = Array.init 256 (fun a -> 0uy)
                    do! 
                        stream.ReceiveAsync(buf |> ArraySegment, CancellationToken.None)
                        |> Async.AwaitTask
                        |> Async.Ignore
                    return
                        (Text.Encoding.Default.GetString(buf), stream)
                } |> Async.RunSynchronously
            |> State

        let subscribe() =
            let subscription = 
                { action = "subscribe"; trades = ["AAPL"]; quotes = []; bars = [] }
                |> Json.serialize
            fun (stream: Stream) ->      
                async {
                    let subscriptionBuffer = Text.Encoding.Default.GetBytes subscription
                    do!
                        stream.SendAsync(subscriptionBuffer |> ArraySegment, WebSocketMessageType.Text, true, CancellationToken.None)
                        |> Async.AwaitTask

                    let buf = Array.init 256 (fun a -> 0uy)
                    do! 
                        stream.ReceiveAsync(buf |> ArraySegment, CancellationToken.None)
                        |> Async.AwaitTask
                        |> Async.Ignore
                    return
                        (Text.Encoding.Default.GetString(buf), stream)
                } |> Async.RunSynchronously
            |> State

        let listen() =
            fun (stream: Stream) ->
                async {
                    let buf = Array.init 256 (fun a -> 0uy)
                    do! 
                        stream.ReceiveAsync(buf |> ArraySegment, CancellationToken.None)
                        |> Async.AwaitTask
                        |> Async.Ignore
                    return
                        (Text.Encoding.Default.GetString(buf)|> Json.deserialize<Trade list>, stream)
                } |> Async.RunSynchronously
            |> State

        let status() =
            fun (stream: Stream) ->
                printfn "%A" stream.State
                (stream.State, stream)
            |> State
