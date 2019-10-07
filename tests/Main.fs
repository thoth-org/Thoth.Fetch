module Tests.Main

open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.Testing
open Thoth.Fetch
open Thoth.Json
open Node
open System 

[<Global>]
let it (msg: string) (f: (obj->unit)->unit): unit = jsNative

[<Global>]
let describe (msg: string) (f: unit->unit): unit = jsNative

[<Global>]
let before (f: unit->unit): unit = jsNative

[<Global>]
let after (f: unit->unit): unit = jsNative

let mutable serverInstance = Unchecked.defaultof<Http.Server>
let databaseCreationDate = DateTime.UtcNow

type FakeDeleteResponse =
    { IsSuccess : bool }

    static member Decoder =
        Decode.object (fun get ->
            { IsSuccess = get.Required.Field "isSuccess" Decode.bool}
        )
    static member Encoder (r:FakeDeleteResponse) = failwith "NotImplemented"

let fakeDeleteResponseCoder = Extra.withCustom FakeDeleteResponse.Encoder FakeDeleteResponse.Decoder Extra.empty 

type Book =
    { Id : int
      Title : string
      Author : string
      CreatedAt : DateTime
      UpdatedAt : DateTime option }

    static member Decoder =
        Decode.object (fun get ->
            { Id = get.Required.Field "id" Decode.int
              Title = get.Required.Field "title" Decode.string
              Author = get.Required.Field "author" Decode.string
              CreatedAt = get.Required.Field "createdAt" Decode.datetime
              UpdatedAt = get.Optional.Field "updatedAt" Decode.datetime }
        )

    static member Encoder (book : Book)=
        Encode.object [
            "id", Encode.int book.Id
            "title", Encode.string book.Title
            "author", Encode.string book.Author
            "createdAt", Encode.datetime book.CreatedAt
            "updatedAt", Encode.option Encode.datetime book.UpdatedAt
        ]

let bookCoder: ExtraCoders = Extra.withCustom Book.Encoder Book.Decoder Extra.empty

type Author =
    { Id : int
      Name : string }

    static member WrongDecoder =
        Decode.object (fun get ->
            { Id = get.Required.Field "id" Decode.int
              Name = get.Required.Field "author" Decode.string
            }
        )

    static member MissingEncoder (author : Author)=
        failwith "Not Implemented"

let brokenAuthorCoder = Extra.withCustom Author.MissingEncoder Author.WrongDecoder Extra.empty

type Database =
    { Books : Book list
      Authors : Author list }

let initialDatabase =
    { Books =
        [ { Id = 1
            Title = "The Warded Man"
            Author = "Peter V. Brett"
            CreatedAt = databaseCreationDate
            UpdatedAt = None }
          { Id = 2
            Title = "Prince of Thorns"
            Author = "Mark Lawrence"
            CreatedAt = databaseCreationDate
            UpdatedAt = None } ]
      Authors =
        [ { Id = 1
            Name = "Peter V. Brett" }
          { Id = 2
            Name = "Mark Lawrence" } ] }

[<Import("*", "json-server")>]
let jsonServer : obj = jsNative

[<Import("default", "./fake-delete.js")>]
let fakeDeleteHandler : obj = jsNative

[<Import("default", "./fake-unit.js")>]
let fakeUnitHandler : obj = jsNative

Node.Api.``global``?fetch <- import "*" "node-fetch"

describe "Thoth.Fetch" <| fun _ ->

    // Set up the json-server instance
    // We are using dynamic typing because `Express` bindings have not been updated to
    // Fable.Core 3 yet.
    // And I don't have time to upgrade it yet

    before <| fun _ ->
        let dbFile = Node.Api.path.join(Node.Api.__dirname, "db.json")
        try
            Node.Api.fs.unlinkSync(U2.Case1 dbFile)
        with
            | _ -> ()

        Node.Api.fs.writeFileSync(dbFile, Encode.Auto.toString(4, initialDatabase, isCamelCase = true))

        let server = jsonServer?create()

        let defaultOptions =
            createObj [
                "logger" ==> false
            ]

        server?``use``(jsonServer?defaults(defaultOptions))
        server?delete("/fake-delete", fakeDeleteHandler)
        server?``get``("/get/unit", fakeUnitHandler)
        server?post("/post/unit", fakeUnitHandler)
        server?delete("/delete/unit", fakeUnitHandler)
        server?put("/put/unit", fakeUnitHandler)
        server?patch("/patch/unit", fakeUnitHandler)
        server?``use``(jsonServer?router(dbFile))
        serverInstance <- server?listen(3000, !!ignore)

    after <| fun _ ->
        serverInstance?close()

    // End of the set up

    describe "Fetch.fetchAs" <| fun _ ->

        it "Fetch.fetchAs works with extra coder" <| fun d ->
            promise {
                let! res = Fetch.fetchAs("http://localhost:3000/books/1", extra = bookCoder, isCamelCase = true)
                let expected =
                    { Id = 1
                      Title = "The Warded Man"
                      Author = "Peter V. Brett"
                      CreatedAt = databaseCreationDate
                      UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start                 


       
        it "Fetch.fetchAs works with auto decoder and camelCase = true" <| fun d ->
            promise {
                let! res = Fetch.fetchAs("http://localhost:3000/books/1", isCamelCase = true)
                let expected =
                    { Id = 1
                      Title = "The Warded Man"
                      Author = "Peter V. Brett"
                      CreatedAt = databaseCreationDate
                      UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start      

        it "Fetch.fetchAs works with auto decoder and CamelCase = false" <| fun d ->
            promise {
                let! res = Fetch.fetchAs("http://localhost:3000/books/1", isCamelCase = false)
                let expected =
                    { Id = 1
                      Title = "The Warded Man"
                      Author = "Peter V. Brett"
                      CreatedAt = databaseCreationDate
                      UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start              
    
        it "Fetch.fetchAs throw an exception explaining why the extra coder failed" <| fun d ->
            promise {
                let! _ = Fetch.fetchAs("http://localhost:3000/authors/1", extra = bookCoder, isCamelCase = true)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Decoding failed!

The following errors were found:

Error at: `$`
Expecting an object with a field named `title` but instead got:
{
    "id": 1,
    "name": "Peter V. Brett"
}

Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "id": 1,
    "name": "Peter V. Brett"
}

Error at: `$`
Expecting an object with a field named `createdAt` but instead got:
{
    "id": 1,
    "name": "Peter V. Brett"
}
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start
        
        it "Fetch.fetchAs throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let! _ = Fetch.fetchAs("http://localhost:3000/authors/1")
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryFetchAs" <| fun _ ->
        it "Fetch.tryFetchAs works with extra coder" <| fun d ->
            promise {
                let! res = Fetch.tryFetchAs("http://localhost:3000/books/1", extra = bookCoder, isCamelCase = true)
                let expected =
                    Ok { Id = 1
                         Title = "The Warded Man"
                         Author = "Peter V. Brett"
                         CreatedAt = databaseCreationDate
                         UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start
        
        it "Fetch.tryFetchAs works with auto decoder" <| fun d ->
            promise {
                let! res = Fetch.tryFetchAs("http://localhost:3000/books/1")
                let expected =
                    Ok { Id = 1
                         Title = "The Warded Man"
                         Author = "Peter V. Brett"
                         CreatedAt = databaseCreationDate
                         UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start


        it "Fetch.tryFetchAs returns an error explaining why the extra coder failed" <| fun d ->
            promise {
                let! res = Fetch.tryFetchAs<_, Author>("http://localhost:3000/authors/1", extra = brokenAuthorCoder, isCamelCase = true)
                let expected =
                    Error(
                        DecodingFailed(
                            """
Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "id": 1,
    "name": "Peter V. Brett"
}
                        """.Trim()
                    ))
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

    
        it "Fetch.tryFetchAs returns an error explaining why the auto decoder failed" <| fun d ->
            promise {
                let! res = Fetch.tryFetchAs<_, Book>("http://localhost:3000/authors/1")
                let expected =
                    Error(
                        DecodingFailed(
                            """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    ))
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.get" <| fun _ ->
        it "Fetch.get works with extra coder" <| fun d ->
            promise {
                let! res = Fetch.get("http://localhost:3000/books/1", extra = bookCoder, isCamelCase = true)
                let expected =
                    { Id = 1
                      Title = "The Warded Man"
                      Author = "Peter V. Brett"
                      CreatedAt = databaseCreationDate
                      UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.get works with auto decoder" <| fun d ->
            promise {
                let! res = Fetch.get("http://localhost:3000/books/1", isCamelCase = true)
                let expected =
                    { Id = 1
                      Title = "The Warded Man"
                      Author = "Peter V. Brett"
                      CreatedAt = databaseCreationDate
                      UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start
        it "Fetch.get throw an exception explaining why the extra coder failed" <| fun d ->
            promise {
                let! _ = Fetch.get("http://localhost:3000/authors/1", extra = bookCoder, isCamelCase = true)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Decoding failed!

The following errors were found:

Error at: `$`
Expecting an object with a field named `title` but instead got:
{
    "id": 1,
    "name": "Peter V. Brett"
}

Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "id": 1,
    "name": "Peter V. Brett"
}

Error at: `$`
Expecting an object with a field named `createdAt` but instead got:
{
    "id": 1,
    "name": "Peter V. Brett"
}
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start


        it "Fetch.get throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let! _ = Fetch.get<_, Book>("http://localhost:3000/authors/1")
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Decoding failed!

Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.get works with unit response" <| fun d ->
            promise {
                let! res = Fetch.get("http://localhost:3000/get/unit")
                Assert.AreEqual(res, ())
                d()
            } |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryGet" <| fun _ ->
        it "Fetch.tryGet works with auto decoder" <| fun d ->
            promise {
                let! res = Fetch.tryGet("http://localhost:3000/books/1")
                let expected =
                    Ok { Id = 1
                         Title = "The Warded Man"
                         Author = "Peter V. Brett"
                         CreatedAt = databaseCreationDate
                         UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryGet returns an error explaining why the extra coder failed" <| fun d ->
            promise {
                let! res = Fetch.tryGet<_, Author>("http://localhost:3000/authors/1",  extra = brokenAuthorCoder, isCamelCase = true)
                let expected =
                    Error(
                        DecodingFailed(
                            """
Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "id": 1,
    "name": "Peter V. Brett"
}
                        """.Trim()
                    ))
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryGet returns an error explaining why the auto decoder failed" <| fun d ->
            promise {
                let! res = Fetch.tryGet<_, Book>("http://localhost:3000/authors/1")
                let expected =
                    Error(
                        DecodingFailed(
                            """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    ))
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryGet works with unit response" <| fun d ->
            promise {
                let! res = Fetch.tryGet("http://localhost:3000/get/unit")
                let expected = Ok ()
                Assert.AreEqual(res, expected)
                d()
            } |> Promise.catch d
            |> Promise.start

    describe "Fetch.post" <| fun _ ->
      
        it "Fetch.post works with extra coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let data =
                    Encode.object [
                        "title", Encode.string "The Amulet of Samarkand"
                        "author", Encode.string "Jonathan Stroud"
                        "createdAt", Encode.datetime now
                    ]
                let! res = Fetch.post("http://localhost:3000/books", data,  extra = bookCoder, isCamelCase = true)
                let expected =
                    { Id = 3
                      Title = "The Amulet of Samarkand"
                      Author = "Jonathan Stroud"
                      CreatedAt = now
                      UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.post works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                        createdAt = now
                    |}

                let! res = Fetch.post("http://localhost:3000/books", data)
                let expected =
                    { Id = 4
                      Title = "The Golem's Eye"
                      Author = "Jonathan Stroud"
                      CreatedAt = now
                      UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.post throw an exception explaining why the extra coder failed" <| fun d ->
            promise {
                let data =
                    Encode.object [
                        "name", Encode.string "Brandon Sanderson"
                    ]
                let! _ = Fetch.post("http://localhost:3000/authors", data,  extra = bookCoder, isCamelCase = true)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Decoding failed!

The following errors were found:

Error at: `$`
Expecting an object with a field named `title` but instead got:
{
    "name": "Brandon Sanderson",
    "id": 3
}

Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "name": "Brandon Sanderson",
    "id": 3
}

Error at: `$`
Expecting an object with a field named `createdAt` but instead got:
{
    "name": "Brandon Sanderson",
    "id": 3
}
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start
  
        it "Fetch.post throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! _ = Fetch.post<_, Book>("http://localhost:3000/authors", data)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Decoding failed!

Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.post works with unit response" <| fun d ->
            promise {
                let data = {| mailto = "Maxime"|}
                let! res = Fetch.post<_, unit>("http://localhost:3000/post/unit", data)
                Assert.AreEqual(res, ())
                d()
            } |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryPost" <| fun _ ->
        it "Fetch.tryPost works with extra coder" <| fun d ->
        promise {
            let now = DateTime.UtcNow
            let data =
                Encode.object [
                    "title", Encode.string "The Amulet of Samarkand"
                    "author", Encode.string "Jonathan Stroud"
                    "createdAt", Encode.datetime now
                ]
            let! res = Fetch.tryPost("http://localhost:3000/books", data,  extra = bookCoder, isCamelCase = true)
            let expected =
                Ok { Id = 5
                     Title = "The Amulet of Samarkand"
                     Author = "Jonathan Stroud"
                     CreatedAt = now
                     UpdatedAt = None }

            Assert.AreEqual(res, expected)
            d()
        }
        |> Promise.catch d
        |> Promise.start

       
        it "Fetch.tryPost works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                        createdAt = now
                    |}

                let! res = Fetch.tryPost("http://localhost:3000/books", data)
                let expected =
                    Ok { Id = 4
                         Title = "The Golem's Eye"
                         Author = "Jonathan Stroud"
                         CreatedAt = now
                         UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

      
        it "Fetch.tryPost throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! res = Fetch.tryPost<_, Book>("http://localhost:3000/authors", data)

                let expected =
                    Error(
                        DecodingFailed(
                            """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    ))

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start


 
    describe "Fetch.put" <| fun _ ->
    

        it "Fetch.put works with extra coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs("http://localhost:3000/books/3")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.put("http://localhost:3000/books/3", updatedBook, extra = bookCoder, isCamelCase = true)

                Assert.AreEqual(res, updatedBook)
                d()
            }
            |> Promise.catch d
            |> Promise.start


        it "Fetch.put works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs("http://localhost:3000/books/4")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.put("http://localhost:3000/books/4", updatedBook, isCamelCase = true)
                
                Assert.AreEqual(res, updatedBook)
                d()
            }
            |> Promise.catch d
            |> Promise.start



        it "Fetch.put throw an exception explaining why the extra coder failed" <| fun d ->
            promise {
                let data =
                    Encode.object [
                        "name", Encode.string "Brandon Sanderson"
                    ]
                let! _ = Fetch.put("http://localhost:3000/authors/1", data,  extra = bookCoder, isCamelCase = true)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Decoding failed!

The following errors were found:

Error at: `$`
Expecting an object with a field named `title` but instead got:
{
    "name": "Brandon Sanderson",
    "id": 1
}

Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "name": "Brandon Sanderson",
    "id": 1
}

Error at: `$`
Expecting an object with a field named `createdAt` but instead got:
{
    "name": "Brandon Sanderson",
    "id": 1
}
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.put throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! _ = Fetch.put<_, Book>("http://localhost:3000/authors/1", data)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Decoding failed!

Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.put works with unit response" <| fun d ->
            promise {
                let! res = Fetch.put("http://localhost:3000/put/unit")
                Assert.AreEqual(res, ())
                d()
            } |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryPut" <| fun _ ->
        it "Fetch.tryPut works with extra coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs("http://localhost:3000/books/5")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.tryPut("http://localhost:3000/books/5", updatedBook, extra = bookCoder, isCamelCase = true)
                let expected = Ok updatedBook

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPut works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs("http://localhost:3000/books/5")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.tryPut("http://localhost:3000/books/5", updatedBook, isCamelCase = true)
                let expected = Ok updatedBook

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPut throw an exception explaining why the extra coder failed" <| fun d ->
            promise {
                let data =
                    Encode.object [
                        "name", Encode.string "Brandon Sanderson"
                    ]
                let! res = Fetch.tryPut<_,Author>("http://localhost:3000/authors/1", data,  extra = brokenAuthorCoder, isCamelCase = true)
                let expected =
                    Error(
                        DecodingFailed(
                            """
Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "name": "Brandon Sanderson",
    "id": 1
}
                        """.Trim()
                    ))
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPut throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! res = Fetch.tryPut<_, Book>("http://localhost:3000/authors/1", data)

                let expected =
                    Error(
                        DecodingFailed(
                            """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    ))

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPut works with unit response" <| fun d ->
            promise {
                let! res = Fetch.tryPut<_, unit>("http://localhost:3000/put/unit", null)
                let expected = Ok ()
                Assert.AreEqual(res, expected)
                d()
            } |> Promise.catch d
            |> Promise.start

    describe "Fetch.patch" <| fun _ ->
        it "Fetch.patch works with extra coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs("http://localhost:3000/books/3")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.patch("http://localhost:3000/books/3", updatedBook,  extra = bookCoder, isCamelCase = true)

                Assert.AreEqual(res, updatedBook)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.patch works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs("http://localhost:3000/books/4")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.patch("http://localhost:3000/books/4", updatedBook, isCamelCase = true)

                Assert.AreEqual(res, updatedBook)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.patch throw an exception explaining why the extra coder failed" <| fun d ->
            promise {
                let data =
                    Encode.object [
                        "name", Encode.string "Brandon Sanderson"
                    ]
                let! _ = Fetch.patch<_, Author>("http://localhost:3000/authors/1", data, extra = brokenAuthorCoder, isCamelCase = true)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Decoding failed!

Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "id": 1,
    "name": "Brandon Sanderson"
}
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.patch throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! _ = Fetch.patch<_, Book>("http://localhost:3000/authors/1", data)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Decoding failed!

Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.patch works with unit response" <| fun d ->
            promise {
                let! res = Fetch.patch("http://localhost:3000/patch/unit", null)
                Assert.AreEqual(res, ())
                d()
            } |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryPatch" <| fun _ ->
        it "Fetch.tryPatch works with extra coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs("http://localhost:3000/books/5")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.tryPatch("http://localhost:3000/books/5",updatedBook,  extra = bookCoder, isCamelCase = true)
                let expected = Ok updatedBook

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatch works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs("http://localhost:3000/books/5")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.tryPatch("http://localhost:3000/books/5", updatedBook, isCamelCase = true)
                let expected = Ok updatedBook

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatch throw an exception explaining why the extra coder failed" <| fun d ->
            promise {
                let data =
                    Encode.object [
                        "name", Encode.string "Brandon Sanderson"
                    ]
                let! res = Fetch.tryPatch<_, Author>("http://localhost:3000/authors/1", data,  extra = brokenAuthorCoder, isCamelCase = true)
                let expected =
                    Error(
                        DecodingFailed(
                            """
Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "id": 1,
    "name": "Brandon Sanderson"
}
                        """.Trim()
                    ))
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatch throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! res = Fetch.tryPatch<_, Book>("http://localhost:3000/authors/1", data)

                let expected =
                    Error(
                        DecodingFailed(
                            """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    ))

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatch works with unit response" <| fun d ->
            promise {
                let! res = Fetch.tryPatch<_, unit>("http://localhost:3000/patch/unit", null)
                let expected = Ok ()
                Assert.AreEqual(res, expected)
                d()
            } |> Promise.catch d
            |> Promise.start

    describe "Fetch.delete" <| fun _ ->
        it "Fetch.delete works with extra coder" <| fun d ->
            promise {
                let! res = Fetch.delete("http://localhost:3000/fake-delete", null, extra = fakeDeleteResponseCoder, isCamelCase = true)
                let expected =
                    { IsSuccess = true }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.delete works with auto coder" <| fun d ->
            promise {
                let! res = Fetch.delete<_,FakeDeleteResponse>("http://localhost:3000/fake-delete", isCamelCase = true)
                let expected =
                    { IsSuccess = true }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.delete throw an exception explaining why the extra coder failed" <| fun d ->
            promise {
                let! _ = Fetch.delete("http://localhost:3000/fake-delete", null,  extra = bookCoder, isCamelCase = true)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Decoding failed!

The following errors were found:

Error at: `$`
Expecting an object with a field named `id` but instead got:
{
    "isSuccess": true
}

Error at: `$`
Expecting an object with a field named `title` but instead got:
{
    "isSuccess": true
}

Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "isSuccess": true
}

Error at: `$`
Expecting an object with a field named `createdAt` but instead got:
{
    "isSuccess": true
}
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.delete throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let! _ = Fetch.delete<_, Book>("http://localhost:3000/fake-delete", null)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
Decoding failed!

Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                    """.Trim()
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.delete works with unit response" <| fun d ->
            promise {
                let! res = Fetch.delete<_, unit>("http://localhost:3000/delete/unit", null)
                Assert.AreEqual(res, ())
                d()
            } |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryDelete" <| fun _ ->
        it "Fetch.tryDelete works with extra coder" <| fun d ->
            promise {
                let! res = Fetch.tryDelete("http://localhost:3000/fake-delete", null, extra = fakeDeleteResponseCoder, isCamelCase = true)
                let expected =
                    Ok { IsSuccess = true }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryDelete works with auto coder" <| fun d ->
            promise {
                let! res = Fetch.tryDelete<_,FakeDeleteResponse>("http://localhost:3000/fake-delete", null, isCamelCase = true)
                let expected =
                    Ok { IsSuccess = true }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryDelete throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let! res = Fetch.tryDelete<_, Book>("http://localhost:3000/fake-delete", null)
                let expected =
                    Error(
                        DecodingFailed(
                            """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    ))
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryDelete works with unit response" <| fun d ->
            promise {
                let! res = Fetch.tryDelete<_, unit>("http://localhost:3000/delete/unit", null)
                let expected = Ok ()
                Assert.AreEqual(res, expected)
                d()
            } |> Promise.catch d
            |> Promise.start

    describe "Errors" <| fun _ ->
        it "A 404 should be reported as Bad Status" <| fun d ->
            promise {
                let! (Error (BadStatus res)) = Fetch.tryGet("http://localhost:3000/404")
                let expected = 404
                Assert.AreEqual(res.Status, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start
 
        it "A failing encoder should be detected" <| fun d ->
            promise {
                let data = { Id = 1; Name = "Alfonso" }
                let! response = Fetch.tryPatch ("http://localhost:3000/authors/1", data , extra = brokenAuthorCoder )
                let (Error(PreparingRequestFailed exn)) = response
                let expected = "Not Implemented"
                Assert.AreEqual (exn.Message, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "A network issue should be detected" <| fun d ->
            promise {
                let! (Error (NetworkError exn)) = Fetch.tryFetchAs("http://just.wrong")
                Assert.AreEqual (isNull exn, false)
                d()
            }
            |> Promise.catch d
            |> Promise.start