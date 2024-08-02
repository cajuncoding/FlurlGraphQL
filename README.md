# FlurlGraphQL
`FlurlGraphQL`` is a lightweight, simplified, asynchronous, fluent GraphQL client querying API extensions for the amazing Flurl Http library!

This makes it super easy to execute ad-hoc and simple or advanced queries against a GraphQL API such as the awesome [HotChocolate .NET GraphQL Server](https://chillicream.com/docs/hotchocolate/v13).

One of the primary goals of `FlurlGraphQL` is to help to prevent you from getting bogged down in details like what should the GraphQL payload look like, how to handle and parse errors, or 
polluting your data models with unnecessary properties like `Edges`, `Nodes`, `Items`, etc. for paginated data. Handling paginated queries and results is dramatically simplified, making it 
easy and intuitive to retreive any single page, all pages/results, and even stream the results via `IAsyncEnumerable` in `netstandard2.1` (or `IEnumerable<Task>` in `netstandard2.0`).

The spirit of Flurl is fully maintained in this api so you can start your query from your endpoint, fully configure the request just as you would with any Flurl request 
(e.g. manage Headers, Auth Tokens, Query params, Json Serialization settings, etc.).

However, since GraphQL has unique elements we now provide ability to quickly set the query, GraphQL query variables (similar to how you would with Query Params), etc. and
then retrieve the results from the GraphQL Json response in any number of ways -- all of which are named descriptively and intuitivly follow the spirit of Flurl.

#### Give us a Star ⭐
*If you found FlurlGraphQL helpful, the easiest way for you to help is to give us a GitHub Star ⭐! It's completely free and will help the project out!*

#### [Buy me a Coffee ☕](https://www.buymeacoffee.com/cajuncoding)
*I'm happy to share with the community, but if you find this useful (e.g for professional use), and are so inclinded,
then I do love-me-some-coffee!*

<a href="https://www.buymeacoffee.com/cajuncoding" target="_blank">
<img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174">
</a>

### Basic Usage:

[Click here to jump to the advanced Usage Docs below...](#flurlgraphql-usage)

```csharp
var results = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
    .WithGraphQLQuery(@"
        query ($ids: [Int!], $friendsCount: Int!) {
	    charactersById(ids: $ids) {
                personalIdentifier
                name
                friends(first: $friendsCount) {
                    nodes {
                        personalIdentifier
                        name
                    }
                }
            }
        }
    ")
    .SetGraphQLVariables(new { ids = new[] { 1000, 2001 }, friendsCount = 2 })
    .PostGraphQLQueryAsync()
    .ReceiveGraphQLQueryResults<StarWarsCharacter>();
```

### Now Flurl v4.0+ compatible
FlurlGraphQL is now fully updated to support Flurl v4.0+ with some significant performance improvements. Just as when upgrading to Flurl v4+, 
there may be breaking changes such as those highlighted in the [Flurl upgrade docs](https://flurl.dev/docs/upgrade/).

#### Key Changes are:
 - Namespace, Project/Library, and NuGet name has now been simplified to `FlurlGraphQL` (vs `FlurlGraphQL.Querying` in v1.x).
 - Default Json processing now uses `System.Text.Json` for serialization/de-serialization.
   - The use of `System.Text.Json` brings along numerous changes associated with its use so it is best to refer to 
     [Microsoft's migration guide here](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft?pivots=dotnet-6-0).
     - `System.Text.Json` processing with Json transformation strategy is now **~10X faster** than the original Newtonsoft.Json processing
     - The new `Newtonsoft.Json` processing has also been optimized and which now benchmarks at **~2X faster** *-- using the new Json transformation strategy (vs Converter)*.
 - No longer need to manage Json configuration separately from the core Flurl configuration. We now dynamically initialize the Json Serializer using 
   the same settings/options as the core Flurl request.
   - Therefore the previous GraphQL specific global configuration has been removed: `FlurlGraphQLConfig.ConfigureDefaults(config => config.NewtonsoftJsonSerializerSettings = ... )`
 - `Newtonsoft.Json` is still fully supported but requires explicitly referencing the `FlurlGraphQL.Newtonsoft` library also available on Nuget.
   - To then enable `Newtonsoft.Json` processing you need to either:
     1. Initialize your global Flurl settings and/or clients with the out-of-the-box Flurl `NewtonsoftJsonSerializer` via Flurl Global or Request level Configuration.
        - Flurl Global Config Example: `FlurlHttp.Clients.UseNewtonsoft();`
        - Flurl Request or Client level Example: `clientOrRequest.WithSettigns(settings => settings.JsonSerializer = new NewtonsoftJsonSerializer(new JsonSerializerSettings()))...`
        - Doing this will automatically implement Newtonsoft processing for any/all GraphQL requests as the defautl also.
        - See Flurl docs for more info:  [Flurl.Http.Newtonsoft](https://github.com/tmenier/Flurl/tree/dev/src/Flurl.Http.Newtonsoft#flurlhttpnewtonsoft)
     2. Or initialize it at the request level using the `.UseGraphQLNewtonsoftJson(...)` extension method available in ``FlurlGraphQL.Newtonsoft``.
        - The Json processing can always be customized/overridden at the request level for any specific GraphQL Request to use either 
          `System.Text.Json` (via `.UseGraphQLSystemTextJson()`) or `Newtonsoft.Json` via (`.UseGraphQLNewtonsoftJson()`).
   - Dynamics are now only supported when using `Newtonsoft.Json` which is consistent with Flurl v4+.
 - Retrieving the Raw Json responses now have dedicated APIs due to the different Json object models that each Json processing library uses.
   - If using `System.Text.Json` then you must now use the `.ReceiveGraphQLRawSystemTextJsonResponse()` method which returns a `JsonObject`.
   - If using `Newtonsoft.Json` then you must now use the `.ReceiveGraphQLRawNewtonsoftJsonResponse()` method which returns a `JObject`.


## Performance with System.Text.Json vs Newtonsoft.Json
The System.Text.Json processing with Json transformation strategy is now **~10X faster** than the original Newtonsoft.Json processing.

And the newly optimized Newtonsoft.Json processing with new Json transformation strategy (vs Converter) also now benchmarks **~2X faster**.

The following Benchmarks were run using .NET 6. As one might assume older versions of .NET are slower while newer versions are even faster.
For example, .NET 4.6.1 is quite slow compared to .NET 6, however .NET 8 is noticeably faster.

    // * Benchmark.NET Summary using .NET 6 *

    BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3296/23H2/2023Update/SunValley3)
    AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
    .NET SDK 8.0.200-preview.23624.5
      [Host]     : .NET 6.0.28 (6.0.2824.12007), X64 RyuJIT AVX2
      DefaultJob : .NET 6.0.28 (6.0.2824.12007), X64 RyuJIT AVX2


    | Method                             | Mean       | Error    | StdDev   | Ratio |
    |----------------------------------- |-----------:|---------:|---------:|------:|
    | ParsingWithNewtonsoftJsonConverter | 2,517.0 ms | 27.39 ms | 24.28 ms |  1.00 |
    | ParsingWithNewtonsoftJsonTransform | 1,246.9 ms |  9.73 ms |  9.10 ms |  0.50 |
    | ParsingWithSystemTextJsonTransform |   282.8 ms |  5.60 ms |  7.48 ms |  0.11 |

    // * Hints *
    Outliers
      FlurlGraphQLParsingBenchmarks.ParsingWithNewtonsoftJsonConverter: Default -> 1 outlier  was  removed (2.58 s)

    // * Legends *
      Mean   : Arithmetic mean of all measurements
      Error  : Half of 99.9% confidence interval
      StdDev : Standard deviation of all measurements
      Ratio  : Mean of the ratio distribution ([Current]/[Baseline])
      1 ms   : 1 Millisecond (0.001 sec)


## Nuget Packages (netstandard2.0, netstandard2.1, net6.0, and net461 compatible)
To use this in your project, add the [FlurlGraphQL](https://www.nuget.org/packages/FlurlGraphQL/) NuGet package to your project.

To use this in your project with `Newtonsoft.Json` processing then add the add the [FlurlGraphQL.Newtonsoft](https://www.nuget.org/packages/FlurlGraphQL.Newtonsoft/) NuGet package to your project.

## Release Notes:
### v2.0.1
- Fix issue with incorrect deserialization when using wrapper convenience class GraphQLEdge&lt;T&gt;

### v2.0 (compatible with Flurl v4.0+) 🚀
- Implement full support for Flurl v4.0+
- Completely rewritten Json processing engine to now support both System.Text.Json &amp; Newtonsoft.Json.
- System.Text.Json processing with Json transformation strategy is now ~10X faster than the original Newtonsoft.Json processing.
- Optimized Newtonsoft.Json processing with new Json transformation strategy (vs Converter) which now benchmarks at ~2X faster.

### v1.3.1
- Fixed bug in Error handling not identifying GraphQL Server errors correctly in all cases, and therefore not propagating the full details returned by the Server. 
- Fixed bug in Error handling not processing the error path variables correctly.

### v1.3.1
- Fixed Null reference issue in GraphQL/Request Error handling of HttpStatusCode.

### v1.3.0
- Added better support for Mutation handling so that single payload (per Mutation convention best practices) can be returned easily via `.ReceiveGraphQLMutationResult()`.
  - This eliminates the need to use `.ReceiveGraphQLRawJsonResponse()` for dynamic Mutation response handling; but you may continue to do so if required.
- Fixed bug to ensure Errors are returned on IGraphQLQueryResults when possible (not available on Batch Queries).
- Fixed bug in processing logic for paginated requests when TotalCount is the only selected field on a paginated request; only affected CollectionSegment/Offset Paging requests.

### v1.2.0
- Added support to control the Persisted Query payload field name for other GraphQL servers (e.g. Relay server) which may be different than HotChocolate .NET GraphQL Server.
- Added global configuration support via FlurlGraphQLConfig.ConfigureDefaults(config => ...) so that configurable options can be set once globlly with current support for Persisted Query Field Name and Json Serializer Settings.

### v1.1.0
- Added support for Persisted Queries via `.WithGraphQLPersistedQuery()` api.
- Added support to execute GraphQL as GET requests via `.GetGraphQLQueryAsync()` api for edge cases, though `POST` requests are highly encouraged.
- Improved consistency of the use of (optional) custom Json Serialization settings when `.SetGraphQLNewtonsoftJsonSerializerSettings()` is used to override the default GraphQL settings; 
	now initial query payloads sent also use these instead of the default Flurl settings. Some edge cases in GraphQL may require more advanced control over the Json serialization process 
	than the default Flurl Serializer interface offers and the default settings are internal and not accessible so we provide a way to control this for GraphQL specifically.

### v1.0.0
 - v1.0.0 includes `FlurlGraphQL.Querying` api extensions and requires `Flurl v3.2.4` along with `Newtonsoft.Json`.
 - Initial release of the GraphQL Querying (only) extensions for Flurl.Http.
 - Supporting querying of typed data results with support for Relay based Cursor Paging, HotChocolate based Offset paging, Batch querying, Flurl style exception handling, 
     and simplified data models when using nested paginated results from GraphQL.
 - *NOTE: This does not currently support Subscriptions.*

# FlurlGraphQL Usage
These GraphQL apis are an extension of the `Flurl.Http` library.

For core Flurl concepts check out the official [Flurl docs here](https://flurl.dev/docs/fluent-http/). 

Once you have a GraphQL API endpoint url initialized, you will have access to the following:

## Normal Flurl initialization of your GraphQL Endpoint Url
```csharp
var graphqlUrl = "https://graphql-star-wars.azurewebsites.net/api/graphql".SetQueryParam("code", azFuncToken)
```

## Initialize GraphQL Query or Persisted Query (Hash/ID)...
Any calls to any of the GraphQL extensions will return a new `IFlurlGraphQLRequest` which exposes the unique features of GraphQL (vs simple REST API)

```csharp
//Inline your query, or load it from an embedded resource, etc.
//This returns a new IFlurlGraphQLRequest which exposes the unique features of GraphQL (vs simple REST Api)
graphqlUrl.WithGraphQLQuery("...");

//When using Persisted queries you simply need to pass the query id or hash...
//NOTE: It's common for these IDs to be hashes (unique for every change) but really they can be any label 
//          that is likely versioned when changed...
graphqlUrl.WithGraphQLPersistedQuery("AllCharactersWithFriendsPaginated-v1");
```

## Set Query Variables...
```csharp
graphqlUrl
	.SetGraphQLVariable("ids", new[] {1001, 2001})
	.SetGraphQLVariable("friendsCount", 2);
	
//OR
graphqlUrl.SetGraphQLVariables(new { 
	ids = new[] {1001, 2001}, 
	friendsCount = 2 
});
```

## Send your Query to the Server...
Calls to execute the query with the Server will return a new `IFlurlGraphQLResponse` which exposes the unique features for processing GraphQL results (vs simple REST API).
```csharp
//Uses a POST request to execute the query with the server and returns an IFlurlGraphQLResponse...
//NOTE: This is strongly encouraged api to use for many reasons (vs GET request below).
var graphqlResponse = await graphqlUrl.PostGraphQLQueryAsync();

//Uses a GET request to execute the query with the server and returns an IFlurlGraphQLResponse...
//NOTE: This HIGHLY DISCOURAGED since POST requests are far more resilient (no query size or variable limitations),
//      but is provided for edge cases when a GET request must be used.
//NOTE: Some GraphQL servers may not even support Variables with GET requests, though HotChocolate GraphQL for .NET does!
var graphqlResponse = await graphqlUrl.GetGraphQLQueryAsync();
```

## Receive your Results from the Response (simple results)...
To return results in the most simple form this provides an enumerable `IGraphQLQueryResults<out TResult> : IReadOnlyList<TResult>` of your typed results.
```csharp
//Get a simplified flattened enumerable set of typed results...
var graphqlResults = await graphqlResponse.ReceiveGraphQLQueryResults<StarWarsCharacter>();
```

## Receive Paginated Results from the Response (without Cluttering your Data Model)...
Paginated structures in GraphQL is where the responses get more complex but processing them shouldn't be...

The paging apis encapsulate your typed results in a model that provides a greatly simplified facade and handles this as either `IGraphQLConnectionResults<TResult>` for Cursor based pagination or `IGraphQLCollectionSegmentResults<TResult>` for Offset based pagination.

These interfaces both expose `PageInfo` & `TotalCount` properties that may optionally be populated (if requested) along with some helpers such as `HasPageInfo()` or `HasTotalCount()`.

### Cursor Paging Example to simply retrieve a single Page...

Cursor paging is the approach that is strongly recommended by GraphQL.org however, offset based paging (aka CollectionSegment - 
*using from HotChocolate .NET GraphQL Server naming convention*)) is availble also (see below)[#offsetslice-paging-results-via-collectionsegment].

```csharp
var results = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
    .WithGraphQLQuery(@"
        query ($first: Int, $after: String) {
	        characters(first: $first, after: $after) {
                totalCount
		        pageInfo {
                    hasNextPage
                    hasPreviousPage
                    startCursor
                    endCursor
                }
                nodes {
                    personalIdentifier
                    name
	                height
                }
            }
        }
    ")
    .SetGraphQLVariables(new { first = 2 })
    .PostGraphQLQueryAsync()
    .ReceiveGraphQLConnectionResults<StarWarsCharacter>();
```

### Cursor Paging results via Connections...
Cursor based paging is handled by what GraphQL calls a [Connection](https://graphql.org/learn/pagination/#complete-connection-model), and the api is compliant with the [GraphQL.org recommended approach](https://graphql.org/learn/pagination/#connection-specification) provided by the [Relay specification for cursor paging](https://relay.dev/graphql/connections.htm).

```csharp
//Retrieve Cursor based Paginated results (aka Connection)...
var graphqlResults = await graphqlResponse.ReceiveGraphQLConnectionResults<StarWarsCharacter>();

//Access the TotalCount (if requested in the query)...
var totalCount = graphqlResults.TotalCount;

//Access the paging details easily (if requested in the query)...
var pageInfo = graphqlResults.PageInfo;
var hasNextPage = pageInfo.HasNextPage;
var hasPreviousPage = pageInfo.HasPreviousPage;
var startCursor = pageInfo.StartCursor;
var endCursor = pageInfo.EndCursor;

//Enumerate the actual results of the page...
foreach(var starWarsCharacter in graphqlResults)
    Debug.WriteLine(starWarsCharacter.Name);
```

### Want access to the actual Cursor?
```csharp
//Use the provided GraphQLEdge<> class wrapper when Retrieving your Cursor based Paginated results (aka Connection)...
var graphqlResults = await graphqlResponse.ReceiveGraphQLConnectionResults<GraphQLEdge<StarWarsCharacter>>();

//Enumerate the actual results of the page...
foreach(var edge in graphqlResults)
{
    var starWarsCharacter = edge.Node;
    var cursor = edge.Cursor;
}

//OR Implement the IGraphQLEdge interface on your models...
public class CursorStarWarsCharacter : StarWarsCharacter, IGraphQLEdge { /* Implement Cursor property */ };

//OR simply add a Cursor Property (must be read/write) to your Model (IGraphQLEdge interface is not strictly needed)...
//NOTE: You may also do this to access any other non-standard property that may be customized and added to the `Edge` by the GraphQL Server...
public class CursorStarWarsCharacter : StarWarsCharacter 
{
    public string Cursor { get; set; }
    public int CustomEdgeProperty { get; set; }
};

//Retrieve Cursor based Paginated results (aka Connection)...
var graphqlResults = await graphqlResponse.ReceiveGraphQLConnectionResults<CursorStarWarsCharacter>();

//Enumerate the actual results which will also have the Cursor (if requested in the query)...
foreach(var result in graphqlResults)
    Debug.WriteLine($"My Name is {result.Name} and my Cursor is [{result.Cursor}].");

```

## Advanced Cursor Pagination (Retrieve or Stream ALL pages)
The api significantly simplifies the process of iterating through all pages of a GraphQL query and can either internally retreive all pages (returns an enumerable set of all pages), or allow streaming of the pages for you to handle.

**NOTE:** _The streaming function is very efficient (esp. `AsyncEnumerable`) and actually pre-fetches the next page (if one exists) while you are processing the current page._

### Retrieve All Cursor based Page results ...
**NOTE: This will block and await while processing and retrieving all possible pages!**
```csharp
//Returns an IList<IGraphQLConnectionResults<TResult>>
//NOTE: This will block and await while processing and retrieving all possible pages...
var graphqlPages = await graphqlResponse.ReceiveAllGraphQLQueryConnectionPages<StarWarsCharacter>();

//Iterate the pages...
foreach (var page in graphqlPages)
{
    page.HasAnyResults();
    page.HasTotalCount();
    page.PageInfo?.StartCursor
    page.PageInfo?.EndCursor
}

//Gather All results for all the pages...
var allResults = graphqlPages.SelectMany(p => p);
```

### Stream All Cursor based Page results (netstandard2.1)...
```csharp
//Returns an IAsyncEnumerable<IGraphQLConnectionResults<TResult>>
var pagesAsyncEnumerable = graphqlResponse.ReceiveGraphQLConnectionPagesAsyncEnumerable<StarWarsCharacter>();

//Stream the pages...
//NOTE: Using this process to store the resulting page data to a Repository/DB would be done in 
//	a streaming fashion minimizing the memory utilization...
await foreach (var page in pagesAsyncEnumerable)
{
    //... process the page data...
}
```

### Stream All Cursor based Page results (netstandard2.0)...
NOTE: AsyncEnumerable is not available in netstandard2.0, however we can still emulate the streaming to minimize our server utilization via `IEnumerable<Task<>>` 
which awaits each item as we enumerate.
```csharp
//Returns an IEnumerable<Task<IGraphQLConnectionResults<TResult>>>
var graphqlPagesTasks = await graphqlResponse.ReceiveGraphQLConnectionPagesAsEnumerableTasks<StarWarsCharacter>();

//Enumerate the async retrieved pages (as Tasks) in a streaming fashion...
//NOTE: Using this process to store the resulting page data to a Repository/DB would be done in 
//	a streaming fashion minimizing the memory utilization...
foreach (var pageTask in graphqlPagesTasks)
{
    var page = await pageTask;
    //... process the page data...
}
```

### Offset/Slice Paging results via CollectionSegment...
Offset based paging is not recommeded by GraphQL.org and therefore is less formalized with [no recommended implemenation](https://graphql.org/learn/pagination/#pagination-and-edges). 
So this api is compliant with the [HotChocolate .NET GraphQL Server] approach wich is fully GraphQL Spec compliant and [provides a formal implementaiton of Offset paging](https://chillicream.com/docs/hotchocolate/v12/fetching-data/pagination#offset-pagination) 
using `skip`/`take` arguments to return a `CollectionSegment`.
NOTE: However the HotChocoalte team also strongly encourages the use of Cursor Paging with Connections as the most flexible form of pagination.

```csharp
//Retrieve Offset based Paginated results (aka CollectionSegment)...
var graphqlResults = await graphqlResponse.ReceiveGraphQLCollectionSegmentResults<StarWarsCharacter>();

//Access the TotalCount (if requested in the query)...
var totalCount = graphqlResults.TotalCount;

//Access the paging details easily (if requested in the query)...
var pageInfo = graphqlResults.PageInfo;
var hasNextPage = pageInfo.HasNextPage;
var hasPreviousPage = pageInfo.HasPreviousPage;

//Enumerate the actual results of the page...
foreach(var starWarsCharacter in graphqlResults)
    Debug.WriteLine(starWarsCharacter.Name);
```

## Advanced Offset Pagination (Retrieve or Stream ALL pages)
Just as with Cursor pagination, the api significantly simplifies the process of iterating through all pages of a GraphQL query for Offset pagination results also.
NOTE: The streaming function is very efficient (esp. `AsyncEnumerable`) and actually pre-fetches the next page (if one exists) while you are processing the current page.

#### Retrive All Offset based Page results...
**NOTE:** This will block and await while processing and retrieving all possible pages...
```csharp
//Returns an IList<IGraphQLCollectionSegmentResults<TResult>>
var graphqlPages = await graphqlResponse.ReceiveAllGraphQLQueryCollectionSegmentPages<StarWarsCharacter>();

//Iterate the pages...
foreach (var page in graphqlPages)
{
    page.HasAnyResults();
    page.HasTotalCount();
}

//Gather All results for all the pages...
var allResults = graphqlPages.SelectMany(p => p);
```

#### Stream All Offset based Page results (netstandard2.1)...
```csharp
//Returns an IAsyncEnumerable<IGraphQLCollectionSegmentResults<TResult>>
var pagesAsyncEnumerable = graphqlResponse.ReceiveGraphQLCollectionSegmentPagesAsyncEnumerable<StarWarsCharacter>();

//Stream the pages...
//NOTE: Using this process to store the resulting page data to a Repository/DB would be done in 
//	a streaming fashion minimizing the memory utilization...
await foreach (var page in pagesAsyncEnumerable)
{
    //... process the page data...
}
```

#### Stream All Offset based Page results (netstandard2.0)...
NOTE: AsyncEnumerable is not available in netstandard2.0, however we can still emulate the streaming to minimize our server utilization via `IEnumerable<Task<>>` 
which awaits each item as we enumerate.
```csharp
//Returns an IEnumerable<Task<IGraphQLCollectionSegmentResults<TResult>>>
var graphqlPagesTasks = await graphqlResponse.ReceiveGraphQLCollectionSegmentPagesAsEnumerableTasks<StarWarsCharacter>();

//Enumerate the async retrieved pages (as Tasks) in a streaming fashion...
//NOTE: Using this process to store the resulting page data to a Repository/DB would be done in 
//	a streaming fashion minimizing the memory utilization...
foreach (var pageTask in graphqlPagesTasks)
{
    var page = await pageTask;
    //... process the page data...
}
```

### Data Models with Nested Paginated results...
In GraphQL it's easy to expose nested selections of a result than itself is a paginated set of data. Thats why de-serializing this into
a normal model is complex and usually results in dedicated data models that are cluttered / polluted with unecessary elements such as `Nodes`, `Items`, `Edges`, or `PageInfo`, `Cursor`, etc.

You can still use these models if you like but in many cases with these nested data elements we primarily care about the results and
would like to keep a simplified model. This is handled by the api in that any `List<>` or `Array` in your data model (aka implements `ICollection`) that
is mapped to a paginated result in the GraphQL response, will automatically be flattened. So if your query requested `Edges` or `Nodes` they
are collected into the simplified model as a simple list of results without the need to complicate our data model.

Here's an example of how we might just want the Friends as a list of results in our StarWarsCharacter (simple model), and we can select them via a complex nested,
recursive, paginated graph and this will be automatically handled:
```csharp

public class StarWarsCharacter
{
    public string Name { get; set; }
    //Recursive referencing set of Paginated Friends...
    public List<StarWarsCharacter> Friends { get; set; }
    public string Cursor { get; set; }
}

var results = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
    .WithGraphQLQuery(@"
        query ($ids: [Int!], $friendsCount: Int!) {
	        charactersById(ids: $ids) {
	            friends(first: $friendsCount) {
	                nodes {
		                friends(first: $friendsCount) {
		                    nodes {
			                    name
			                    personalIdentifier
			                }
		                }
		            }
		        }
	        }
        }
    ")
    .SetGraphQLVariables(new { ids = new[] { 1000, 2001 }, friendsCount = 3 })
    .PostGraphQLQueryAsync()
    .ReceiveGraphQLQueryResults<StarWarsCharacter>();

foreach (var result in results)
{
    //... process each of the 2 initial results by Id...
    foreach (var friend in result.Friends)
    {
        //... process each of the nested friend results, but without the need for 
        //     our Model to be complicated by the fact that this is a paginated result from GraphQL...
    }
}
```

## Mutations...
GraphQL provides the ability to create, update, delete data in what are called mutation operations. In addition, *mutations* are different from queries in that they
have different conventions and best practices for their implementations. In general, GraphQL mutations should take in a single *Input* and return a single 
result *Payload*; the result payload may be a single business object type but is more commonly a root type for a collection of Results & Errors (as defined by the GraphQL Schema).

Due to the complexity of all the varying types of Mutation Input & result Paylaod designs the API for mutations will fallback to parse the response as a single
object result model (as opposed to a an Array that a Query would return). Therefore, your model should implement any/all of the response Payload field features you are interested in.

And if you need even more low level processing or just want to handle the Mutation result more dynamically then you can always use raw Json handling 
via the ReceiveGraphQLRawJsonResponse API (see below).

```csharp
var newCharacterModel = new CharacterModel()
{
    //...Populate the new Character Model...
};

var json = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
    .WithGraphQLQuery(@"
        mutation ($newCharacter: Character) {
            characterCreateOrUpdate(input: $newCharacter) {
		        result {
                    personalIdentifier
	                name
		        }
		        errors {
			        ... on Error {
				        errorCode
				        message
			        }
		        }
	        }
        }
    ")
    .SetGraphQLVariables(new { newCharacter: newCharacterModel })
    .PostGraphQLQueryAsync()
    //NOTE: Here CharacterCreateOrUpdate Result will a single Payload result (vs a List as  Query would return)
    //      for which teh model would have both a Result property & an Errors property to be deserialized based
    //      on the unique GraphQL Schema Mutation design...
    .ReceiveGraphQLMutationResult<CharacterCreateOrUpdateResult>();
```


## Batch Querying...
GraphQL provides the ability to execute multiple queries in a single request as a batch. When this is done each response is provided in the same order as requested
in the json response object named the same as the GraphQL Query operation. The api provides the ability to retrieve all results (vs only the first by default) by
retrieving the Batch results, which can then be handled one by one.

Each query can be retrieved by it's index or it's operation string key name (*case-insensitive*).

Here's an example of a batch query that uses an alias to run multiple queries and retrieve the results...
```csharp
var batchResults = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
    .WithGraphQLQuery(@"
        query ($first: Int) {
	        characters(first: $first) {
                nodes {
	                personalIdentifier
		            name
	                height
		        }
	        }

	        charactersCount: characters {
                totalCount
	        }
        }
    ")
    .SetGraphQLVariables(new { first = 2 })
    .PostGraphQLQueryAsync()
    .ReceiveGraphQLBatchQueryResults();

var charactersResultsByName = batchResults.GetResults<StarWarsCharacter>("characters");
var charactersResultsByIndex = batchResults.GetResults<StarWarsCharacter>(0);
Assert.AreEqual(charactersResultsByName, charactersResultsByIndex);

var countResult = batchResults.GetConnectionResults<StarWarsCharacter>("charactersCount");
Assert.IsTrue(countResult.TotalCount > charactersResultsByName.Count);
```

## RAW Json Handling (fully manual)...
You can always request the raw Json response that was returned by the GraphQL server ready to be fully handled manually (for off-the-wall edge cases). 
Simply use the respective API based on if you want are using System.Text.Json (via `.ReceiveGraphQLRawSystemTextJsonResponse()`) 
or Newtonsoft.Json (via `.ReceiveGraphQLRawNewtonsoftJsonResponse()`).

*NOTE: You cannot receive `Newtonsoft.Json` raw json if the request was initialzied and executing using `System.Text.Json` serailizer and vice-versa; 
a runtime exception will be thrown becuase this would be a large performance impact that would likely go unnoticed if it was allowed.*

```csharp

//For System.Text.Json Raw GraphQL response processing you will get a `JsonObject` result back for RAW Json handling!
var jsonObject = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
    .WithGraphQLQuery(@"
        query ($first: Int) {
            characters(first: $first) {
                nodes {
	                personalIdentifier
	                name
	                height
		        }
	        }
        }
    ")
    .SetGraphQLVariables(new { first = 2 })
    .PostGraphQLQueryAsync()
    .ReceiveGraphQLRawSystemTextJsonResponse();

//OR For Newtonsoft.Json Raw GraphQL response processing you will get a `JObject` result back (just as with v1.x) for RAW Json handling!
var jsonObject = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
    .WithGraphQLQuery(@"
        query ($first: Int) {
            characters(first: $first) {
                nodes {
	                personalIdentifier
	                name
	                height
		        }
	        }
        }
    ")
    .SetGraphQLVariables(new { first = 2 })
    .PostGraphQLQueryAsync()
    .ReceiveGraphQLRawNewtonsoftJsonResponse();
```

## Error Handling...
Consistent with the [spirit of Flurl for error handling](https://flurl.dev/docs/error-handling/#error-handling), errors from GraphQL will result 
in a `FlurlGraphQLException` being thrown with the details of the errors payload already parsed & provided as a helpful error message. However the raw error 
details are also available in the `GraphQLErrors` property.

```csharp
try
{
    var json = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
        .WithGraphQLQuery(@"
            query (BAD_REQUEST) {
	            MALFORMED QUERY
            }
        ")
        .PostGraphQLQueryAsync()
        .ReceiveGraphQLRawJsonResponse();
}
catch(FlurlGraphQLException graphqlException)
{
    //... handle the exception...
    graphqlException.Message; //Message constructed from available details in the GraphQL Errors collection
    graphqlException.Query; //Original Query
    graphqlException.GraphQLErrors; //Parsed GraphQL Errors as Models that you can interrogate
    graphqlException.ErrorResponseContent; //Original Error Response Json Text
    graphqlException.InnerException; //The Original Http Exception
}
```

## Need direct control over the Json Serialization Settings?
Now with version 2.0 we dynamically inherit and implement all the settings from the base/core Flurl request!

Therefore there is no need to explicitly set the settings for only GraphQL requests anymore, however you may continue to do so.

We still provide support to manually control the Json serializer settings specifically for individual GraphQL request processing.

*NOTE: These settings will impact both how the initial query paylaod is serialized before being sent to the GraphQL server 
and how the response is parsed when being de-serailized back into your model.*

```csharp
    //Override the Json Serialization Settings per request...
    var json = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
        .WithGraphQLQuery("...")
        .UseGraphQLSystemTextJson(new JsonSerializerOptions() //<== System.Text.Json Options!
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            //NOTE: THIS settings will not actually have any effect as the Framework always switches it ON to enable `case insensitive` processing.
            //      This is due to the fact that GraphQL Json and C# often have different naming conventions for the Json so the vast majority of requests 
            //      would fail if not enabled automatically!
            PropertyNameCaseInsensitive = true
        })
        .SetGraphQLVariables(...)
        .PostGraphQLQueryAsync()
        .ReceiveGraphQLRawSystemTextJsonResponse();

    //OR for Newtonsoft.Json then you need to use the following for each request...
    var json = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
        .WithGraphQLQuery("...")
        .UseGraphQLNewtonsoftJson(new JsonSerializerSettings() //<== Newtonsof.Json Settings!
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        })
        .SetGraphQLVariables(...)
        .PostGraphQLQueryAsync()
        .ReceiveGraphQLRawNewtonsoftJsonResponse();
```

## Need to control the Persisted Query Parameter Field Name?

The .NET HotChocolate GraphQL Server uses `id` for the Persisted query parameter name, however not all servers are consistent here.
Relay for example uses `doc_id` (see [here for more details](https://chillicream.com/docs/hotchocolate/v13/performance/persisted-queries#client-expectations)).

This can be controlled per request, or globally by setting them in the FlurlGraphQL default configuration...
```csharp
    //Override the Persisted Query field name per request...
    var json = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
        .WithGraphQLPersistedQuery("...")
        .SetPersistedQueryPayloadFieldName("doc_id")
        .SetGraphQLVariables(...)
        .PostGraphQLQueryAsync()
        .ReceiveGraphQLRawJsonResponse();


    //Override the Persisted Query field name globally by setting it in the default configuration...
    FlurlGraphQLConfig.ConfigureDefaults(config =>
    {
        config.PersistedQueryPayloadFieldName = "doc_id";
    });
```
