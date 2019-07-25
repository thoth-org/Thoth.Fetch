module TestLibrary

// open System.Threading

// /// <summary>
// ///  This class performs an important function.
// /// </summary>
// let testSummary = ""


// /// <summary><c>DoWork</c> is a method in the <c>TestClass</c> class.
// /// </summary>
// let testInlinedCode = ""

// /// <summary>
// /// <code lang="fsharp">
// /// let markdownDemo (arg1 : string) (arg2 : string) =
// ///     ""
// ///
// /// type Alias = int
// ///
// /// type Alpha = class end
// /// </code>
// /// </summary>
// let testMultilineCode = ""

// /// <code lang="fsharp">
// /// type LightDU =
// ///     | CaseA
// ///     | CaseB
// /// </code>
// let testMultilineCodeWithLang = "" // Not a standard XML doc comment feature but why not ;)

// /// <code lang="js">
// /// export default function (req, res) {
// ///     res.jsonp({
// ///         isSuccess : true
// ///     });
// /// }
// /// </code>
// let testMultilineCodeWithLang2 = "" // Not a standard XML doc comment feature but why not ;)

// /// <summary>
// /// This sample shows how to specify the <see cref="Fetch"/> constructor as a cref attribute.
// /// </summary>
// /// <example>
// /// This sample shows how to call the <see cref="GetZero"/> method.
// /// <code lang="csharp">
// /// class TestClass
// /// {
// ///     static int Main()
// ///     {
// ///         return GetZero();
// ///     }
// /// }
// /// </code>
// /// </example>
// /// <example>
// /// This sample shows how to call the <see cref="GetZero"/> method.
// /// <code lang="fsharp">
// /// let markdownDemo (arg1 : string) (arg2 : string) =
// ///     ""
// ///
// /// type Alias = int
// ///
// /// type Alpha = class end
// /// </code>
// /// </example>
// let testExample = ""

// /// <exception cref="System.Exception">Thrown when...</exception>
// /// <exception cref="System.Exception">Thrown when...</exception>
// /// <exception cref="System.Exception">Thrown when...</exception>
// let testExceptions = ""

// /// <summary>Here is an example of a bulleted list:
// /// <list type="bullet">
// /// <item>
// /// <description>Item 1.</description>
// /// </item>
// /// <item>
// /// <description>Item 2.</description>
// /// </item>
// /// </list>
// /// </summary>
// let testList = ""

// /// <summary>
// /// <para>
// /// This should be a new paragraph
// /// </para>
// /// <para>
// /// This should be a new paragraph
// /// </para>
// /// <para>
// /// This should be a new paragraph
// /// </para>
// /// </summary>
// let testParagraphs = ""


// /// <param name="url">URL to request</param>
// /// <param name="data">Data sent via the body, it will be converted to JSON before</param>
// /// <param name="properties">Parameters passed to fetch</param>
// /// <param name="isCamelCase">Options passed to Thoth.Json to control JSON keys representation</param>
// /// <param name="extra">Options passed to Thoth.Json to extends the known coders</param>
// /// <param name="responseResolver">Used by Fable to provide generic type info</param>
// /// <param name="dataResolver">Used by Fable to provide generic type info</param>
// let testParameters = ""

// /// <summary>
// /// The <paramref name="url"/> parameter takes a string.
// /// </summary>
// let testParamRef = ""

// /// <summary>
// /// Some summary thing
// /// </summary>
// /// <remarks>
// /// You may have some additional information about this class.
// /// </remarks>
// let testRemark = ""

// /// <summary>
// /// <para>Here's how you could make a second paragraph in a description. <see cref="System.Console.WriteLine(System.String)"/> for information about output statements.</para>
// /// <seealso cref="System.String"/>
// /// <seealso cref="System.String"/>
// /// <seealso cref="System.String"/>
// /// </summary>
// /// <typeparam name="Data">Type of the data to serialize to JSON in the body</typeparam>
// /// <typeparam name="Response">Type of the response</typeparam>
// let testTypeParam = ""

// /// <summary>
// /// Creates a new array of arbitrary type <typeparamref name="Response"/>
// /// </summary>
// /// <summary>The Name property represents the employee's name.</summary>
// /// <value>The Name property gets/sets the value of the string field, _name.</value>
// let test = ""

// /// <returns>Returns a <c>JS.Promise&lt;Result&lt;'Response,string&gt;&gt;</c></returns>
// let testReturns = ""


// /// <summary>
// /// Asynchronously reads the next JSON token from the source.
// /// </summary>
// /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
// /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read. The <see cref="Task{TResult}.Result"/>
// /// property returns <c>true</c> if the next token was read successfully; <c>false</c> if there are no more tokens to read.</returns>
// /// <remarks>The default behaviour is to execute synchronously, returning an already-completed task. Derived
// /// classes can override this behaviour for true asynchronicity.</remarks>
// let ReadAsync (cancellationToken : CancellationToken) = ""

// type ITypeResolver<'T> = class end


/// <summary>
/// This sample shows how to specify the <see cref="Fetch"/> constructor as a cref attribute.
/// <para>
/// This is a paragraph, with some <c>inlined code</c>
/// </para>
/// <para>
/// This is another paragraph, with a block code
/// <code>
/// type LightDU =
///     | CaseA
///     | CaseB
/// </code>
/// </para>
/// </summary>
/// <remarks>This a remark blocks because sometimes the summary isn't enought:
/// <para>
/// This is another paragraph, with a block code with synthax highlights
/// <code lang="fsharp">
/// type LightDU =
///     | CaseA
///     | CaseB
/// </code>
/// </para>
/// <para>
/// Demonstrate paramref: <paramref name="url"/> parameter takes a string.
/// </para>
/// <para>
/// Demonstrate typeparamref: Creates a new array of arbitrary type <typeparamref name="Response"/>
/// </para>
/// <para><a href="http://www.google.com">Click me</a></para>
/// </remarks>
/// <param name="url">URL to request</param>
/// <param name="data">Data sent via the body, it will be converted to JSON before</param>
/// <param name="properties">Parameters passed to fetch</param>
/// <param name="isCamelCase">Options passed to Thoth.Json to control JSON keys representation</param>
/// <param name="extra">Options passed to Thoth.Json to extends the known coders</param>
/// <param name="responseResolver">Used by Fable to provide generic type info</param>
/// <param name="dataResolver">Used by Fable to provide generic type info</param>
/// <typeparam name="Data">Type of the data to serialize to JSON in the body</typeparam>
/// <typeparam name="Response">Type of the response</typeparam>
/// <exception cref="System.Exception">Thrown when...</exception>
/// <exception cref="System.Exception2">Thrown when...</exception>
/// <example>
/// This sample shows how to call the <see cref="GetZero"/> method.
/// <code lang="csharp">
/// class TestClass
/// {
///     static int Main()
///     {
///         return GetZero();
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="System.String"/>
/// <seealso cref="System.Boolean"/>
///
///
/// <returns>
/// <table><thead><tr><th> Value
///  </th><th> Meaning
///  </th></tr></thead><tbody><tr><td> Less than zero
///  </td><td><code data-dev-comment-type="paramref">uri1</code> is less than <code data-dev-comment-type="paramref">uri2</code>.
///  </td></tr><tr><td> Zero
///  </td><td><code data-dev-comment-type="paramref">uri1</code> equals <code data-dev-comment-type="paramref">uri2</code>.
///  </td></tr><tr><td> Greater than zero
///  </td><td><code data-dev-comment-type="paramref">uri1</code> is greater than <code data-dev-comment-type="paramref">uri2</code>.
///  </td></tr></tbody></table>
///
/// <para> This is a paragrah in <c>returns</c> tag</para>
///
/// <para>
/// <table><thead><tr><th> Value
///  </th><th> Meaning
///  </th></tr></thead><tbody><tr><td> Less than zero
///  </td><td><code data-dev-comment-type="paramref">uri1</code> is less than <code data-dev-comment-type="paramref">uri2</code>.
///  </td></tr><tr><td> Zero
///  </td><td><code data-dev-comment-type="paramref">uri1</code> equals <code data-dev-comment-type="paramref">uri2</code>.
///  </td></tr><tr><td> Greater than zero
///  </td><td><code data-dev-comment-type="paramref">uri1</code> is greater than <code data-dev-comment-type="paramref">uri2</code>.
///  </td></tr></tbody></table>
/// </para>
/// </returns>
///
let testEverything =  ""
