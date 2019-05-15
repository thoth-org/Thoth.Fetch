module Thoth.Fetch

open Fetch
open Fable.Core
open Thoth.Json

let internal toJsonBody (value : JsonValue) =
    #if DEBUG
    Encode.toString 4 value
    |> U2.Case2
    #else
    Encode.toString 0 value
    |> U2.Case2
    #endif

type Fetch =

    /// **Description**
    ///
    /// Retrieves data from the specified resource by applying the provided `decoder`.
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to be request
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `init` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
    ///
    static member fetchAs<'Response>(url : string,
                                     decoder : Decoder<'Response>,
                                     ?init : RequestProperties list) =
        promise {
            let init = defaultArg init []
            // TODO: Rewrite our own version of `Fetch.fetch` to give better error
            // ATM, when an error occured we are loosing information like status code, etc.
            let! response = Fetch.fetch url init
            let! body = response.text()
            return Decode.unsafeFromString decoder body
        }

    /// **Description**
    ///
    /// Retrieves data from the specified resource.
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to be request
    ///   * `init` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///   * `isCamelCase` - parameter of type `bool option` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - parameter of type `ExtraCoders option` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - parameter of type `ITypeResolver<'Response> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
    ///
    static member fetchAs<'Response>(url : string,
                                     ?init : RequestProperties list,
                                     ?isCamelCase : bool,
                                     ?extra: ExtraCoders,
                                     [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        let decoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)
        Fetch.fetchAs(url, decoder, ?init = init)

    /// **Description**
    ///
    /// Retrieves data from the specified resource by applying the provided `decoder`.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to be request
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `init` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryFetchAs<'Response>(url : string,
                                        decoder : Decoder<'Response>,
                                        ?init : RequestProperties list) =
        promise {
            let init = defaultArg init []
            // TODO: Rewrite our own version of `Fetch.fetch` to give better error
            // ATM, when an error occured we are loosing information like status code, etc.
            let! response = Fetch.fetch url init
            let! body = response.text()
            return Decode.fromString decoder body
        }

    /// **Description**
    ///
    /// Retrieves data from the specified resource.
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to be request
    ///   * `init` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///   * `isCamelCase` - parameter of type `bool option` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - parameter of type `ExtraCoders option` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - parameter of type `ITypeResolver<'Response> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryFetchAs<'Response>(url : string,
                                        ?init : RequestProperties list,
                                        ?isCamelCase : bool,
                                        ?extra: ExtraCoders,
                                        [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        let decoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)
        Fetch.tryFetchAs(url, decoder, ?init = init)

    /// Alias to `Fetch.fetchAs`
    static member get<'Response>(url : string,
                                 decoder : Decoder<'Response>,
                                 ?init : RequestProperties list) =
        Fetch.fetchAs(url, decoder, ?init = init)

    /// Alias to `Fetch.tryFetchAs`
    static member tryGet<'Response>(url : string,
                                    decoder : Decoder<'Response>,
                                    ?init : RequestProperties list) =
        Fetch.tryFetchAs(url, decoder, ?init = init)

    /// Alias to `Fetch.fetchAs`
    static member get<'Response>(url : string,
                                 ?init : RequestProperties list,
                                 ?isCamelCase : bool,
                                 ?extra: ExtraCoders,
                                 [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.fetchAs(url, ?init = init, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver)

    /// Alias to `Fetch.tryFetchAs`
    static member tryGet<'Response>(url : string,
                                    ?init : RequestProperties list,
                                    ?isCamelCase : bool,
                                    ?extra: ExtraCoders,
                                    [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.tryFetchAs(url, ?init = init, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver)

    static member post<'Response>(url : string,
                                  data : JsonValue,
                                  decoder : Decoder<'Response>,
                                  ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.POST
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchAs(url, decoder, init = properties)

    static member post<'Data, 'Response>(url : string,
                                         data : 'Data,
                                         ?properties : RequestProperties list,
                                         ?isCamelCase : bool,
                                         ?extra: ExtraCoders,
                                         [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                         [<Inject>] ?dataResolver: ITypeResolver<'Data>) =

        let dataEncoder = Encode.Auto.generateEncoderCached<'Data>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = dataResolver)
        let responseDecoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)

        let body =
            data
            |> dataEncoder
            |> toJsonBody

        let properties =
            [ RequestProperties.Method HttpMethod.POST
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body body ]
            @ defaultArg properties []

        Fetch.fetchAs(url, responseDecoder, init = properties)

    static member tryPost<'Response>(url : string,
                                     data : JsonValue,
                                     decoder : Decoder<'Response>,
                                     ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.POST
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, init = properties)

    static member tryPost<'Data, 'Response>(url : string,
                                            data : 'Data,
                                            ?properties : RequestProperties list,
                                            ?isCamelCase : bool,
                                            ?extra: ExtraCoders,
                                            [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                            [<Inject>] ?dataResolver: ITypeResolver<'Data>) =

        let dataEncoder = Encode.Auto.generateEncoderCached<'Data>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = dataResolver)
        let responseDecoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)

        let body =
            data
            |> dataEncoder
            |> toJsonBody

        let properties =
            [ RequestProperties.Method HttpMethod.POST
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body body ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, responseDecoder, init = properties)

    static member put<'Response>(url : string,
                                 data : JsonValue,
                                 decoder : Decoder<'Response>,
                                 ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PUT
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchAs(url, decoder, init = properties)

    static member put<'Data, 'Response>(url : string,
                                        data : 'Data,
                                        ?properties : RequestProperties list,
                                        ?isCamelCase : bool,
                                        ?extra: ExtraCoders,
                                        [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                        [<Inject>] ?dataResolver: ITypeResolver<'Data>) =

        let dataEncoder = Encode.Auto.generateEncoderCached<'Data>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = dataResolver)
        let responseDecoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)

        let body =
            data
            |> dataEncoder
            |> toJsonBody

        let properties =
            [ RequestProperties.Method HttpMethod.PUT
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body body ]
            @ defaultArg properties []

        Fetch.fetchAs(url, responseDecoder, init = properties)

    static member tryPut<'Response>(url : string,
                                    data : JsonValue,
                                    decoder : Decoder<'Response>,
                                    ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PUT
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, init = properties)

    static member tryPut<'Data, 'Response>(url : string,
                                           data : 'Data,
                                           ?properties : RequestProperties list,
                                           ?isCamelCase : bool,
                                           ?extra: ExtraCoders,
                                           [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                           [<Inject>] ?dataResolver: ITypeResolver<'Data>) =

        let dataEncoder = Encode.Auto.generateEncoderCached<'Data>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = dataResolver)
        let responseDecoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)

        let body =
            data
            |> dataEncoder
            |> toJsonBody

        let properties =
            [ RequestProperties.Method HttpMethod.PUT
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body body ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, responseDecoder, init = properties)

    static member patch<'Response>(url : string,
                                   data : JsonValue,
                                   decoder : Decoder<'Response>,
                                   ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PATCH
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchAs(url, decoder, init = properties)

    static member patch<'Data, 'Response>(url : string,
                                          data : 'Data,
                                          ?properties : RequestProperties list,
                                          ?isCamelCase : bool,
                                          ?extra: ExtraCoders,
                                          [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                          [<Inject>] ?dataResolver: ITypeResolver<'Data>) =

        let dataEncoder = Encode.Auto.generateEncoderCached<'Data>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = dataResolver)
        let responseDecoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)

        let body =
            data
            |> dataEncoder
            |> toJsonBody

        let properties =
            [ RequestProperties.Method HttpMethod.PATCH
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body body ]
            @ defaultArg properties []

        Fetch.fetchAs(url, responseDecoder, init = properties)

    static member tryPatch<'Response>(url : string,
                                      data : JsonValue,
                                      decoder : Decoder<'Response>,
                                      ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PATCH
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, init = properties)

    static member tryPatch<'Data, 'Response>(url : string,
                                             data : 'Data,
                                             ?properties : RequestProperties list,
                                             ?isCamelCase : bool,
                                             ?extra: ExtraCoders,
                                             [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                             [<Inject>] ?dataResolver: ITypeResolver<'Data>) =

        let dataEncoder = Encode.Auto.generateEncoderCached<'Data>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = dataResolver)
        let responseDecoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)

        let body =
            data
            |> dataEncoder
            |> toJsonBody

        let properties =
            [ RequestProperties.Method HttpMethod.PATCH
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body body ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, responseDecoder, init = properties)

    static member delete<'Response>(url : string,
                                    data : JsonValue,
                                    decoder : Decoder<'Response>,
                                    ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.DELETE
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchAs(url, decoder, init = properties)

    static member delete<'Data, 'Response>(url : string,
                                           data : 'Data,
                                           ?properties : RequestProperties list,
                                           ?isCamelCase : bool,
                                           ?extra: ExtraCoders,
                                           [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                           [<Inject>] ?dataResolver: ITypeResolver<'Data>) =

        let dataEncoder = Encode.Auto.generateEncoderCached<'Data>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = dataResolver)
        let responseDecoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)

        let body =
            data
            |> dataEncoder
            |> toJsonBody

        let properties =
            [ RequestProperties.Method HttpMethod.DELETE
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body body ]
            @ defaultArg properties []

        Fetch.fetchAs(url, responseDecoder, init = properties)

    static member tryDelete<'Response>(url : string,
                                       data : JsonValue,
                                       decoder : Decoder<'Response>,
                                       ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.DELETE
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, init = properties)

    static member tryDelete<'Data, 'Response>(url : string,
                                              data : 'Data,
                                              ?properties : RequestProperties list,
                                              ?isCamelCase : bool,
                                              ?extra: ExtraCoders,
                                              [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                              [<Inject>] ?dataResolver: ITypeResolver<'Data>) =

        let dataEncoder = Encode.Auto.generateEncoderCached<'Data>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = dataResolver)
        let responseDecoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)

        let body =
            data
            |> dataEncoder
            |> toJsonBody

        let properties =
            [ RequestProperties.Method HttpMethod.DELETE
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body body ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, responseDecoder, init = properties)
