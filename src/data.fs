namespace rec AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl
open Json.Net

open System.Threading
open System.Net.WebSockets


type Trades = { trades: Trade array; symbol: string; next_page_token: string option} with 
    static member Zero = {trades = [||]; symbol = ""; next_page_token = None}
and [<CLIMutable>] Trade = { x: string; p: decimal; c: string array; i: int64} 
        
type Quotes = {  quotes: Quote array; symbol: string; next_page_token: string option} with 
    static member Zero = {quotes = [||]; symbol = ""; next_page_token = None}
and Quote = {[<JsonField("S")>] S': string option; t: DateTime; ax: string;  ap: decimal; [<JsonField("as")>] as': int; bx: string; bp: decimal; 
                bs: int; c: string list; z: string option} with 
    static member Zero = 
        {S' = None; t = DateTime.MinValue; ax = "";  ap = decimal 0; as' = 0; bx = ""; bp = decimal 0.0; 
            bs = 0; c = []; z = None}

type Bars = { bars: Bar array; symbol: string; next_page_token: string option} with 
    static member Zero = {bars = [||]; symbol = ""; next_page_token = None}
and Bar = {[<JsonField("S")>] S': string option; t: DateTime; o: decimal; h: decimal; l: decimal; c: decimal; v: Int64} with
    static member Zero = 
        {S' = None; t = DateTime.MinValue; o = decimal 0; h = decimal 0; l = decimal 0; c = decimal 0; v = int64 0}

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
        let private DATA_POINT = "https://data.alpaca.markets/v2"
        
        let tz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
        let EST t = TimeZoneInfo.ConvertTimeFromUtc(t, tz)    
        
        [<RequireQualifiedAccess>]
        module Trades = 
            let private TRADES_POINT symbol = Url.Combine(DATA_POINT, "stocks", symbol, "trades")
            let get (start: DateTime) (end': DateTime) (symbol: string) =  
                let start = Helpers.dateToString (start.ToUniversalTime())
                let end' = Helpers.dateToString (end'.ToUniversalTime())
                fun () ->
                    Http.Request( TRADES_POINT symbol, 
                                    httpMethod = "GET",
                                    query = ["start", start; "end", end'],
                                    headers = HEADERS)
                |> handleResponse<Trades>

        [<RequireQualifiedAccess>]
        module Quotes = 
            let private QUOTES_POINT symbol = Url.Combine(DATA_POINT, "stocks", symbol, "quotes")

            /// <summary>provides NBBO quotes for a given ticker symbol</summary>
            let get (start: DateTime) (end': DateTime) (symbol: string) (limit: int) = 
                let start = Helpers.dateToString (start.ToUniversalTime())
                let end' = Helpers.dateToString (end'.ToUniversalTime())
                fun () ->
                    Http.Request( QUOTES_POINT symbol, 
                                        httpMethod = "GET",
                                        query = ["start", start; "end",  end'; "limit", string limit],
                                        headers = HEADERS)
                |> handleResponse<Quotes>
                 
        [<RequireQualifiedAccess>]
        module Bars =    
            let private BARS_POINT symbol = Url.Combine(DATA_POINT, "stocks", symbol, "bars")   

            /// <summary>returns aggregate historical data for the requested securities</summary>
            let get (start: DateTime) (end': DateTime) (symbol: string) (limit: int) (timeFrame: TimeFrame) = 
                let start = Helpers.dateToString (start.ToUniversalTime())
                let end' = Helpers.dateToString (end'.ToUniversalTime())
                fun () ->
                    Http.Request( BARS_POINT symbol, 
                                        httpMethod = "GET",
                                        query = ["start", string start; "end", string end'; "limit", "1000"; "timeframe", TimeFrame.string timeFrame ],
                                        headers = HEADERS)
                |> handleResponse<Bars>  

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

        let subscribe trades quotes bars =
            let subscription = 
                { action = "subscribe"; trades = trades; quotes = quotes; bars = bars }
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
                        (Text.Encoding.Default.GetString(buf) |> JsonNet.Deserialize<Trade array>, stream)
                } |> Async.RunSynchronously
            |> State

        let status() =
            fun (stream: Stream) ->
                printfn "%A" stream.State
                (stream.State, stream)
            |> State
