**Description**

Initializes a new instance of the `System.Uri` class with the specified URI.

**Parameters**

* `uriString`: A URI.

**Exceptions**

* `System.ArgumentNullException`: `uriString` is null.
* `System.UriFormatException`:
>
>
> In the [.NET for Windows Store apps](http://go.microsoft.com/fwlink/?LinkID=247912) or the [Portable Class Library](https://docs.microsoft.com/en-gb/dotnet/standard/cross-platform/cross-platform-development-with-the-portable-class-library), catch the base class exception, `System.FormatException`, instead.
>
>
> `uriString` is empty.
>
> *or*
>
>   > The scheme specified in `uriString` is not correctly formed. See `System.Uri.CheckSchemeName(System.String)`.
>
> *or*
>
>   > `uriString` contains too many slashes.
>
> *or*
>
>   > The password specified in `uriString` is not valid.
>
> *or*
>
>   > The host name specified in `uriString` is not valid.
>
> *or*
>
>   > The file name specified in `uriString` is not valid.
>
> *or*
>
>   > The user name specified in `uriString` is not valid.
>
> *or*
>
>   > The host or authority name specified in `uriString` cannot be terminated by backslashes.
>
> *or*
>
>   > The port number specified in `uriString` is not valid or cannot be parsed.
>
> *or*
>
>   > The length of `uriString` exceeds 65519 characters.
>
> *or*
>
> * The length of the scheme specified in `uriString` exceeds 1023 characters.
> *or*
> There is an invalid character sequence in `uriString`.
> *or*
> The MS-DOS path specified in `uriString` must start with c:\\.
>

**Type Description**

Provides an object representation of a uniform resource identifier (URI) and easy access to the parts of the URI.
