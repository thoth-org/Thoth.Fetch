module Tests.Main

open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.Testing
open Thoth.Fetch
open Thoth.Json
open Node
open System

type FakeDeleteResponse =
    { IsSuccess : bool }

    static member Decoder =
        Decode.object (fun get ->
            { IsSuccess = get.Required.Field "isSuccess" Decode.bool}
        )

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

type Author =
    { Id : int
      Name : string }

type Database =
    { Books : Book list
      Authors : Author list }


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

[<Import("default", "./configure-server.js")>]
let configureServer (_server : obj) : unit = jsNative

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
        configureServer server

        server?``use``(jsonServer?router(dbFile))
        serverInstance <- server?listen(3000, !!ignore)

    after <| fun _ ->
        serverInstance?close()

    // End of the set up

    describe "Fetch.fetchAs" <| fun _ ->

        it "Fetch.fetchAs works with manual decoder" <| fun d ->
            promise {
                let! res = Fetch.fetchAs("http://localhost:3000/books/1", Book.Decoder)
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

        it "Fetch.fetchAs works with auto decoder" <| fun d ->
            promise {
                let! res = Fetch.fetchAs<Book>("http://localhost:3000/books/1", isCamelCase = true)
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

        it "Fetch.fetchAs throw an exception explaining why the manual decoder failed" <| fun d ->
            promise {
                let! _ = Fetch.fetchAs("http://localhost:3000/authors/1", Book.Decoder)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
I run into the following problems:

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
                let! _ = Fetch.fetchAs<Book>("http://localhost:3000/authors/1")
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

    describe "Fetch.fetchUnit" <| fun _ ->

        it "Fetch.fetchUnit works with no body response" <| fun d ->
            promise {
                let! res = Fetch.fetchUnit ("http://localhost:3000/get/unit-via-no-body")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.fetchUnit works with no body response and non default status code" <| fun d ->
            promise {
                let! res = Fetch.fetchUnit ("http://localhost:3000/get/unit-via-no-body-and-non-default-status-code")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.fetchUnit returns an error if a body is found" <| fun d ->
            promise {
                let! _ = Fetch.fetchUnit ("http://localhost:3000/books/1")
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for this request"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryFetchAs" <| fun _ ->
        it "Fetch.tryFetchAs works with manual decoder" <| fun d ->
            promise {
                let! res = Fetch.tryFetchAs("http://localhost:3000/books/1", Book.Decoder)
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
                let! res = Fetch.tryFetchAs<Book>("http://localhost:3000/books/1")
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

        it "Fetch.tryFetchAs returns an error explaining why the manual decoder failed" <| fun d ->
            promise {
                let! res = Fetch.tryFetchAs("http://localhost:3000/authors/1", Book.Decoder)
                let expected =
                    Error(
                        """
I run into the following problems:

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
                    )
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryFetchAs returns an error explaining why the auto decoder failed" <| fun d ->
            promise {
                let! res = Fetch.tryFetchAs<Book>("http://localhost:3000/authors/1")
                let expected =
                    Error(
                        """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    )
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryFetchUnit" <| fun _ ->

        it "Fetch.tryFetchUnit works with no body response" <| fun d ->
            promise {
                let! res = Fetch.tryFetchUnit ("http://localhost:3000/get/unit-via-no-body")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryFetchUnit works with no body response and non default status code" <| fun d ->
            promise {
                let! res = Fetch.tryFetchUnit ("http://localhost:3000/get/unit-via-no-body-and-non-default-status-code")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryFetchUnit returns an error if a body is found" <| fun d ->
            promise {
                let! res = Fetch.tryFetchUnit ("http://localhost:3000/books/1")
                let expected = Error "No body expected for this request"

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.getAs" <| fun _ ->
        it "Fetch.getAs works with manual decoder" <| fun d ->
            promise {
                let! res = Fetch.getAs("http://localhost:3000/books/1", Book.Decoder)
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

        it "Fetch.getAs works with auto decoder" <| fun d ->
            promise {
                let! res = Fetch.getAs<Book>("http://localhost:3000/books/1", isCamelCase = true)
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

        it "Fetch.getAs throw an exception explaining why the manual decoder failed" <| fun d ->
            promise {
                let! _ = Fetch.getAs("http://localhost:3000/authors/1", Book.Decoder)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
I run into the following problems:

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

        it "Fetch.getAs throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let! _ = Fetch.getAs<Book>("http://localhost:3000/authors/1")
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

    describe "Fetch.get" <| fun _ ->

        it "Fetch.get works with no body response" <| fun d ->
            promise {
                let! res = Fetch.get ("http://localhost:3000/get/unit-via-no-body")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.get works with no body response and non default status code" <| fun d ->
            promise {
                let! res = Fetch.get ("http://localhost:3000/get/unit-via-no-body-and-non-default-status-code")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.get returns an error if a body is found" <| fun d ->
            promise {
                let! _ = Fetch.get ("http://localhost:3000/books/1")
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.get` request. If you expect a body to decode, please use `Fetch.getAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryGetAs" <| fun _ ->
        it "Fetch.tryGetAs works with manual decoder" <| fun d ->
            promise {
                let! res = Fetch.tryGetAs("http://localhost:3000/books/1", Book.Decoder)
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

        it "Fetch.tryGetAs works with auto decoder" <| fun d ->
            promise {
                let! res = Fetch.tryGetAs<Book>("http://localhost:3000/books/1")
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

        it "Fetch.tryGetAs returns an error explaining why the manual decoder failed" <| fun d ->
            promise {
                let! res = Fetch.tryGetAs("http://localhost:3000/authors/1", Book.Decoder)
                let expected =
                    Error(
                        """
I run into the following problems:

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
                    )
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryGetAs returns an error explaining why the auto decoder failed" <| fun d ->
            promise {
                let! res = Fetch.tryGetAs<Book>("http://localhost:3000/authors/1")
                let expected =
                    Error(
                        """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    )
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryGet" <| fun _ ->

        it "Fetch.tryGet works with no body response" <| fun d ->
            promise {
                let! res = Fetch.tryGet ("http://localhost:3000/get/unit-via-no-body")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryGet works with no body response and non default status code" <| fun d ->
            promise {
                let! res = Fetch.tryGet ("http://localhost:3000/get/unit-via-no-body-and-non-default-status-code")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryGet returns an error if a body is found" <| fun d ->
            promise {
                let! res = Fetch.tryGet ("http://localhost:3000/books/1")
                let expected = Error "No body expected for `Fetch.tryGet` request. If you expect a body to decode, please use `Fetch.tryGetAs`"

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.postAs" <| fun _ ->
        it "Fetch.postAs works with manual coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let data =
                    Encode.object [
                        "title", Encode.string "The Amulet of Samarkand"
                        "author", Encode.string "Jonathan Stroud"
                        "createdAt", Encode.datetime now
                    ]
                let! res = Fetch.postAs("http://localhost:3000/books", data, Book.Decoder)
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

        it "Fetch.postAs works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                        createdAt = now
                    |}

                let! res = Fetch.postAs<_, Book>("http://localhost:3000/books", data)
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

        it "Fetch.postAs throw an exception explaining why the manual decoder failed" <| fun d ->
            promise {
                let data =
                    Encode.object [
                        "name", Encode.string "Brandon Sanderson"
                    ]
                let! _ = Fetch.postAs("http://localhost:3000/authors", data, Book.Decoder)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
I run into the following problems:

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

        it "Fetch.postAs throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! _ = Fetch.postAs<_, Book>("http://localhost:3000/authors", data)
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

        // it "Fetch.postAs works with unit response" <| fun d ->
        //     promise {
        //         let! res = Fetch.postAs<_, unit>("http://localhost:3000/post/unit", null)
        //         Assert.AreEqual(res, ())
        //         d()
        //     } |> Promise.catch d
        //     |> Promise.start

    describe "Fetch.post" <| fun _ ->

        it "Fetch.post works with no body response and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.post("http://localhost:3000/post/unit-via-no-body", Encode.string "some value")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.post works with no body response and non default status code and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.post("http://localhost:3000/post/unit-via-no-body-and-non-default-status-code", Encode.string "some value")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.post with a manual encoder returns an error if a body is found" <| fun d ->
            promise {
                let! _ = Fetch.post("http://localhost:3000/post/unit-via-no-body", Encode.string "some value")
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.post` request. If you expect a body to decode, please use `Fetch.postAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.post works with no body response and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.post("http://localhost:3000/post/unit-via-no-body", data)
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.post works with no body response and non default status code and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.post("http://localhost:3000/post/unit-via-no-body-and-non-default-status-code", data)
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.post with a auto encoder returns an error if a body is found" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! _ = Fetch.post("http://localhost:3000/post/unit-via-no-body", data)
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.post` request. If you expect a body to decode, please use `Fetch.postAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryPostAs" <| fun _ ->
        it "Fetch.tryPostAs works with manual coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let data =
                    Encode.object [
                        "title", Encode.string "The Amulet of Samarkand"
                        "author", Encode.string "Jonathan Stroud"
                        "createdAt", Encode.datetime now
                    ]
                let! res = Fetch.tryPostAs("http://localhost:3000/books", data, Book.Decoder)
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

        it "Fetch.tryPostAs works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                        createdAt = now
                    |}

                let! res = Fetch.tryPostAs<_, Book>("http://localhost:3000/books", data)
                let expected =
                    Ok { Id = 6
                         Title = "The Golem's Eye"
                         Author = "Jonathan Stroud"
                         CreatedAt = now
                         UpdatedAt = None }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPostAs throw an exception explaining why the manual decoder failed" <| fun d ->
            promise {
                let data =
                    Encode.object [
                        "name", Encode.string "Brandon Sanderson"
                    ]
                let! res = Fetch.tryPostAs("http://localhost:3000/authors", data, Book.Decoder)
                let expected =
                    Error(
                        """
I run into the following problems:

Error at: `$`
Expecting an object with a field named `title` but instead got:
{
    "name": "Brandon Sanderson",
    "id": 5
}

Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "name": "Brandon Sanderson",
    "id": 5
}

Error at: `$`
Expecting an object with a field named `createdAt` but instead got:
{
    "name": "Brandon Sanderson",
    "id": 5
}
                        """.Trim()
                    )
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPostAs throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! res = Fetch.tryPostAs<_, Book>("http://localhost:3000/authors", data)

                let expected =
                    Error(
                        """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    )

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        // it "Fetch.tryPostAs works with unit response" <| fun d ->
        //     promise {
        //         let! res = Fetch.tryPostAs<_, unit>("http://localhost:3000/post/unit", null)
        //         let expected = Ok ()
        //         Assert.AreEqual(res, expected)
        //         d()
        //     } |> Promise.catch d
        //     |> Promise.start

    describe "Fetch.tryPost" <| fun _ ->

        it "Fetch.tryPost works with no body response and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.tryPost("http://localhost:3000/post/unit-via-no-body", Encode.string "some value")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPost works with no body response and non default status code and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.tryPost("http://localhost:3000/post/unit-via-no-body-and-non-default-status-code", Encode.string "some value")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPost with a manual encoder returns an error if a body is found" <| fun d ->
            promise {
                let! _ = Fetch.tryPost("http://localhost:3000/post/unit-via-no-body", Encode.string "some value")
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.tryPost` request. If you expect a body to decode, please use `Fetch.tryPostAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPost works with no body response and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.tryPost("http://localhost:3000/post/unit-via-no-body", data)
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPost works with no body response and non default status code and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.tryPost("http://localhost:3000/post/unit-via-no-body-and-non-default-status-code", data)
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPost with a auto encoder returns an error if a body is found" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! _ = Fetch.tryPost("http://localhost:3000/post/unit-via-no-body", data)
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.tryPost` request. If you expect a body to decode, please use `Fetch.tryPostAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.putAs" <| fun _ ->
        it "Fetch.putAs works with manual coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs<Book>("http://localhost:3000/books/3")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.putAs("http://localhost:3000/books/3", Book.Encoder updatedBook, Book.Decoder)

                Assert.AreEqual(res, updatedBook)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.putAs works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs<Book>("http://localhost:3000/books/4")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.putAs("http://localhost:3000/books/4", updatedBook, isCamelCase = true)

                Assert.AreEqual(res, updatedBook)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.putAs throw an exception explaining why the manual decoder failed" <| fun d ->
            promise {
                let data =
                    Encode.object [
                        "name", Encode.string "Brandon Sanderson"
                    ]
                let! _ = Fetch.putAs("http://localhost:3000/authors/1", data, Book.Decoder)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
I run into the following problems:

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

        it "Fetch.putAs throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! _ = Fetch.putAs<_, Book>("http://localhost:3000/authors/1", data)
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

        // it "Fetch.putAs works with unit response" <| fun d ->
        //     promise {
        //         let! res = Fetch.putAs<_, unit>("http://localhost:3000/put/unit", null)
        //         Assert.AreEqual(res, ())
        //         d()
        //     } |> Promise.catch d
        //     |> Promise.start

    describe "Fetch.put" <| fun _ ->

        it "Fetch.put works with no body response and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.put("http://localhost:3000/put/unit-via-no-body", Encode.string "some value")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.put works with no body response and non default status code and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.put("http://localhost:3000/put/unit-via-no-body-and-non-default-status-code", Encode.string "some value")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.put with a manual encoder returns an error if a body is found" <| fun d ->
            promise {
                let! _ = Fetch.put("http://localhost:3000/put/unit-via-no-body", Encode.string "some value")
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.put` request. If you expect a body to decode, please use `Fetch.putAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.put works with no body response and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.put("http://localhost:3000/put/unit-via-no-body", data)
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.put works with no body response and non default status code and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.put("http://localhost:3000/put/unit-via-no-body-and-non-default-status-code", data)
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.put with a auto encoder returns an error if a body is found" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! _ = Fetch.put("http://localhost:3000/put/unit-via-no-body", data)
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.put` request. If you expect a body to decode, please use `Fetch.putAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryPutAs" <| fun _ ->
        it "Fetch.tryPutAs works with manual coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs<Book>("http://localhost:3000/books/5")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.tryPutAs("http://localhost:3000/books/5", Book.Encoder updatedBook, Book.Decoder)
                let expected = Ok updatedBook

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPutAs works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs<Book>("http://localhost:3000/books/6")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.tryPutAs("http://localhost:3000/books/6", updatedBook, isCamelCase = true)
                let expected = Ok updatedBook

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPutAs throw an exception explaining why the manual decoder failed" <| fun d ->
            promise {
                let data =
                    Encode.object [
                        "name", Encode.string "Brandon Sanderson"
                    ]
                let! res = Fetch.tryPutAs("http://localhost:3000/authors/1", data, Book.Decoder)
                let expected =
                    Error(
                        """
I run into the following problems:

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
                    )
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPutAs throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! res = Fetch.tryPutAs<_, Book>("http://localhost:3000/authors/1", data)

                let expected =
                    Error(
                        """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    )

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        // it "Fetch.tryPutAs works with unit response" <| fun d ->
        //     promise {
        //         let! res = Fetch.tryPutAs<_, unit>("http://localhost:3000/put/unit", null)
        //         let expected = Ok ()
        //         Assert.AreEqual(res, expected)
        //         d()
        //     } |> Promise.catch d
        //     |> Promise.start


    describe "Fetch.tryPut" <| fun _ ->

        it "Fetch.tryPut works with no body response and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.tryPut("http://localhost:3000/put/unit-via-no-body", Encode.string "some value")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPut works with no body response and non default status code and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.tryPut("http://localhost:3000/put/unit-via-no-body-and-non-default-status-code", Encode.string "some value")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPut with a manual encoder returns an error if a body is found" <| fun d ->
            promise {
                let! _ = Fetch.tryPut("http://localhost:3000/put/unit-via-no-body", Encode.string "some value")
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.tryPut` request. If you expect a body to decode, please use `Fetch.tryPutAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPut works with no body response and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.tryPut("http://localhost:3000/put/unit-via-no-body", data)
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPut works with no body response and non default status code and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.tryPut("http://localhost:3000/put/unit-via-no-body-and-non-default-status-code", data)
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPut with a auto encoder returns an error if a body is found" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! _ = Fetch.tryPut("http://localhost:3000/put/unit-via-no-body", data)
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.tryPut` request. If you expect a body to decode, please use `Fetch.tryPutAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start


    describe "Fetch.patchAs" <| fun _ ->
        it "Fetch.patchAs works with manual coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs<Book>("http://localhost:3000/books/3")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.patchAs("http://localhost:3000/books/3", Book.Encoder updatedBook, Book.Decoder)

                Assert.AreEqual(res, updatedBook)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.patchAs works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs<Book>("http://localhost:3000/books/4")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.patchAs("http://localhost:3000/books/4", updatedBook, isCamelCase = true)

                Assert.AreEqual(res, updatedBook)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.patchAs throw an exception explaining why the manual decoder failed" <| fun d ->
            promise {
                let data =
                    Encode.object [
                        "name", Encode.string "Brandon Sanderson"
                    ]
                let! _ = Fetch.patchAs("http://localhost:3000/authors/1", data, Book.Decoder)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
I run into the following problems:

Error at: `$`
Expecting an object with a field named `title` but instead got:
{
    "id": 1,
    "name": "Brandon Sanderson"
}

Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "id": 1,
    "name": "Brandon Sanderson"
}

Error at: `$`
Expecting an object with a field named `createdAt` but instead got:
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

        it "Fetch.patchAs throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! _ = Fetch.patchAs<_, Book>("http://localhost:3000/authors/1", data)
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

        // it "Fetch.patchAs works with unit response" <| fun d ->
        //     promise {
        //         let! res = Fetch.patchAs<_, unit>("http://localhost:3000/patch/unit", null)
        //         Assert.AreEqual(res, ())
        //         d()
        //     } |> Promise.catch d
        //     |> Promise.start

    describe "Fetch.patch" <| fun _ ->

        it "Fetch.patch works with no body response and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.patch("http://localhost:3000/patch/unit-via-no-body", Encode.string "some value")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.patch works with no body response and non default status code and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.patch("http://localhost:3000/patch/unit-via-no-body-and-non-default-status-code", Encode.string "some value")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.patch with a manual encoder returns an error if a body is found" <| fun d ->
            promise {
                let! _ = Fetch.patch("http://localhost:3000/patch/unit-via-no-body", Encode.string "some value")
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.patch` request. If you expect a body to decode, please use `Fetch.patchAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.patch works with no body response and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.patch("http://localhost:3000/patch/unit-via-no-body", data)
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.patch works with no body response and non default status code and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.patch("http://localhost:3000/patch/unit-via-no-body-and-non-default-status-code", data)
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.patch with a auto encoder returns an error if a body is found" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! _ = Fetch.patch("http://localhost:3000/patch/unit-via-no-body", data)
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.patch` request. If you expect a body to decode, please use `Fetch.patchAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryPatchAs" <| fun _ ->
        it "Fetch.tryPatchAs works with manual coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs<Book>("http://localhost:3000/books/5")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.tryPatchAs("http://localhost:3000/books/5", Book.Encoder updatedBook, Book.Decoder)
                let expected = Ok updatedBook

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatchAs works with auto coder" <| fun d ->
            promise {
                let now = DateTime.UtcNow
                let! originalBook = Fetch.fetchAs<Book>("http://localhost:3000/books/6")
                let updatedBook =
                    { originalBook with UpdatedAt = Some now }

                let! res = Fetch.tryPatchAs("http://localhost:3000/books/6", updatedBook, isCamelCase = true)
                let expected = Ok updatedBook

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatchAs throw an exception explaining why the manual decoder failed" <| fun d ->
            promise {
                let data =
                    Encode.object [
                        "name", Encode.string "Brandon Sanderson"
                    ]
                let! res = Fetch.tryPatchAs("http://localhost:3000/authors/1", data, Book.Decoder)
                let expected =
                    Error(
                        """
I run into the following problems:

Error at: `$`
Expecting an object with a field named `title` but instead got:
{
    "id": 1,
    "name": "Brandon Sanderson"
}

Error at: `$`
Expecting an object with a field named `author` but instead got:
{
    "id": 1,
    "name": "Brandon Sanderson"
}

Error at: `$`
Expecting an object with a field named `createdAt` but instead got:
{
    "id": 1,
    "name": "Brandon Sanderson"
}
                        """.Trim()
                    )
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatchAs throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let data = {| name = "Brandon Sanderson" |}
                let! res = Fetch.tryPatchAs<_, Book>("http://localhost:3000/authors/1", data)

                let expected =
                    Error(
                        """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    )

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        // it "Fetch.tryPatchAs works with unit response" <| fun d ->
        //     promise {
        //         let! res = Fetch.tryPatchAs<_, unit>("http://localhost:3000/patch/unit", null)
        //         let expected = Ok ()
        //         Assert.AreEqual(res, expected)
        //         d()
        //     } |> Promise.catch d
        //     |> Promise.start

    describe "Fetch.tryPatch" <| fun _ ->

        it "Fetch.tryPatch works with no body response and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.tryPatch("http://localhost:3000/patch/unit-via-no-body", Encode.string "some value")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatch works with no body response and non default status code and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.tryPatch("http://localhost:3000/patch/unit-via-no-body-and-non-default-status-code", Encode.string "some value")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatch with a manual encoder returns an error if a body is found" <| fun d ->
            promise {
                let! _ = Fetch.tryPatch("http://localhost:3000/patch/unit-via-no-body", Encode.string "some value")
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.tryPatch` request. If you expect a body to decode, please use `Fetch.tryPatchAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatch works with no body response and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.tryPatch("http://localhost:3000/patch/unit-via-no-body", data)
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatch works with no body response and non default status code and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.tryPatch("http://localhost:3000/patch/unit-via-no-body-and-non-default-status-code", data)
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryPatch with a auto encoder returns an error if a body is found" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! _ = Fetch.tryPatch("http://localhost:3000/patch/unit-via-no-body", data)
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.tryPatch` request. If you expect a body to decode, please use `Fetch.tryPatchAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start


    describe "Fetch.deleteAs" <| fun _ ->
        it "Fetch.deleteAs works with manual coder" <| fun d ->
            promise {
                let! res = Fetch.deleteAs("http://localhost:3000/fake-delete", null, FakeDeleteResponse.Decoder)
                let expected =
                    { IsSuccess = true }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.deleteAs works with auto coder" <| fun d ->
            promise {
                let! res = Fetch.deleteAs<_, FakeDeleteResponse>("http://localhost:3000/fake-delete", null, isCamelCase = true)
                let expected =
                    { IsSuccess = true }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.deleteAs throw an exception explaining why the manual decoder failed" <| fun d ->
            promise {
                let! _ = Fetch.deleteAs("http://localhost:3000/fake-delete", null, Book.Decoder)
                d()
            }
            |> Promise.catch (fun error ->
                let expected =
                    """
I run into the following problems:

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

        it "Fetch.deleteAs throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let! _ = Fetch.deleteAs<_, Book>("http://localhost:3000/fake-delete", null)
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

        // it "Fetch.deleteAs works with unit response" <| fun d ->
        //     promise {
        //         let! res = Fetch.deleteAs<_, unit>("http://localhost:3000/delete/unit", null)
        //         Assert.AreEqual(res, ())
        //         d()
        //     } |> Promise.catch d
        //     |> Promise.start

    describe "Fetch.delete" <| fun _ ->

        it "Fetch.delete works with no body response and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.delete("http://localhost:3000/delete/unit-via-no-body", Encode.string "some value")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.delete works with no body response and non default status code and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.delete("http://localhost:3000/delete/unit-via-no-body-and-non-default-status-code", Encode.string "some value")
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.delete with a manual encoder returns an error if a body is found" <| fun d ->
            promise {
                let! _ = Fetch.delete("http://localhost:3000/delete/unit-via-no-body", Encode.string "some value")
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.delete` request. If you expect a body to decode, please use `Fetch.deleteAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.delete works with no body response and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.delete("http://localhost:3000/delete/unit-via-no-body", data)
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.delete works with no body response and non default status code and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.delete("http://localhost:3000/delete/unit-via-no-body-and-non-default-status-code", data)
                Assert.AreEqual(res, ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.delete with a auto encoder returns an error if a body is found" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! _ = Fetch.delete("http://localhost:3000/delete/unit-via-no-body", data)
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.delete` request. If you expect a body to decode, please use `Fetch.deleteAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

    describe "Fetch.tryDeleteAs" <| fun _ ->
        it "Fetch.tryDeleteAs works with manual coder" <| fun d ->
            promise {
                let! res = Fetch.tryDeleteAs("http://localhost:3000/fake-delete", null, FakeDeleteResponse.Decoder)
                let expected =
                    Ok { IsSuccess = true }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryDeleteAs works with auto coder" <| fun d ->
            promise {
                let! res = Fetch.tryDeleteAs<_, FakeDeleteResponse>("http://localhost:3000/fake-delete", null, isCamelCase = true)
                let expected =
                    Ok { IsSuccess = true }

                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryDeleteAs throw an exception explaining why the manual decoder failed" <| fun d ->
            promise {
                let! res = Fetch.tryDeleteAs("http://localhost:3000/fake-delete", null, Book.Decoder)
                let expected =
                    Error(
                        """
I run into the following problems:

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
                    )
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryDeleteAs throw an exception explaining why the auto decoder failed" <| fun d ->
            promise {
                let! res = Fetch.tryDeleteAs<_, Book>("http://localhost:3000/fake-delete", null)
                let expected =
                    Error(
                        """
Error at: `$.createdAt`
Expecting a datetime but instead got: undefined
                        """.Trim()
                    )
                Assert.AreEqual(res, expected)
                d()
            }
            |> Promise.catch d
            |> Promise.start

        // it "Fetch.tryDeleteAs works with unit response" <| fun d ->
        //     promise {
        //         let! res = Fetch.tryDeleteAs<_, unit>("http://localhost:3000/delete/unit", null)
        //         let expected = Ok ()
        //         Assert.AreEqual(res, expected)
        //         d()
        //     } |> Promise.catch d
        //     |> Promise.start

    describe "Fetch.tryDelete" <| fun _ ->

        it "Fetch.tryDelete works with no body response and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.tryDelete("http://localhost:3000/delete/unit-via-no-body", Encode.string "some value")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryDelete works with no body response and non default status code and manual encoder" <| fun d ->
            promise {
                let! res = Fetch.tryDelete("http://localhost:3000/delete/unit-via-no-body-and-non-default-status-code", Encode.string "some value")
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryDelete with a manual encoder returns an error if a body is found" <| fun d ->
            promise {
                let! _ = Fetch.tryDelete("http://localhost:3000/delete/unit-via-no-body", Encode.string "some value")
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.tryDelete` request. If you expect a body to decode, please use `Fetch.tryDeleteAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryDelete works with no body response and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.tryDelete("http://localhost:3000/delete/unit-via-no-body", data)
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryDelete works with no body response and non default status code and auto encoder" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! res = Fetch.tryDelete("http://localhost:3000/delete/unit-via-no-body-and-non-default-status-code", data)
                Assert.AreEqual(res, Ok ())
                d()
            }
            |> Promise.catch d
            |> Promise.start

        it "Fetch.tryDelete with a auto encoder returns an error if a body is found" <| fun d ->
            promise {
                let data =
                    {|
                        title = "The Golem's Eye"
                        author = "Jonathan Stroud"
                    |}
                let! _ = Fetch.tryDelete("http://localhost:3000/delete/unit-via-no-body", data)
                d()
            }
            |> Promise.catch (fun error ->
                let expected = "No body expected for `Fetch.tryDelete` request. If you expect a body to decode, please use `Fetch.tryDeleteAs`"
                Assert.AreEqual(error.Message, expected)
                d()
            )
            |> Promise.catch d
            |> Promise.start
