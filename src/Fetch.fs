module Thoth.Fetch

open Fetch
open Fable.Core
open Fable.Core.JsInterop
open Thoth.Json



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
    ///   * `httpMethod` - optional parameter of type `HttpMethod` - HttpMethod used, defaults to GET
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryFetchAs<'Data,'Response>(url : string,
                                              ?httpMethod : HttpMethod,
                                              ?data : 'Data,
                                              ?decoder : Decoder<'Response>,
                                              ?encoder : Encoder<'Data>,
                                              ?properties : RequestProperties list,
                                              ?headers : HttpRequestHeaders list,
                                              ?isCamelCase : bool,
                                              ?extra: ExtraCoders,
                                              [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                              [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
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
                match encoder with
                | Some encoder  -> [ data |> encoder |> toJsonBody |> Body ]
                | _-> [ Fetch.toBody<'Data>(data, ?isCamelCase= isCamelCase, ?extra = extra, ?dataResolver = dataResolver) ]) 
               |> Option.defaultValue [])  

        promise {
            let! response = Fetch.fetch url properties
            let! body = response.text()
            let result =
                if responseResolver.Value.ResolveType().FullName = typedefof<unit>.FullName 
                then 
                    Ok (unbox ())
                else
                    match decoder with
                    | Some decoder -> Decode.fromString decoder body
                    | _ -> Fetch.fromBody (body, ?isCamelCase= isCamelCase, ?extra = extra, ?responseResolver= responseResolver)
            return result }

   
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
    ///   * `httpMethod` - optional parameter of type `HttpMethod` - HttpMethod used, defaults to GET
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
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
                                           ?decoder : Decoder<'Response>,
                                           ?encoder : Encoder<'Data>,
                                           ?properties : RequestProperties list,
                                           ?headers : HttpRequestHeaders list,
                                           ?isCamelCase : bool,
                                           ?extra: ExtraCoders,
                                           [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                           [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        promise{
            let! result = Fetch.tryFetchAs<'Data,'Response>(url, ?httpMethod = httpMethod, ?data = data, ?decoder = decoder, ?encoder = encoder, ?properties = properties, ?headers = headers, ?isCamelCase = isCamelCase, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
            let response =
                   match result with
                   | Ok response -> response
                   | Error msg -> failwith msg
            return response}
    
    /// **Description**
    ///
    /// Send a **GET** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type if required
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type if required
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
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
                                         ?decoder : Decoder<'Response>,
                                         ?encoder : Encoder<'Data>,
                                         ?properties : RequestProperties list,
                                         ?headers : HttpRequestHeaders list,
                                         ?isCamelCase : bool,
                                         ?extra: ExtraCoders,
                                         [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                         [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs(url, ?data= data, ?decoder = decoder, ?encoder = encoder, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
   
    /// **Description**
    ///
    /// Send a **GET** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type if required
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type if required
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryGet<'Data,'Response>(url : string,
                                            ?data : 'Data,
                                            ?decoder : Decoder<'Response>,
                                            ?encoder : Encoder<'Data>,
                                            ?properties : RequestProperties list,
                                            ?headers : HttpRequestHeaders list,
                                            ?isCamelCase : bool,
                                            ?extra: ExtraCoders,
                                            [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                            [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs(url, ?data= data, ?decoder = decoder, ?encoder = encoder, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
    
    /// **Description**
    ///
    /// Send a **POST** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type if required
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type if required
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
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
                                          ?decoder : Decoder<'Response>,
                                          ?encoder : Encoder<'Data>,
                                          ?properties : RequestProperties list,
                                          ?headers : HttpRequestHeaders list,
                                          ?isCamelCase : bool,
                                          ?extra: ExtraCoders,
                                          [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                          [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs(url, httpMethod = HttpMethod.POST, ?data= data, ?decoder = decoder, ?encoder = encoder, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
   
    /// **Description**
    ///
    /// Send a **POST** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type if required
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type if required
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryPost<'Data,'Response>(url : string,
                                             ?data : 'Data,
                                             ?decoder : Decoder<'Response>,
                                             ?encoder : Encoder<'Data>,
                                             ?properties : RequestProperties list,
                                             ?headers : HttpRequestHeaders list,
                                             ?isCamelCase : bool,
                                             ?extra: ExtraCoders,
                                             [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                             [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs(url, httpMethod = HttpMethod.POST, ?data= data, ?decoder = decoder, ?encoder = encoder, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
    
    /// **Description**
    ///
    /// Send a **PUT** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type if required
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type if required
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
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
                                         ?decoder : Decoder<'Response>,
                                         ?encoder : Encoder<'Data>,
                                         ?properties : RequestProperties list,
                                         ?headers : HttpRequestHeaders list,
                                         ?isCamelCase : bool,
                                         ?extra: ExtraCoders,
                                         [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                         [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs(url, httpMethod = HttpMethod.PUT, ?data= data, ?decoder = decoder, ?encoder = encoder, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
   
    /// **Description**
    ///
    /// Send a **PUT** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type if required
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type if required
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryPut<'Data,'Response>(url : string,
                                            ?data : 'Data,
                                            ?decoder : Decoder<'Response>,
                                            ?encoder : Encoder<'Data>,
                                            ?properties : RequestProperties list,
                                            ?headers : HttpRequestHeaders list,
                                            ?isCamelCase : bool,
                                            ?extra: ExtraCoders,
                                            [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                            [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs(url, httpMethod = HttpMethod.PUT, ?data= data, ?decoder = decoder, ?encoder = encoder, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
    
    /// **Description**
    ///
    /// Send a **PATCH** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type if required
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type if required
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
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
                                           ?decoder : Decoder<'Response>,
                                           ?encoder : Encoder<'Data>,
                                           ?properties : RequestProperties list,
                                           ?headers : HttpRequestHeaders list,
                                           ?isCamelCase : bool,
                                           ?extra: ExtraCoders,
                                           [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                           [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs(url, httpMethod = HttpMethod.PATCH, ?data= data, ?decoder = decoder, ?encoder = encoder, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
   
    /// **Description**
    ///
    /// Send a **PATCH** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type if required
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type if required
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    /// 
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryPatch<'Data,'Response>(url : string,
                                              ?data : 'Data,
                                              ?decoder : Decoder<'Response>,
                                              ?encoder : Encoder<'Data>,
                                              ?properties : RequestProperties list,
                                              ?headers : HttpRequestHeaders list,
                                              ?isCamelCase : bool,
                                              ?extra: ExtraCoders,
                                              [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                              [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs(url, httpMethod = HttpMethod.PATCH, ?data= data, ?decoder = decoder, ?encoder = encoder, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
    
    /// **Description**
    ///
    /// Send a **DELETE** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"` if data is provided.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type if required
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type if required
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
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
                                            ?decoder : Decoder<'Response>,
                                            ?encoder : Encoder<'Data>,
                                            ?properties : RequestProperties list,
                                            ?headers : HttpRequestHeaders list,
                                            ?isCamelCase : bool,
                                            ?extra: ExtraCoders,
                                            [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                            [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.fetchAs(url, httpMethod = HttpMethod.DELETE, ?data= data, ?decoder = decoder, ?encoder = encoder, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
   
    /// **Description**
    ///
    /// Send a **DELETE** request to the specified resource and apply the provided `decoder` to the response.
    ///
    /// This method set the `ContentType` header to `"application/json"`.
    ///
    /// An encoder will be generated or retrieved from the cache for the `'Data` type if required
    ///
    /// A decoder will be generated or retrieved from the cache for the `'Response` type if required
    ///
    /// An exception will be thrown if the decoder failed.
    ///
    /// **Parameters**
    ///   * `url` - parameter of type `string` - URL to request
    ///   * `data` - optional parameter of type `'Data` - Data sent via the body, it will be converted to JSON before
    ///   * `decoder` - optional parameter of type `Decoder<'Response>`- Decoder applied to the server response
    ///   * `encoder` - optional parameter of type `Encoder<'Data>`- Decoder applied to the server response
    ///   * `properties` - optional parameter of type `RequestProperties list` - Parameters passed to fetch
    ///   * `headers` - optional parameter of type `HttpRequestHeaders list` - Parameters passed to fetch
    ///   * `isCamelCase` - optional parameter of type `bool` - Options passed to Thoth.Json to control JSON keys representation
    ///   * `extra` - optional parameter of type `ExtraCoders` - Options passed to Thoth.Json to extends the known coders
    ///   * `responseResolver` - optional parameter of type `ITypeResolver<'Response>` - Used by Fable to provide generic type info
    ///   * `dataResolver` - parameter of type `ITypeResolver<'Data> option` - Used by Fable to provide generic type info
    ///
    /// **Output Type**
    ///   * `JS.Promise<Result<'Response,string>>`
    ///
    /// **Exceptions**
    ///
    static member tryDelete<'Data,'Response>(url : string,
                                               ?data : 'Data,
                                               ?decoder : Decoder<'Response>,
                                               ?encoder : Encoder<'Data>,
                                               ?properties : RequestProperties list,
                                               ?headers : HttpRequestHeaders list,
                                               ?isCamelCase : bool,
                                               ?extra: ExtraCoders,
                                               [<Inject>] ?responseResolver: ITypeResolver<'Response>,
                                               [<Inject>] ?dataResolver: ITypeResolver<'Data>) =
        Fetch.tryFetchAs(url, httpMethod = HttpMethod.DELETE, ?data= data, ?decoder = decoder, ?encoder = encoder, ?properties = properties, ?headers= headers, ?isCamelCase = isCamelCase, ?extra = extra, ?responseResolver = responseResolver, ?dataResolver = dataResolver)
