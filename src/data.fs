namespace rec AlpacaFs

open AlpacaFs
open Config

open System
open FSharp.Data
open FSharp.Json
open Flurl

open System.Threading
open System.Net.WebSockets

open System.Text.Json
open System.Text.Json.Serialization



type Trades = { trades: Trade array; symbol: string; next_page_token: string option} with 
    static member Zero = {trades = [||]; symbol = ""; next_page_token = None}
and Trade = {t: DateTime; x: string; p: decimal; s: int; c: string array; i: int64; z: string} with 
    static member Zero = {t= DateTime.MinValue; x = ""; p = decimal 0; s = 0; c = [||]; i = int64 0; z = ""}
        
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
module Stream = 
    let inline run state (Data.RealTime.State f) = 
            f state

    let bind (x, f) =
            fun state -> 
                let (result, newState) = run state x 
                run newState (result |> f)
            |> Data.RealTime.State

[<RequireQualifiedAccess>]
module Data = 

    /// [omit]
    /// 
    type Schema = T | Q | B
    with 
        static member fromString str =
            match toUpperUnion<Schema> str with
            | Some case -> case
            | None -> T
    
    /// [omit]
    /// 
    type SchemaRecord = {[<JsonField(Transform=typeof<JsonTransforms.SchemaTransform>)>] t: Schema}
    
    [<RequireQualifiedAccess>]
    module Historical = 
        let private DATA_POINT = "https://data.alpaca.markets/v2"
        
        let tz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
        let EST t = TimeZoneInfo.ConvertTimeFromUtc(t, tz)    
        
        [<RequireQualifiedAccess>]
        module Trades = 
            let private TRADES_POINT symbol = Url.Combine(DATA_POINT, "stocks", symbol, "trades")
            /// Get the trades of a stock from start to end'
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

            /// provides NBBO quotes for a given ticker symbol
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

            /// returns aggregate historical data for the requested securities
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
    module RealTime = 
        type Stream = ClientWebSocket

        let private STREAM_POINT = "wss://stream.data.alpaca.markets/v2/iex"
        //let private STREAM_POINT =  "wss://paper-api.alpaca.markets/stream"

        type State<'s, 'a> = State of ('s -> ('a * 's))

        type Access= {action: string ; trades: string list ; quotes: string list ; bars: string list}

        type Successes = Success list
        and Success = {T: string; msg: string}

        type Errors = Error list
        and Error = {T: string; code: int; msg: string}

        type StreamBuilder() = 
            member _.Zero () = State(fun s -> ((), s))
            member _.Return x = State(fun s -> (x, s))  
            member inline _.ReturnFrom x = x
            member _.Yield x = State(fun s -> (x, s)) 
            member inline _.YieldFrom x = x

            member _.Bind (x, f) = Stream.bind(x, f)

            member _.Delay f = f()
            member _.Combine (x1, x2)=
                fun stream ->
                    let (_, newStream) = Stream.run stream x1 
                    Stream.run newStream x2
                |> State   
           

        let create () : Stream = 
            new ClientWebSocket()
        
        /// Connects to the stream
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

        /// Authinticates the connection to the stream                
        let authenticate() = 
            let authentication = 
                {| action = "auth"; key = AlpacaFs.Config.API_KEY; secret = AlpacaFs.Config.SECRET_KEY |}
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

        /// Start listening for trades, quotes, and bars of specific symbols
        let subscribe trades quotes bars =
            let subscription = 
                { action = "subscribe"; trades = trades; quotes = quotes; bars = bars }
                |> Json.serialize
            printfn "%s" subscription
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

        /// Applies a function on the message received
        let onMessage(onMessage: Trade array -> unit) =
            fun (stream: Stream) ->
                let rec loop status = 
                    match status with 
                    |WebSocketState.Open ->  
                        printfn "Websocket is open"
                        async {
                            let buf = Array.init 256 (fun a -> 0uy)
                            let! request =
                                stream.ReceiveAsync(buf |> ArraySegment, CancellationToken.None)
                                |> Async.AwaitTask
                            let msg = Text.Encoding.UTF8.GetString(buf, 0, request.Count)
                            printfn "%s" msg
                            let trades = msg |>Json.deserialize<Trade array> //System.Text.Json.JsonSerializer.Deserialize<Trade array>
                            onMessage(trades)
                            return! loop stream.State
                        }
                    | _ -> 
                        printfn $"Websocket is {stream.State}"
                        async {return ("", stream)}
                loop stream.State |> Async.RunSynchronously 
            |> State
 
        /// Gets the status of the stream connection
        let status() =
            fun (stream: Stream) ->
                printfn "%A" stream.State
                (stream.State, stream)
            |> State
