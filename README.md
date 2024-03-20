# FlurlGraphQL
Lightweight, simplified, asynchronous, fluent GraphQL client querying API extensions for the amazing Flurl Http library!

This makes it super easy to execute ad-hoc and simple queries against a GraphQL API such as the awesome [HotChocolate .NET GraphQL Server](https://chillicream.com/docs/hotchocolate/v13).

`FlurlGraphQL.Querying` helps to prevent you from getting bogged down in details like what should the GraphQL payload look like, how to handle and parse errors, or polluting your data models with unnecessary properties like `Edges`, `Nodes`, `Items`, etc. for paginated data. Handling paginated queries and results is dramatically simplified, making it easy and intuitive to retreive any page, all results, and even stream the results via `IAsyncEnumerable` in `netstandard2.1` or `IEnumerable<Task>` in `netstandard2.0`.

The spirit of Flurl is fully maintained in this api so you can start your query from your endpoint, fully configure the request just as you would with any Flurl request (e.g. manage Headers, Auth Tokens, Query params, etc.).

However, since GraphQL has unique elements we now provide ability to quickly set the query, query variables (similar to how you would with Query Params), etc. and
then retrieve the results from the GraphQL Json response in any number of ways.

#### Give us a Star ⭐
*If you found FlurlGraphQL helpful, the easiest way for you to help is to give us a GitHub Star ⭐! It's completely free and will help the project out!*

#### [Buy me a Coffee ☕](https://www.buymeacoffee.com/cajuncoding)
*I'm happy to share with the community, but if you find this useful (e.g for professional use), and are so inclinded,
then I do love-me-some-coffee!*

<a href="https://www.buymeacoffee.com/cajuncoding" target="_blank">
<img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174">
</a>

## Basic Query Results Example:
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

## Performance with System.Text.Json vs Newtonsoft.Json
    // * Summary *

    BenchmarkDotNet v0.13.12, Windows 11 (10.0.22621.3155/22H2/2022Update/SunValley2)
    AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
    .NET SDK 8.0.200-preview.23624.5
      [Host]     : .NET 6.0.27 (6.0.2724.6912), X64 RyuJIT AVX2 [AttachedDebugger]
      DefaultJob : .NET 6.0.27 (6.0.2724.6912), X64 RyuJIT AVX2


    | Method                    | Mean      | Error    | StdDev   | Ratio |
    |-------------------------- |----------:|---------:|---------:|------:|
    | ParsingNewtonsoftJson     | 578.75 ms | 4.985 ms | 4.663 ms |  1.00 |
    | ParsingWithSystemTextJson |  54.50 ms | 0.738 ms | 0.654 ms |  0.09 |

    // * Summary *

    BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3296/23H2/2023Update/SunValley3)
    AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
    .NET SDK 8.0.200-preview.23624.5
      [Host]     : .NET 6.0.28 (6.0.2824.12007), X64 RyuJIT AVX2
      DefaultJob : .NET 6.0.28 (6.0.2824.12007), X64 RyuJIT AVX2


    | Method                             | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD |
    |----------------------------------- |-----------:|---------:|---------:|-----------:|------:|--------:|
    | ParsingWithNewtonsoftJsonConverter | 2,212.6 ms | 22.88 ms | 20.29 ms | 2,213.6 ms |  1.00 |    0.00 |
    | ParsingWithNewtonsoftJsonRewriting | 1,058.9 ms | 20.92 ms | 36.08 ms | 1,042.5 ms |  0.49 |    0.02 |
    | ParsingWithSystemTextJsonRewriting |   291.2 ms |  3.08 ms |  2.73 ms |   290.2 ms |  0.13 |    0.00 |

    // * Hints *
    Outliers
      FlurlGraphQLParsingBenchmarks.ParsingWithNewtonsoftJsonConverter: Default -> 1 outlier  was  removed (2.31 s)
      FlurlGraphQLParsingBenchmarks.ParsingWithNewtonsoftJsonRewriting: Default -> 2 outliers were removed (1.18 s, 1.19 s)
      FlurlGraphQLParsingBenchmarks.ParsingWithSystemTextJsonRewriting: Default -> 1 outlier  was  removed (301.52 ms)
      

    // * Summary *

    BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3296/23H2/2023Update/SunValley3)
    AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
    .NET SDK 8.0.200-preview.23624.5
      [Host]     : .NET 6.0.28 (6.0.2824.12007), X64 RyuJIT AVX2
      DefaultJob : .NET 6.0.28 (6.0.2824.12007), X64 RyuJIT AVX2


    | Method                             | Mean       | Error    | StdDev   | Ratio |
    |----------------------------------- |-----------:|---------:|---------:|------:|
    | ParsingWithNewtonsoftJsonConverter | 2,421.8 ms | 29.55 ms | 27.64 ms |  1.00 |
    | ParsingWithNewtonsoftJsonRewriting | 1,025.1 ms | 15.77 ms | 12.31 ms |  0.42 |
    | ParsingWithSystemTextJsonRewriting |   323.9 ms |  5.77 ms |  5.40 ms |  0.13 |


## Nuget Package (netstandard2.0 & netstandard2.1)
To use this in your project, add the [FlurlGraphQL.Querying](https://www.nuget.org/packages/FlurlGraphQL.Querying/) NuGet package to your project.

## Release Notes:
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

# Fluent GraphQL Querying (extensions for Flurl.Http):
These GraphQL apis are an extension of the `Flurl.Http` library.

For core Flurl concepts check out the official [Flurl docs here](https://flurl.dev/docs/fluent-http/). 

Once you have a GraphQL API endpoint url initialized, you will have access to the following:

### Normal Flurl initialization of your GraphQL Endpoint Url
```csharp
var graphqlURL = "https://graphql-star-wars.azurewebsites.net/api/graphql".SetQueryParam("code", azFuncToken)
```

### Initialize GraphQL Query or Persisted Query (Hash/ID)...
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

### Set Query Variables...
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

### Send your Query to the Server...
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

### Receive your Results from the Response (simple results)...
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

#### Want access to the actual Cursor?
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
public class CursorStarWarsCharacter : StarWarsCharacter, IGraphQLEdge {};

//Retrieve Cursor based Paginated results (aka Connection)...
var graphqlResults = await graphqlResponse.ReceiveGraphQLConnectionResults<CursorStarWarsCharacter>();

//Enumerate the actual results which will also have the Cursor (if requested in the query)...
foreach(var result in graphqlResults)
    Debug.WriteLine($"My Name is {result.Name} and my Cursor is [{result.Cursor}].");

```

### Advanced Cursor Pagination (Retrieve or Stream ALL pages)
The api significantly simplifies the process of iterating through all pages of a GraphQL query and can either internally retreive all pages (returns an enumerable set of all pages), or allow streaming of the pages for you to handle.

**NOTE:** _The streaming function is very efficient (esp. `AsyncEnumerable`) and actually pre-fetches the next page (if one exists) while you are processing the current page._

#### Retrieve All Cursor based Page results ...
**NOTE:** This will block and await while processing and retrieving all possible pages...
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

#### Stream All Cursor based Page results (netstandard2.1)...
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

#### Stream All Cursor based Page results (netstandard2.0)...
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

### Advanced Offset Pagination (Retrieve or Stream ALL pages)
Just as with Cursor pagination, the api significantly simplifies the process of iterating through all pages of a GraphQL query for Offset pagination also.
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

### Mutations...
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


### Batch Querying...
GraphQL provides the ability to execute multiple queries in a single request as a batch. When this is done each response is provided in the same order as requested
in the json response object named the same as the GraphQL Query operation. The api provides the ability to retrieve all results (vs only the first by default) by
retrieving the Batch results, which can then be handled one by one.

Each query can be retrieved by it's index or it's operation string key name (case-insensitive).

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

### RAW Json Handling (fully manual)...
You can always request the raw Json response that was returned by the GraphQL server ready to be fully handled manually (for off-the-wall edge cases). 
Simply use the `.ReceiveGraphQLRawJsonResponse()` api method to get the response as a parsed Json result (e.g. `JObject` for `Newtonsoft.Json`)

```csharp
var json = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
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
    .ReceiveGraphQLRawJsonResponse();
```


### Error Handling...
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
    graphqlException.Message; //Message constructed from available details int he GraphQL Errors 
    graphqlException.Query; //Original Query
    graphqlException.GraphQLErrors; //Parsed GraphQL Errors
    graphqlException.ErrorResponseContent; //Original Error Response Json Text
    graphqlException.InnerException; //The Original Http Exception
}
```

### Need direct control over the Json Serialization Settings?
The default Flurl serializer interface is very limited and the Json Serializer settings (that you can override) are internal
and not accessible. Therefore we provide support to manually control the Json serializer settings specifically for GraphQL
processing, and we use these consistently if they are set.

In addition the Json Serialization settings can be controlled per request, or globally by setting them in the FlurlGraphQL default configuration...

*NOTE: These settings will impact both how the initial query paylaod is serialized before being sent to the GraphQL server 
and how the response is parsed when being de-serailized back into your model.*

```csharp
    //Override the Json Serialization Settings per request...
    var json = await "https://graphql-star-wars.azurewebsites.net/api/graphql"
        .WithGraphQLQuery("...")
        .SetGraphQLNewtonsoftJsonSerializerSettings(new JsonSerializerSettings()
        {
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        })
        .SetGraphQLVariables(...)
        .PostGraphQLQueryAsync()
        .ReceiveGraphQLRawJsonResponse();

    //Override the Json Serialization Settings globally by setting them in the default configuration...
    FlurlGraphQLConfig.ConfigureDefaults(config =>
    {
        config.NewtonsoftJsonSerializerSettings = new JsonSerializerSettings()
        {
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };
    });
```

### Need to control the Persisted Query Parameter Field Name?

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