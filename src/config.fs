
namespace AlpacaFs

open System
open Flurl



module Config =
    let BASE_POINT = "https://paper-api.alpaca.markets/v2"
    let ACCOUNT_POINT = Url.Combine(BASE_POINT, "/account")
    let API_KEY = System.Environment.GetEnvironmentVariable("APCA-API-KEY-ID", EnvironmentVariableTarget.User)
    let SECRET_KEY = System.Environment.GetEnvironmentVariable("APCA-API-SECRET-KEY", EnvironmentVariableTarget.User) 
    let HEADERS =  ["APCA-API-KEY-ID", API_KEY; "APCA-API-SECRET-KEY", SECRET_KEY]



