module Thoth.Fetch

open Fetch
open Fable.Core
open Fable.Core.JsInterop
open Thoth.Json

type FetchError =
    | PreparingRequestFailed of exn
    | DecodingFailed of string
    | BadStatus of Response
    | NetworkError of exn

type [<Erase>] GlobalFetch =
        [<Global>]static member fetch (req: RequestInfo, ?init: RequestInit) = jsNative :JS.Promise<Response>

let private globalFetch (url: string) (init: RequestProperties list) : JS.Promise<Response> =
    GlobalFetch.fetch(RequestInfo.Url url, requestProps init)

let internal toJsonBody (value : JsonValue) : BodyInit=
    #if DEBUG
    Encode.toString 4 value
    |> (!^)
    #else
    Encode.toString 0 value
    |> (!^)
    #endif

type Fetch =

    static member internal toBody<'Data>(data:'Data, ?isCamelCase:bool, ?extra:ExtraCoders, [<Inject>]?dataResolver: ITypeResolver<'Data>) =
        let encode = Encode.Auto.generateEncoderCached<'Data>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = dataResolver)
        encode data |> toJsonBody |> Body
        
        
    static member internal fromBody<'Response>(value:string, ?isCamelCase:bool, ?extra:ExtraCoders, [<Inject>]?responseResolver: ITypeResolver<'Response>) =
        let decoder = Decode.Auto.generateDecoderCached<'Response>(?isCamelCase = isCamelCase, ?extra = extra, ?resolver = responseResolver)
        Decode.fromString decoder value 
        
    /// **Description**
    ///
    /// Send a request to the specified resource and apply a `decoder` to the response.
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type.
    ///
    /// If fetch and decoding succeed, we return `Ok 'Response`.
    ///
    /// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `httpMethod` - optional parameter of type `HttpMethod` - HttpMethod used for Request, defaults to **GET**
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
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
            let headers = 
                match data with
                | Some _ -> ContentType "application/json" :: defaultArg headers []
                | _ -> defaultArg headers []  

            let properties =
                [  Method <| defaultArg httpMethod HttpMethod.GET
                   requestHeaders headers ]
                @ defaultArg properties []
                @ (data 
                   |> Option.map (fun data ->
                        [ Fetch.toBody<'Data>(data, ?isCamelCase= isCamelCase, ?extra = extra, ?dataResolver = dataResolver) ]) 
                   |> Option.defaultValue []) 

            promise {
                let! response = globalFetch url properties
                let! body = response.text()
                let result =
                    if response.Ok then 
                        if responseResolver.Value.ResolveType().FullName = typedefof<unit>.FullName 
                        then 
                            Ok (unbox ())
                        else
                            Fetch.fromBody (body, ?isCamelCase= isCamelCase, ?extra = extra, ?responseResolver= responseResolver)
                            |> function
                               | Ok value -> Ok value
                               | Error msg -> DecodingFailed msg |> Error 
                    else BadStatus response |> Error
                return result }
            |> Promise.catch (NetworkError >> Error)

        with | exn  -> promise { return PreparingRequestFailed exn |> Error }
   
    /// **Description**
    ///
    /// Send a request to the specified resource and apply a `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type 
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type 
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `httpMethod` - optional parameter of type `HttpMethod` - HttpMethod used, defaults to **GET**
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
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
            let! result = Fetch.tryFetchAs<'Data,'Response>(url, ?httpMethod = httpMethod, ?data = data, ?properties = properties, ?headers = headers, ?isCamelCase = isCamelCase, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
            let response =
                   match result with
                   | Ok response -> response
                   | Error error ->
                        match error with
                        | PreparingRequestFailed exn -> raise exn
                        | DecodingFailed msg -> failwith ("Decoding failed!\n\n" + msg)
                        | BadStatus response -> failwith (string response.Status + " " + response.StatusText + " for URL " + response.Url)
                        | NetworkError exn -> failwith ("NetworkError!\n\n" + exn.Message)
            return response}
    
    /// **Description**
    ///
    /// Send a **GET** request to the specified resource and apply a `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type 
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type 
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
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
    /// Send a **GET** request to the specified resource and apply a `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type 
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type 
    ///
    /// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
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
    /// Send a **POST** request to the specified resource and apply a `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type 
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type 
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
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
    /// Send a **POST** request to the specified resource and apply a `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type 
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type 
    ///
    /// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
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
    /// Send a **PUT** request to the specified resource and apply a `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type 
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type 
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
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
    /// Send a **PUT** request to the specified resource and apply a `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type 
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type 
    ///
    /// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
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
    /// Send a **PATCH** request to the specified resource and apply a `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type 
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type 
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
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
    /// Send a **PATCH** request to the specified resource and apply a `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type 
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type 
    ///
    /// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
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
    /// Send a **DELETE** request to the specified resource and apply a `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type 
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type 
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<'Response>`
    ///
    /// **Exceptions**
    ///   * `System.Exception` - Contains information explaining why the decoder failed
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
    /// Send a **DELETE** request to the specified resource and apply a `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type 
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type 
    ///
    /// If we failed, we return `Error (FetchError)` containing an better explanation.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
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
