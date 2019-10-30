---
title: Thoth.Fetch
---

[[toc]]

## Usage

Thoth.Fetch provides an easy to use API for working with [Fable.Fetch](https://github.com/fable-compiler/fable-fetch) and [Thoth.Json](https://mangelmaxime.github.io/Thoth/json/v3.html). It supports both manual and auto coders depending on your preferences.

For each method, Thoth.Fetch provides a ***safe*** and ***unsafe*** version.

We call ***safe*** a method which returns a `Result<'T, string>`.

We call ***unsafe*** a method that will throw an exception when a coder fails.

List of "unsafe" methods:
- fetchAs
- get
- post
- put
- patch
- delete

List of "safe" methods:
- tryFetchAs
- tryGet
- tryPost
- tryPut
- tryPatch
- tryDelete

## Manual coders

### Example

#### Define your decoder

```fsharp
open Thoth.Fetch
open Thoth.Json
open System

/// Type representing our ressource
type Book =
    { Id : int
      Title : string
      Author : string
      CreatedAt : DateTime
      UpdatedAt : DateTime option }

    /// Transform a Book from JSON
    static member Decoder =
        Decode.object (fun get ->
            { Id = get.Required.Field "id" Decode.int
              Title = get.Required.Field "title" Decode.string
              Author = get.Required.Field "author" Decode.string
              CreatedAt = get.Required.Field "createdAt" Decode.datetime
              UpdatedAt = get.Optional.Field "updatedAt" Decode.datetime }
        )
```

#### GET request

```fsharp
let getBookById (id : int) =
    promise {
        let url = sprintf "http://localhost:8080/books/%i" id
        return! Fetch.get(url, Book.Decoder)
    }
```

#### POST request

```fsharp
let createBook (book : Book) =
    promise {
        let url = "http://localhost:8080/books/"
        let data =
            Encode.object [
                "title", Encode.string book.Title
                "author", Encode.string book.Author
                "createdAt", Encode.datetime book.CreatedAt
                "updatedAt", Encode.option Encode.datetime book.UpdatedAt
            ]
        return! Fetch.post(url, data, Book.Decoder)
    }
```

#### PUT request

```fsharp
let updateBook (book : Book) =
    promise {
        let url = "http://localhost:8080/books/"
        let data =
            Encode.object [
                "id", Encode.int book.Id
                "title", Encode.string book.Title
                "author", Encode.string book.Author
                "createdAt", Encode.datetime book.CreatedAt
                "updatedAt", Encode.option Encode.datetime book.UpdatedAt
            ]
        return! Fetch.put(url, data, Book.Decoder)
    }
```

#### DELETE request

```fsharp
let deleteBook (book : Book) =
    promise {
        let url = sprintf "http://localhost:8080/books/%i" book.Id
        // We need to pass `null` to send no data
        // Otherwise, F# compiler can't resolve the overload
        return! Fetch.delete(url, null, Decode.bool)
    }
```

## Auto coders

You need to help F# type inference to determine which type is expected.

Here are two ways to do it. There are other approaches but but these are simpler:

### Type via the promise result

```fsharp
let getBookById (id : int) : JS.Promise<Book> =
    promise {
        let url = sprintf "http://localhost:8080/books/%i" id
        return! Fetch.get(url, isCamelCase = true)
    }

let createBook (book : Book) : JS.Promise<Book> =
    promise {
        let url = "http://localhost:8080/books/"
        let data =
            {| title = book.Title
               author = book.Author
               createdAt = book.CreatedAt
               updatedAt = book.UpdatedAt |}

        return! Fetch.post(url, data, isCamelCase = true)
    }
```

### Type when calling Fetch method
```fsharp
let getBookById (id : int) =
    promise {
        let url = sprintf "http://localhost:8080/books/%i" id
        return! Fetch.get<Book>(url, isCamelCase = true)
    }

let createBook (book : Book) =
    promise {
        let url = "http://localhost:8080/books/"
        let data =
            {| title = book.Title
               author = book.Author
               createdAt = book.CreatedAt
               updatedAt = book.UpdatedAt |}

        return! Fetch.post<_, Book>(url, data, isCamelCase = true)
    }
```

### Example

#### Define your type

```fsharp
open Fable.Core
open Thoth.Fetch
open System

/// Type representing our ressource
type Book =
    { Id : int
      Title : string
      Author : string
      CreatedAt : DateTime
      UpdatedAt : DateTime option }
```

#### GET request

```fsharp
let getBookById (id : int) : JS.Promise<Book> =
    promise {
        let url = sprintf "http://localhost:8080/books/%i" id
        return! Fetch.get(url, isCamelCase = true)
    }
```

#### POST request

```fsharp
let createBook (book : Book) : JS.Promise<Book> =
    promise {
        let url = "http://localhost:8080/books/"
        let data =
            {| title = book.Title
               author = book.Author
               createdAt = book.CreatedAt
               updatedAt = book.UpdatedAt |}

        return! Fetch.post(url, data, isCamelCase = true)
    }
```

#### PUT request

```fsharp
let updateBook (book : Book) : JS.Promise<Book> =
    promise {
        let url = "http://localhost:8080/books/"
        let data =
            {| id = book.Id
               title = book.Title
               author = book.Author
               createdAt = book.CreatedAt
               updatedAt = book.UpdatedAt |}

        return! Fetch.put(url, data, isCamelCase = true)
    }
```

#### DELETE request

```fsharp
let deleteBook (book : Book) : JS.Promise<bool> =
    promise {
        let url = sprintf "http://localhost:8080/books/%i" book.Id
        // We need to pass `null` to send no data
        // Otherwise, F# compiler can't resolve the overload
        return! Fetch.delete(url, null)
    }
```

### Keep control over Thoth.Json

When using auto coders, you can pass `isCamelCase` and/or `extra` arguments in order to control `Thoth.Json` behaviour. You can learn more about them by reading `Thoth.Json` [documentation](https://mangelmaxime.github.io/Thoth/json/v3.html).
