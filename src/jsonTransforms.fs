
namespace AlpacaFs


open FSharp.Json
open System

module internal JsonTransforms = 
    type IntTransform() =
        interface ITypeTransform with
            member x.targetType () = typeof<string>
            member x.toTargetType num = string num :> obj
            member x.fromTargetType (str) = int (str :?> string) :> obj
    type FloatTransform() =
        interface ITypeTransform with
            member x.targetType () = typeof<string>
            member x.toTargetType num = string num :> obj
            member x.fromTargetType (str) = float (str :?> string) :> obj
    type DecimalTransform() =
        interface ITypeTransform with
            member x.targetType () = typeof<string>
            member x.toTargetType num = string num :> obj
            member x.fromTargetType (str) = decimal (str :?> string) :> obj     
    type DateTimeTransform() =
        interface ITypeTransform with
            member x.targetType () = typeof<string>
            member x.toTargetType dateTime = string dateTime :> obj
            member x.fromTargetType (str) =  DateTime.Parse (str :?> string) :> obj

    type SchemaTransform() =
        interface ITypeTransform with
            member x.targetType () = typeof<string>
            member x.toTargetType schema = (string schema).ToLower() :> obj
            member x.fromTargetType(str) = 
                let (Some case) = toUpperUnion(str :?> string)
                case :> obj
                
                