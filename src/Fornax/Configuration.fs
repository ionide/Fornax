module Configuration

module Yaml =
    open YamlDotNet.Serialization
    open System

    let parse (modelType : Type) (document : string) =
        let deserializer =
            DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build()
        let method = (typedefof<Deserializer>).GetMethod("Deserialize", [| typedefof<string> |])
        let genericMethod = method.MakeGenericMethod(modelType)
        genericMethod.Invoke(deserializer, [| document |])