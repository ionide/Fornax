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

    let parseConcrete<'a> (document : string) =
        let deserializer =
            DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build()

        deserializer.Deserialize<'a> document

module FornaxConfiguration =

    [<CLIMutable>]
    type StyleConfigRepresentation =
        { entry : string }

    [<CLIMutable>]
    type FornaxConfigurationRepresentation =
        { exclude : string array
          style : StyleConfigRepresentation }

    type StyleConfiguration =
        { Entry : string option }

    type FornaxConfiguration =
        { Exclude : string list
          StyleConfiguration : StyleConfiguration }

    let private defaultStyleConfig =
        { Entry = None }

    let private defaultConfiguration =
        { Exclude = []
          StyleConfiguration = defaultStyleConfig }

    let private applyExcludeRules (representation : FornaxConfigurationRepresentation) =
        if isNull representation.exclude then
            []
        else
            Array.toList representation.exclude

    let private applyStyleRules (representation : FornaxConfigurationRepresentation) =
        if box representation.style |> isNull then
            defaultStyleConfig
        else
            let entryValue =
                if isNull representation.style.entry then
                    None
                else
                    Some representation.style.entry

            { Entry = entryValue }

    let parseFornaxConfiguration (fileContent : string) : FornaxConfiguration =
        let representation = Yaml.parseConcrete<FornaxConfigurationRepresentation> fileContent

        if box representation |> isNull then
            defaultConfiguration
        else
            { Exclude = applyExcludeRules representation
              StyleConfiguration = applyStyleRules representation }
