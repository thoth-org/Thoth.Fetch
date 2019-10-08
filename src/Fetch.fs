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

    type [<Erase>] GlobalFetch =
         [<Global>]static member fetch (req: RequestInfo, ?init: RequestInit) = jsNative :JS.Promise<Response>

    let fetch (url: string) (init: RequestProperties list) : JS.Promise<Response> =
        GlobalFetch.fetch(RequestInfo.Url url, requestProps init)

    let withContentTypeJson data headers =
        match data with
        | Some _ -> ContentType "application/json" :: headers
        | _ -> headers

    let withBody data isCamelCase extra dataResolver properties =
        data  
        |> Option.map (fun data -> 
            Encode.Auto.toString (0, data, ?isCamelCase = isCamelCase, ?extra = extra, ?resolver = dataResolver)
            |> (!^) |> Body
            |> fun body -> body :: properties)
        |> Option.defaultValue properties

    let withProperties custom properties =
        custom 
        |> Option.map ((@) properties)
        |> Option.defaultValue properties

    let eitherUnit (responseResolver:ITypeResolver<'Response>) cont =
        if responseResolver.ResolveType().FullName = typedefof<unit>.FullName 
        then Ok (unbox ()) else cont()

    let resolve (response:Response) isCamelCase extra (responseResolver:ITypeResolver<'Response> option) =
        
        let decode body = Decode.Auto.fromString (body, ?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)
        let eitherUnitOr = eitherUnit responseResolver.Value

        promise {
            let! body  = response.text()
            let result =
                if response.Ok then
                    eitherUnitOr <| fun () ->
                        match decode body with 
                        | Ok value -> Ok value
                        | Error msg -> DecodingFailed msg |> Error
                else FetchFailed response |> Error
            return result
        }

    let message error =
        match error with
            | PreparingRequestFailed exn -> "Preparing request failed!\n\n" + exn.Message
            | DecodingFailed msg         -> "Decoding failed!\n\n" + msg
            | FetchFailed response       -> "Fetch failed!\n\n"+ string response.Status + " " + response.StatusText + " for URL " + response.Url
            | NetworkError exn           -> "Network Error!\n\n" + exn.Message

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
    ///   * `httpMethod` - optional parameter of type `HttpMethod` - HttpMethod used for Request, defaults to **GET**
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    /// 
    static member tryFetchAs<'Data,'Response>(url : string,
                                              ?httpMethod : HttpMethod,
                                              ?data : 'Data,
                                              ?properties : RequestProperties list,
                                              ?headers : HttpRequestHeaders list,
                                              ?isCamelCase : bool,
                                              ?extra: ExtraCoders,
                                              [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                              [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        try
            let properties =
                [ Method <| defaultArg httpMethod HttpMethod.GET
                  requestHeaders (defaultArg headers [] |> withContentTypeJson data) ]
                |> withBody data isCamelCase extra dataResolver
                |> withProperties properties

            promise {
                let! response = fetch url properties
                return! resolve response isCamelCase extra responseResolver 
            } |> Promise.catch (NetworkError >> Error)

        with | exn  -> promise { return PreparingRequestFailed exn |> Error }
   
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
    ///   * `httpMethod` - optional parameter of type `HttpMethod` - HttpMethod used, defaults to **GET**
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch's properties
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
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
    static member fetchAs<'Data,'Response>(url : string,
                                           ?httpMethod : HttpMethod,
                                           ?data : 'Data,
                                           ?properties : RequestProperties list,
                                           ?headers : HttpRequestHeaders list,
                                           ?isCamelCase : bool,
                                           ?extra: ExtraCoders,
                                           [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                           [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        promise{
            let! result = Fetch.tryFetchAs<'Data,'Response>(url, ?httpMethod = httpMethod, ?data = data, ?properties = properties, ?headers = headers, ?isCamelCase = isCamelCase, ?extra=extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
            let response =
                   match result with
                   | Ok response -> response
                   | Error error -> failwith (message error) 
            return response }
    
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
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
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
    static member get<'Data,'Response>(url : string,
                                         ?data : 'Data,
                                         ?properties : RequestProperties list,
                                         ?headers : HttpRequestHeaders list,
                                         ?isCamelCase : bool,
                                         ?extra: ExtraCoders,
                                         [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                         [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs(url, ?data= data, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
   
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
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryGet<'Data,'Response>(url : string,
                                            ?data : 'Data,
                                            ?properties : RequestProperties list,
                                            ?headers : HttpRequestHeaders list,
                                            ?isCamelCase : bool,
                                            ?extra: ExtraCoders,
                                            [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                            [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs(url, ?data= data, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
    
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
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
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
    static member post<'Data, 'Response>(url : string,
                                          ?data : 'Data,
                                          ?properties : RequestProperties list,
                                          ?headers : HttpRequestHeaders list,
                                          ?isCamelCase : bool,
                                          ?extra: ExtraCoders,
                                          [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                          [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs(url, httpMethod = HttpMethod.POST, ?data= data, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
   
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
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryPost<'Data,'Response>(url : string,
                                             ?data : 'Data,
                                             ?properties : RequestProperties list,
                                             ?headers : HttpRequestHeaders list,
                                             ?isCamelCase : bool,
                                             ?extra: ExtraCoders,
                                             [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                             [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs(url, httpMethod = HttpMethod.POST, ?data= data, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
    
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
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
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
    static member put<'Data,'Response>(url : string,
                                         ?data : 'Data,
                                         ?properties : RequestProperties list,
                                         ?headers : HttpRequestHeaders list,
                                         ?isCamelCase : bool,
                                         ?extra: ExtraCoders,
                                         [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                         [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs(url, httpMethod = HttpMethod.PUT, ?data= data, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
   
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
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryPut<'Data,'Response>(url : string,
                                            ?data : 'Data,
                                            ?properties : RequestProperties list,
                                            ?headers : HttpRequestHeaders list,
                                            ?isCamelCase : bool,
                                            ?extra: ExtraCoders,
                                            [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                            [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs(url, httpMethod = HttpMethod.PUT, ?data= data, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
    
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
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
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
    static member patch<'Data,'Response>(url : string,
                                           ?data : 'Data,
                                           ?properties : RequestProperties list,
                                           ?headers : HttpRequestHeaders list,
                                           ?isCamelCase : bool,
                                           ?extra: ExtraCoders,
                                           [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                           [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs(url, httpMethod = HttpMethod.PATCH, ?data= data, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
   
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
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryPatch<'Data,'Response>(url : string,
                                              ?data : 'Data,
                                              ?properties : RequestProperties list,
                                              ?headers : HttpRequestHeaders list,
                                              ?isCamelCase : bool,
                                              ?extra: ExtraCoders,
                                              [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                              [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs(url, httpMethod = HttpMethod.PATCH, ?data= data, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
    
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
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
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
    static member delete<'Data,'Response>(url : string,
                                            ?data : 'Data,
                                            ?properties : RequestProperties list,
                                            ?headers : HttpRequestHeaders list,
                                            ?isCamelCase : bool,
                                            ?extra: ExtraCoders,
                                            [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                            [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs(url, httpMethod = HttpMethod.DELETE, ?data= data, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
   
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
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,FetchError>>`
    ///
    /// **Exceptions**
    ///
    static member tryDelete<'Data,'Response>(url : string,
                                               ?data : 'Data,
                                               ?properties : RequestProperties list,
                                               ?headers : HttpRequestHeaders list,
                                               ?isCamelCase : bool,
                                               ?extra: ExtraCoders,
                                               [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                               [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs(url, httpMethod = HttpMethod.DELETE, ?data= data, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
