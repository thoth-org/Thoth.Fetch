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
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
    ///
    static member fetchAs<'Response>(url : string,
                                     decoder : Decoder<'Response>,
                                     ?properties : RequestProperties list) =
        promise {
            let properties = defaultArg properties []
            // TODO: Rewrite our own version of `Fetch.fetch` to give better error
            // ATM, when an error occured we are loosing information like status code, etc.
            let! response = Fetch.fetch url properties
            let! body = response.text()
            return Decode.unsafeFromString decoder body
        }

    /// **Description**
    ///
    /// Retrieves data from the specified resource.
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
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
                                     ?properties : RequestProperties list,
                                     ?isCamelCase : bool,
                                     ?extra: ExtraCoders,
                                     [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        let decoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)
        Fetch.fetchAs(url, decoder, ?properties = properties)

    /// **Description**
    ///
    /// Retrieves data from the specified resource by applying the provided `decoder`.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    ///
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryFetchAs<'Response>(url : string,
                                        decoder : Decoder<'Response>,
                                        ?properties : RequestProperties list) =
        promise {
            let properties = defaultArg properties []
            // TODO: Rewrite our own version of `Fetch.fetch` to give better error
            // ATM, when an error occured we are loosing information like status code, etc.
            let! response = Fetch.fetch url properties
            let! body = response.text()
            return Decode.fromString decoder body
        }

    /// **Description**
    ///
    /// Retrieves data from the specified resource.
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    ///
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
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
                                        ?properties : RequestProperties list,
                                        ?isCamelCase : bool,
                                        ?extra: ExtraCoders,
                                        [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        let decoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)
        Fetch.tryFetchAs(url, decoder, ?properties = properties)

    /// Alias to `Fetch.fetchAs`
    static member get<'Response>(url : string,
                                 decoder : Decoder<'Response>,
                                 ?properties : RequestProperties list) =
        Fetch.fetchAs(url, decoder, ?properties = properties)

    /// Alias to `Fetch.tryFetchAs`
    static member tryGet<'Response>(url : string,
                                    decoder : Decoder<'Response>,
                                    ?properties : RequestProperties list) =
        Fetch.tryFetchAs(url, decoder, ?properties = properties)

    /// Alias to `Fetch.fetchAs`
    static member get<'Response>(url : string,
                                 ?properties : RequestProperties list,
                                 ?isCamelCase : bool,
                                 ?extra: ExtraCoders,
                                 [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.fetchAs(url, ?properties = properties, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver)

    /// Alias to `Fetch.tryFetchAs`
    static member tryGet<'Response>(url : string,
                                    ?properties : RequestProperties list,
                                    ?isCamelCase : bool,
                                    ?extra: ExtraCoders,
                                    [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.tryFetchAs(url, ?properties = properties, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver)



    /// **Description**
    ///
    /// Send a **POST** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `JsonValue` - JSON
    ///   * `decoder` - parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
    ///
    static member post<'Response>(url : string,
                                  data : JsonValue,
                                  decoder : Decoder<'Response>,
                                  ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.POST
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchAs(url, decoder, properties = properties)


    /// **Description**
    ///
    /// Send a **POST** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type.
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///   * `isCamelCase` - parameter of type `bool option` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - parameter of type `ExtraCoders option` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - parameter of type `ITypeResolver<'Response> option` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
    ///
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

        Fetch.fetchAs(url, responseDecoder, properties = properties)


    /// **Description**
    ///
    /// Send a **POST** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    ///
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `JsonValue`
    ///   * `decoder` - parameter of type `Decoder<'Response>`
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryPost<'Response>(url : string,
                                     data : JsonValue,
                                     decoder : Decoder<'Response>,
                                     ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.POST
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, properties = properties)


    /// **Description**
    ///
    /// Send a **POST** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type.
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    ///
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///   * `isCamelCase` - parameter of type `bool option` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - parameter of type `ExtraCoders option` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - parameter of type `ITypeResolver<'Response> option` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
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

        Fetch.tryFetchAs(url, responseDecoder, properties = properties)


    /// **Description**
    ///
    /// Send a **PUT** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `JsonValue`
    ///   * `decoder` - parameter of type `Decoder<'Response>`
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
    ///
    static member put<'Response>(url : string,
                                 data : JsonValue,
                                 decoder : Decoder<'Response>,
                                 ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PUT
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchAs(url, decoder, properties = properties)


    /// **Description**
    ///
    /// Send a **PUT** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type.
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///   * `isCamelCase` - parameter of type `bool option` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - parameter of type `ExtraCoders option` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - parameter of type `ITypeResolver<'Response> option` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
    ///
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

        Fetch.fetchAs(url, responseDecoder, properties = properties)


    /// **Description**
    ///
    /// Send a **PUT** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    ///
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `JsonValue`
    ///   * `decoder` - parameter of type `Decoder<'Response>`
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryPut<'Response>(url : string,
                                    data : JsonValue,
                                    decoder : Decoder<'Response>,
                                    ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PUT
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, properties = properties)


    /// **Description**
    ///
    /// Send a **PUT** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type.
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    ///
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///   * `isCamelCase` - parameter of type `bool option` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - parameter of type `ExtraCoders option` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - parameter of type `ITypeResolver<'Response> option` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
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

        Fetch.tryFetchAs(url, responseDecoder, properties = properties)


    /// **Description**
    ///
    /// Send a **PACTH** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `JsonValue`
    ///   * `decoder` - parameter of type `Decoder<'Response>`
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
    ///
    static member patch<'Response>(url : string,
                                   data : JsonValue,
                                   decoder : Decoder<'Response>,
                                   ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PATCH
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchAs(url, decoder, properties = properties)


    /// **Description**
    ///
    /// Send a **PATH** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type.
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///   * `isCamelCase` - parameter of type `bool option` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - parameter of type `ExtraCoders option` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - parameter of type `ITypeResolver<'Response> option` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
    ///
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

        Fetch.fetchAs(url, responseDecoder, properties = properties)


    /// **Description**
    ///
    /// Send a **PATCH** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    ///
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `JsonValue`
    ///   * `decoder` - parameter of type `Decoder<'Response>`
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryPatch<'Response>(url : string,
                                      data : JsonValue,
                                      decoder : Decoder<'Response>,
                                      ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PATCH
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, properties = properties)


    /// **Description**
    ///
    /// Send a **PATCH** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type.
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    ///
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///   * `isCamelCase` - parameter of type `bool option` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - parameter of type `ExtraCoders option` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - parameter of type `ITypeResolver<'Response> option` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
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

        Fetch.tryFetchAs(url, responseDecoder, properties = properties)


    /// **Description**
    ///
    /// Send a **DELETE** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `JsonValue`
    ///   * `decoder` - parameter of type `Decoder<'Response>`
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
    ///
    static member delete<'Response>(url : string,
                                    data : JsonValue,
                                    decoder : Decoder<'Response>,
                                    ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.DELETE
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchAs(url, decoder, properties = properties)


    /// **Description**
    ///
    /// Send a **DELETE** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type.
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///   * `isCamelCase` - parameter of type `bool option` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - parameter of type `ExtraCoders option` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - parameter of type `ITypeResolver<'Response> option` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
    ///
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

        Fetch.fetchAs(url, responseDecoder, properties = properties)


    /// **Description**
    ///
    /// Send a **DELETE** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    ///
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `JsonValue`
    ///   * `decoder` - parameter of type `Decoder<'Response>`
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryDelete<'Response>(url : string,
                                       data : JsonValue,
                                       decoder : Decoder<'Response>,
                                       ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.DELETE
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, properties = properties)


    /// **Description**
    ///
    /// Send a **DELETE** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type.
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// If the decoder succeed, we return `Ok 'Response`.
    ///
    /// If the decoder failed, we return `Error "explanation..."`
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - parameter of type `RequestProperties list option` - Parameters passed to fetch
    ///   * `isCamelCase` - parameter of type `bool option` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - parameter of type `ExtraCoders option` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - parameter of type `ITypeResolver<'Response> option` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
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

        Fetch.tryFetchAs(url, responseDecoder, properties = properties)
