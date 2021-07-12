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

    let encode data dataType caseStrategy extra =
        let encoder = Encode.Auto.generateBoxedEncoderCached (dataType, ?caseStrategy = caseStrategy, ?extra = extra)

        data
        |> encoder
        |> Encode.toString 0

    let withBody data dataType caseStrategy extra properties =
        data
        |> Option.map (fun data ->
            encode data dataType caseStrategy extra
            |> (!^)
            |> Body
            |> fun body -> body :: properties)
        |> Option.defaultValue properties

    let withProperties custom properties =
        custom
        |> Option.map ((@) properties)
        |> Option.defaultValue properties

    let resolve (response: Response) responseType caseStrategy extra (decoder: Decoder<'Response> option) =

        let decoder =
            decoder
            |> Option.defaultWith (fun () ->
                Decode.Auto.generateBoxedDecoderCached (responseType, ?caseStrategy = caseStrategy, ?extra = extra)
                |> Decode.unboxDecoder)

        let decode body = Decode.fromString decoder body

        promise {
            let! result =
                if response.Ok then
                    promise {
                        let! body = response.text()
                        return
                            if responseType = typeof<unit>
                            then Ok(unbox ())
                            else
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
    static member __tryFetchAs<'Data, 'Response> (url: string, dataType: System.Type, responseType: System.Type,
                                                ?decoder: Decoder<'Response>, ?data: 'Data,
                                                ?httpMethod: HttpMethod, ?properties: RequestProperties list,
                                                ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                                ?extra: ExtraCoders) =
        try
            let properties =
                [ Method <| defaultArg httpMethod HttpMethod.GET
                  requestHeaders (defaultArg headers [] |> withContentTypeJson data) ]
                |> withBody data dataType caseStrategy extra
                |> withProperties properties

            promise {
                let! response = fetch url properties
                return! resolve response responseType caseStrategy extra decoder
            }
            |> Promise.catch (NetworkError >> Error)

        with exn -> promise { return PreparingRequestFailed exn |> Error }

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
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member inline tryFetchAs<'Data, 'Response> (url: string, ?decoder: Decoder<'Response>, ?data: 'Data,
                                                ?httpMethod: HttpMethod, ?properties: RequestProperties list,
                                                ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                                ?extra: ExtraCoders) =

        Fetch.__tryFetchAs(url, typeof<'Data>, typeof<'Response>,
                            ?decoder = decoder, ?data = data,
                            ?httpMethod = httpMethod, ?properties = properties,
                            ?headers = headers, ?caseStrategy = caseStrategy,
                            ?extra = extra)

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
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member inline fetchAs<'Data, 'Response> (url: string, ?decoder: Decoder<'Response>, ?data: 'Data,
                                             ?httpMethod: HttpMethod, ?properties: RequestProperties list,
                                             ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders) =
        promise {
            let! result = Fetch.tryFetchAs<'Data, 'Response>
                              (url, ?decoder = decoder, ?httpMethod = httpMethod, ?data = data, ?properties = properties,
                               ?headers = headers, ?caseStrategy = caseStrategy, ?extra = extra)
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
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member inline get<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                         ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                         ?extra: ExtraCoders, ?decoder: Decoder<'Response>) =
        Fetch.fetchAs
            (url, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder)

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
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member inline tryGet<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                            ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                            ?decoder: Decoder<'Response>) =
        Fetch.tryFetchAs
            (url, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder)

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
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member inline post<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                          ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                          ?extra: ExtraCoders, ?decoder: Decoder<'Response>) =
        Fetch.fetchAs
            (url, httpMethod = HttpMethod.POST, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder)

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
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member inline tryPost<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                             ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                             ?decoder: Decoder<'Response>) =
        Fetch.tryFetchAs
            (url, httpMethod = HttpMethod.POST, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder)

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
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member inline put<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                         ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                         ?extra: ExtraCoders, ?decoder: Decoder<'Response>) =
        Fetch.fetchAs
            (url, httpMethod = HttpMethod.PUT, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder)

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
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member inline tryPut<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                            ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                            ?decoder: Decoder<'Response>) =
        Fetch.tryFetchAs
            (url, httpMethod = HttpMethod.PUT, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder)

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
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member inline patch<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                           ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                           ?extra: ExtraCoders, ?decoder: Decoder<'Response>) =
        Fetch.fetchAs
            (url, httpMethod = HttpMethod.PATCH, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder)

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
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member inline tryPatch<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                              ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                              ?decoder: Decoder<'Response>) =
        Fetch.tryFetchAs
            (url, httpMethod = HttpMethod.PATCH, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder)

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
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the request failed
    ///
    static member inline delete<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                            ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy, ?extra: ExtraCoders,
                                            ?decoder: Decoder<'Response>) =
        Fetch.fetchAs
            (url, httpMethod = HttpMethod.DELETE, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder)

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
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member inline tryDelete<'Data, 'Response> (url: string, ?data: 'Data, ?properties: RequestProperties list,
                                               ?headers: HttpRequestHeaders list, ?caseStrategy: CaseStrategy,
                                               ?extra: ExtraCoders, ?decoder: Decoder<'Response>) =
        Fetch.tryFetchAs
            (url, httpMethod = HttpMethod.DELETE, ?data = data, ?properties = properties, ?headers = headers,
             ?caseStrategy = caseStrategy, ?extra = extra, ?decoder = decoder)
