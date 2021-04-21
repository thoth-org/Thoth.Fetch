module Thoth.Fetch

open Fetch
open Fable.Core
open Fable.Core.JsInterop
open Thoth.Json

type FetchError =
    | PreparingRequestFailed of exn
    | DecodingFailed of string
    | FetchFailed of Response
    | NetworkError of exn

module Helper =

    [<Erase>]
    type GlobalFetch =
        [<Global>]
        static member fetch (req: RequestInfo, ?init: RequestInit): JS.Promise<Response> = jsNative

    let fetch (url: string) (init: RequestProperties list): JS.Promise<Response> =
        GlobalFetch.fetch (RequestInfo.Url url, requestProps init)

    let withContentTypeJson data headers =
        match data with
        | Some _ -> ContentType "application/json" :: headers
        | _ -> headers

    let encode data caseStrategy extra dataResolver =
        let encoder =
            Encode.Auto.generateEncoderCached (?caseStrategy = caseStrategy, ?extra = extra, ?resolver = dataResolver)

        data
        |> encoder
        |> Encode.toString 0

    let withBody data caseStrategy extra dataResolver properties =
        data
        |> Option.map (fun data ->
            encode data caseStrategy extra dataResolver
            |> (!^)
            |> Body
            |> fun body -> body :: properties)
        |> Option.defaultValue properties

    let withFormData (data: Browser.Types.FormData) properties =
        data
        |> (!^)
        |> Body
        |> fun body -> body :: properties

    let withProperties custom properties =
        custom
        |> Option.map ((@) properties)
        |> Option.defaultValue properties

    let eitherUnit (responseResolver: ITypeResolver<'Response>) cont =
        if responseResolver.ResolveType().FullName = typedefof<unit>.FullName then Ok(unbox())
        else cont()

    let resolve (response: Response) caseStrategy extra (decoder: Decoder<'Response> option)
        (responseResolver: ITypeResolver<'Response> option) =

        let decoder =
            decoder
            |> Option.defaultValue
                (Decode.Auto.generateDecoderCached
                    (?caseStrategy = caseStrategy, ?extra = extra, ?resolver = responseResolver))

        let decode body = Decode.fromString decoder body

        let eitherUnitOr = eitherUnit responseResolver.Value

        promise {
            let! result =
                if response.Ok then
                    promise {
                        let! body = response.text()
                        return eitherUnitOr <| fun () ->
                            match decode body with
                            | Ok value -> Ok value
                            | Error msg -> DecodingFailed msg |> Error
                    }
                else
                    FetchFailed response |> Error
                    |> Promise.lift
            return result
        }

    let message error =
        match error with
        | PreparingRequestFailed exn ->
            "[Thoth.Fetch] Request preparation failed:\n\n" + exn.Message
        | DecodingFailed msg ->
            "[Thoth.Fetch] Error while decoding the response:\n\n" + msg
        | FetchFailed response ->
            "[Thoth.Fetch] Request failed:\n\n" + string response.Status + " " + response.StatusText + " for URL " + response.Url
        | NetworkError exn ->
            "[Thoth.Fetch] A network error occured:\n\n" + exn.Message

open Helper

type Fetch =

    /// **Description**
    ///
    /// Send a request to the specified resource and decodes the response.
    ///
    /// If fetch and decoding succeed, we return `Ok 'Response`.
    ///
    /// If we fail, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `httpMethod` - optional parameter of type `HttpMethod` - HttpMethod used for Request, defaults to **GET**
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryFetchAs<'Data, 'Response> (url: string, ?decoder: Decoder<'Response>, ?data: 'Data,
                                                ?httpMethod: HttpMethod, ?properties: RequestProperties list,
                                                ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                                ?extra: ExtraCoders,
                                                [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                                [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        try
            let properties =
                [ Method <| defaultArg httpMethod HttpMethod.GET
                  requestHeaders (defaultArg headers [] |> withContentTypeJson data) ]
                |> withBody data caseStrategy extra dataResolver
                |> withProperties properties

            promise {
                let! response = fetch url properties
                return! resolve response caseStrategy extra decoder responseResolver
            }
            |> Promise.catch (NetworkError >> Error)

        with exn -> promise { return PreparingRequestFailed exn |> Error }

    /// **Description**
    ///
    /// Send a request to the specified resource and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    //// An exception will be thrown if fetch fails.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `httpMethod` - optional parameter of type `HttpMethod` - HttpMethod used, defaults to **GET**
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member fetchAs<'Data, 'Response> (url: string, ?decoder: Decoder<'Response>, ?data: 'Data,
                                             ?httpMethod: HttpMethod, ?properties: RequestProperties list,
                                             ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                             [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                             [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        promise {
            let! result = Fetch.tryFetchAs<'Data, 'Response>
                              (url, ?decoder = decoder, ?httpMethod = httpMethod, ?data = data, ?properties = properties,
                               ?headers = headers, ?caseStrategy = caseStrategy, ?extra = extra,
                               ?responseResolver = responseResolver, ?dataResolver = dataResolver)
            let response =
                match result with
                | Ok response -> response
                | Error error -> failwith (message error)
            return response
        }

    /// **Description**
    ///
    /// Send a **GET** request to the specified resource and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    //// An exception will be thrown if the request fails.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member get<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                         ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                         ?extra: ExtraCoders, ?decoder: Decoder<'Response>,
                                         [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                         [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs
            (url, ?data = data, ?properties = properties, ?headers = headers, ?caseStrategy = caseStrategy, ?extra = extra,
             ?decoder = decoder, ?responseResolver = responseResolver, ?dataResolver = dataResolver)

    /// **Description**
    ///
    /// Send a **GET** request to the specified resource and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    //// If we fail, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryGet<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                            ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                            ?decoder: Decoder<'Response>,
                                            [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                            [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs
            (url, ?data = data, ?properties = properties, ?headers = headers, ?caseStrategy = caseStrategy, ?extra = extra,
             ?decoder = decoder, ?responseResolver = responseResolver, ?dataResolver = dataResolver)

    /// **Description**
    ///
    /// Send a **POST** request to the specified resource and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    //// An exception will be thrown if the request fails.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member post<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                          ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                          ?extra: ExtraCoders, ?decoder: Decoder<'Response>,
                                          [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                          [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs
            (url, httpMethod = HttpMethod.POST, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver,
             ?dataResolver = dataResolver)

    /// **Description**
    ///
    /// Send a **POST** request to the specified resource and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    //// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryPost<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                             ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                             ?decoder: Decoder<'Response>,
                                             [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                             [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs
            (url, httpMethod = HttpMethod.POST, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver,
             ?dataResolver = dataResolver)

    /// **Description**
    ///
    /// Send a **PUT** request to the specified resource and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    //// An exception will be thrown if the request fails.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member put<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                         ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                         ?extra: ExtraCoders, ?decoder: Decoder<'Response>,
                                         [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                         [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs
            (url, httpMethod = HttpMethod.PUT, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver,
             ?dataResolver = dataResolver)

    /// **Description**
    ///
    /// Send a **PUT** request to the specified resource and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    //// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryPut<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                            ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                            ?decoder: Decoder<'Response>,
                                            [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                            [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs
            (url, httpMethod = HttpMethod.PUT, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver,
             ?dataResolver = dataResolver)

    /// **Description**
    ///
    /// Send a **PATCH** request to the specified resource and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    //// An exception will be thrown if the request fails.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member patch<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                           ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                           ?extra: ExtraCoders, ?decoder: Decoder<'Response>,
                                           [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                           [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs
            (url, httpMethod = HttpMethod.PATCH, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver,
             ?dataResolver = dataResolver)

    /// **Description**
    ///
    /// Send a **PATCH** request to the specified resource and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    //// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryPatch<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                              ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                              ?decoder: Decoder<'Response>,
                                              [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                              [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs
            (url, httpMethod = HttpMethod.PATCH, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver,
             ?dataResolver = dataResolver)

    /// **Description**
    ///
    /// Send a **DELETE** request to the specified resource and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    //// An exception will be thrown if the request fails.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member delete<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                            ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                            ?decoder: Decoder<'Response>,
                                            [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                            [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs
            (url, httpMethod = HttpMethod.DELETE, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver,
             ?dataResolver = dataResolver)

    /// **Description**
    ///
    /// Send a **DELETE** request to the specified resource and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    //// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryDelete<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                               ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                               ?extra: ExtraCoders, ?decoder: Decoder<'Response>,
                                               [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                               [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs
            (url, httpMethod = HttpMethod.DELETE, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver,
             ?dataResolver = dataResolver)

    /// **Description**
    ///
    /// Send a multi-part file form request to the specified file resource without encoding it and decodes the response.
    ///
    /// If fetch and decoding succeed, we return `Ok 'Response`.
    ///
    /// If we fail, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `httpMethod` - optional parameter of type `HttpMethod` - HttpMethod used for Request, defaults to **POST**
    ///   * `formData` - optional parameter of type `'Browser.Types.FormData` - Data sent via the body
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryFetchAs<'Response> (formData: Browser.Types.FormData, url: string, ?decoder: Decoder<'Response>,
                                         ?httpMethod: HttpMethod, ?properties: RequestProperties list,
                                         ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                         ?extra: ExtraCoders,
                                         [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        try
            let properties =
                [ Method <| defaultArg httpMethod HttpMethod.POST
                  requestHeaders (defaultArg headers []) ]
                |> withFormData formData
                |> withProperties properties

            promise {
                let! response = fetch url properties
                return! resolve response caseStrategy extra decoder responseResolver
            }
            |> Promise.catch (NetworkError >> Error)

        with exn -> promise { return PreparingRequestFailed exn |> Error }

    /// **Description**
    ///
    /// Send a multi-part file form request to the specified file resource without encoding it and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/octet-stream"`.
    ///
    //// An exception will be thrown if fetch fails.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `httpMethod` - optional parameter of type `HttpMethod` - HttpMethod used, defaults to **POST**
    ///   * `formData` - optional parameter of type `'Browser.Types.FormData` - Data sent via the body
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member fetchAs<'Response> (formData: Browser.Types.FormData, url: string, ?decoder: Decoder<'Response>,
                                      ?httpMethod: HttpMethod, ?properties: RequestProperties list,
                                      ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                      [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        promise {
            let! result = Fetch.tryFetchAs<'Response>
                              (formData, url, ?decoder = decoder, ?httpMethod = httpMethod, ?properties = properties,
                               ?headers = headers, ?caseStrategy = caseStrategy, ?extra = extra,
                               ?responseResolver = responseResolver)
            let response =
                match result with
                | Ok response -> response
                | Error error -> failwith (message error)
            return response
        }

    /// **Description**
    ///
    /// Send a **POST** request to the specified file resource without encoding it and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/octet-stream"` if data is provided.
    ///
    //// An exception will be thrown if the request fails.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `formData` - optional parameter of type `'Browser.Types.FormData` - Data sent via the body
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member post<'Response> (formData: Browser.Types.FormData, url: string, ?properties: RequestProperties list,
                                   ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                   ?extra: ExtraCoders, ?decoder: Decoder<'Response>,
                                   [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.fetchAs
            (formData, url, httpMethod = HttpMethod.POST, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver)

    /// **Description**
    ///
    /// Send a **POST** request to the specified file resource without encoding it and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/octet-stream"` if data is provided.
    ///
    //// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `formData` - optional parameter of type `'Browser.Types.FormData` - Data sent via the body
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryPost<'Response> (formData: Browser.Types.FormData, url: string, ?properties: RequestProperties list,
                                      ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                      ?decoder: Decoder<'Response>,
                                      [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.tryFetchAs
            (formData, url, httpMethod = HttpMethod.POST, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver)

    /// **Description**
    ///
    /// Send a **PUT** request to the specified file resource without encoding it and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/octet-stream"`.
    ///
    //// An exception will be thrown if the request fails.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `formData` - optional parameter of type `'Browser.Types.FormData` - Data sent via the body
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member put<'Response> (formData: Browser.Types.FormData, url: string, ?properties: RequestProperties list,
                                  ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                  ?extra: ExtraCoders, ?decoder: Decoder<'Response>,
                                  [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.fetchAs
            (formData, url, httpMethod = HttpMethod.PUT, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver)

    /// **Description**
    ///
    /// Send a **PUT** request to the specified file resource without encoding it and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/octet-stream"`.
    ///
    //// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `formData` - optional parameter of type `'Browser.Types.FormData` - Data sent via the body
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryPut<'Response> (formData: Browser.Types.FormData, url: string, ?properties: RequestProperties list,
                                     ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                     ?decoder: Decoder<'Response>,
                                     [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.tryFetchAs
            (formData, url, httpMethod = HttpMethod.PUT, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver)

    /// **Description**
    ///
    /// Send a **PATCH** request to the specified file resource without encoding it and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/octet-stream"`.
    ///
    //// An exception will be thrown if the request fails.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `formData` - optional parameter of type `'Browser.Types.FormData` - Data sent via the body
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member patch<'Response> (formData: Browser.Types.FormData, url: string, ?properties: RequestProperties list,
                                    ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                    ?extra: ExtraCoders, ?decoder: Decoder<'Response>,
                                    [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.fetchAs
            (formData, url, httpMethod = HttpMethod.PATCH, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver)

    /// **Description**
    ///
    /// Send a **PATCH** request to the specified file resource without encoding it and decodes the response.
    ///
    /// This method set the `ContentType` header to `"application/octet-stream"`.
    ///
    //// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `formData` - optional parameter of type `'Browser.Types.FormData` - Data sent via the body
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `caseStrategy` - optional parameter of type `CaseStrategy` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `decoder` - parameter of type `Decoder<'Response>` - Decoder applied to the server response
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryPatch<'Response> (formData: Browser.Types.FormData, url: string, ?properties: RequestProperties list,
                                       ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                       ?decoder: Decoder<'Response>,
                                       [<Inject>] ?responseResolver: ITypeResolver<'Response>) =
        Fetch.tryFetchAs
            (formData, url, httpMethod = HttpMethod.PATCH, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder, ?responseResolver = responseResolver)




