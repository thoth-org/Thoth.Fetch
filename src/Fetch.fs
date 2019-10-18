module Thoth.Fetch

open Fetch
open Fable.Core
open Fable.Core.JsInterop
open Thoth.Json
open System

let internal toJsonBody (value : JsonValue) : BodyInit=
    #if DEBUG
    Encode.toString 4 value
    |> (!^)
    #else
    Encode.toString 0 value
    |> (!^)
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

    static member fetchUnit(url : string,
                            ?methodName : string,
                            ?properties : RequestProperties list) : JS.Promise<unit> =
        promise {
            let properties = defaultArg properties []
            // TODO: Rewrite our own version of `Fetch.fetch` to give better error
            // ATM, when an error occured we are loosing information like status code, etc.
            let! response = Fetch.fetch url properties
            let! body = response.text()
            if String.IsNullOrEmpty body then
                return ()
            else
                match methodName with
                | Some methodName ->
                    failwithf "No body expected for `Fetch.%s` request. If you expect a body to decode, please use `Fetch.%sAs`" methodName methodName
                | None ->
                    failwithf "No body expected for this request"
        }

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

    static member tryFetchUnit(url : string,
                               ?methodName : string,
                               ?properties : RequestProperties list) : JS.Promise<Result<unit, string>> =
        promise {
            let properties = defaultArg properties []
            // TODO: Rewrite our own version of `Fetch.fetch` to give better error
            // ATM, when an error occured we are loosing information like status code, etc.
            let! response = Fetch.fetch url properties
            let! body = response.text()
            if String.IsNullOrEmpty body then
                return Ok ()
            else
                match methodName with
                | Some methodName ->
                    return Error (sprintf "No body expected for `Fetch.%s` request. If you expect a body to decode, please use `Fetch.%sAs`" methodName methodName)
                | None ->
                    return Error "No body expected for this request"
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
    static member getAs<'Response>(url : string,
                                   decoder : Decoder<'Response>,
                                   ?properties : RequestProperties list) =
        Fetch.fetchAs(url, decoder, ?properties = properties)

    /// Alias to `Fetch.tryFetchAs`
    static member tryGetAs<'Response>(url : string,
                                      decoder : Decoder<'Response>,
                                      ?properties : RequestProperties list) =
        Fetch.tryFetchAs(url, decoder, ?properties = properties)

    /// Alias to `Fetch.fetchAs`
    static member getAs<'Response>(url : string,
                                   ?properties : RequestProperties list,
                                   ?isCamelCase : bool,
                                   ?extra: ExtraCoders,
                                   [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.fetchAs(url, ?properties = properties, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver)

    /// Alias to `Fetch.tryFetchAs`
    static member tryGetAs<'Response>(url : string,
                                      ?properties : RequestProperties list,
                                      ?isCamelCase : bool,
                                      ?extra: ExtraCoders,
                                      [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.tryFetchAs(url, ?properties = properties, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver)

    /// Alias to `Fetch.fetchAs`
    static member get(url : string,
                      ?properties : RequestProperties list) : JS.Promise<unit> =
        Fetch.fetchUnit(url, "get", ?properties = properties)

    /// Alias to `Fetch.tryFetchAs`
    static member tryGet(url : string,
                         ?properties : RequestProperties list) : JS.Promise<Result<unit, string>> =
        Fetch.tryFetchUnit(url, "tryGet", ?properties = properties)

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
    static member postAs<'Response>(url : string,
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
    static member postAs<'Data, 'Response>(url : string,
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

    static member post(url : string,
                       data : JsonValue,
                       ?properties : RequestProperties list) : JS.Promise<unit> =
        let properties =
            [ RequestProperties.Method HttpMethod.POST
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchUnit(url, "post", properties = properties)

    static member post<'Data>(url : string,
                              data : 'Data,
                              ?properties : RequestProperties list,
                              ?isCamelCase : bool,
                              ?extra: ExtraCoders,
                              [<Inject>] ?dataResolver: ITypeResolver<'Data>) : JS.Promise<unit> =

        let dataEncoder = Encode.Auto.generateEncoderCached<'Data>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = dataResolver)

        let body =
            data
            |> dataEncoder
            |> toJsonBody

        let properties =
            [ RequestProperties.Method HttpMethod.POST
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body body ]
            @ defaultArg properties []

        Fetch.fetchUnit(url, "post", properties = properties)

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
    static member tryPostAs<'Response>(url : string,
                                       data : JsonValue,
                                       decoder : Decoder<'Response>,
                                       ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.POST
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, properties = properties)

    static member tryPost(url : string,
                          data : JsonValue,
                          ?properties : RequestProperties list) : JS.Promise<Result<unit, string>> =
        let properties =
            [ RequestProperties.Method HttpMethod.POST
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchUnit(url, "tryPost", properties = properties)

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
    static member tryPostAs<'Data, 'Response>(url : string,
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
    static member putAs<'Response>(url : string,
                                   data : JsonValue,
                                   decoder : Decoder<'Response>,
                                   ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PUT
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchAs(url, decoder, properties = properties)

    static member put(url : string,
                      data : JsonValue,
                      ?properties : RequestProperties list) : JS.Promise<unit> =
        let properties =
            [ RequestProperties.Method HttpMethod.PUT
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchUnit(url, "put", properties = properties)

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
    static member putAs<'Data, 'Response>(url : string,
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
    static member tryPutAs<'Response>(url : string,
                                      data : JsonValue,
                                      decoder : Decoder<'Response>,
                                      ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PUT
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, properties = properties)

    static member tryPut(url : string,
                          data : JsonValue,
                          ?properties : RequestProperties list) : JS.Promise<Result<unit, string>> =
        let properties =
            [ RequestProperties.Method HttpMethod.PUT
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchUnit(url, "tryPut", properties = properties)

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
    static member tryPutAs<'Data, 'Response>(url : string,
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
    static member patchAs<'Response>(url : string,
                                     data : JsonValue,
                                     decoder : Decoder<'Response>,
                                     ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PATCH
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchAs(url, decoder, properties = properties)

    static member patch(url : string,
                       data : JsonValue,
                       ?properties : RequestProperties list) : JS.Promise<unit> =
        let properties =
            [ RequestProperties.Method HttpMethod.PATCH
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchUnit(url, "patch", properties = properties)

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
    static member patchAs<'Data, 'Response>(url : string,
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
    static member tryPatchAs<'Response>(url : string,
                                        data : JsonValue,
                                        decoder : Decoder<'Response>,
                                        ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.PATCH
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, properties = properties)

    static member tryPatch(url : string,
                          data : JsonValue,
                          ?properties : RequestProperties list) : JS.Promise<Result<unit, string>> =
        let properties =
            [ RequestProperties.Method HttpMethod.PATCH
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchUnit(url, "tryPatch", properties = properties)

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
    static member tryPatchAs<'Data, 'Response>(url : string,
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
    static member deleteAs<'Response>(url : string,
                                      data : JsonValue,
                                      decoder : Decoder<'Response>,
                                      ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.DELETE
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchAs(url, decoder, properties = properties)

    static member delete(url : string,
                       data : JsonValue,
                       ?properties : RequestProperties list) : JS.Promise<unit> =
        let properties =
            [ RequestProperties.Method HttpMethod.DELETE
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.fetchUnit(url, "delete", properties = properties)

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
    static member deleteAs<'Data, 'Response>(url : string,
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
    static member tryDeleteAs<'Response>(url : string,
                                         data : JsonValue,
                                         decoder : Decoder<'Response>,
                                         ?properties : RequestProperties list) =
        let properties =
            [ RequestProperties.Method HttpMethod.DELETE
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchAs(url, decoder, properties = properties)

    static member tryDelete(url : string,
                            data : JsonValue,
                            ?properties : RequestProperties list) : JS.Promise<Result<unit, string>> =
        let properties =
            [ RequestProperties.Method HttpMethod.DELETE
              requestHeaders [ ContentType "application/json" ]
              RequestProperties.Body (toJsonBody data) ]
            @ defaultArg properties []

        Fetch.tryFetchUnit(url, "tryDelete", properties = properties)

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
    static member tryDeleteAs<'Data, 'Response>(url : string,
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
