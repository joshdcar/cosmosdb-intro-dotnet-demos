using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosSample
{
    class Program
    {

        // These are the url and the keys for the local CosmosDB Emulator
        private static string cosmsosUrl = "https://localhost:8081";
        private static string cosmosKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private static readonly FeedOptions DefaultOptions = new FeedOptions { EnableCrossPartitionQuery = true };

        static void Main(string[] args)
        {

            //Query Examples
            var result = QueryData();

            //Join Examples (self joins)
            QueryWithJoins();

            //Query w/ location preferences
            QueryWithLocationPreferences();

            //Query w/ statistics expensive\optimized query comparison (RU Cost, etc)
            QueryWithStats();

            //Record Insert
            UpsertDocument(result);


            Console.WriteLine("Press Any Key To Exit");
            Console.Read();

        }


        private static NutritionModel QueryData()
        {

            DocumentClient client = new DocumentClient(new Uri(cosmsosUrl), cosmosKey);

            var collectionUri = UriFactory.CreateDocumentCollectionUri("NutritionDb", "NutritionData");

            //Linq query
            var linqResults = from n in client.CreateDocumentQuery<NutritionModel>(collectionUri, DefaultOptions)
                          where n.id == "03287"
                          select n;

            //Lamda Query
            var lamdaResults = client.CreateDocumentQuery<NutritionModel>(collectionUri, DefaultOptions)
                               .Where(n => n.id == "03287");


            // SQL Query Example
            var query = "Select * from NutritionData where NutritionData.id = @id";

            IQueryable<NutritionModel> results = client.CreateDocumentQuery<NutritionModel>(
                collectionUri,
                new SqlQuerySpec
                {
                    QueryText = query,
                    Parameters = new SqlParameterCollection()
                    {
                        new SqlParameter("@id", "03287")
                    }
                });

            var result = results.AsEnumerable().FirstOrDefault();

            Console.WriteLine("Result: " + result.ToString());

            

            return result;


        }

        private static void QueryWithJoins()
        {

            DocumentClient client = new DocumentClient(new Uri(cosmsosUrl), cosmosKey);

            var collectionUri = UriFactory.CreateDocumentCollectionUri("NutritionDb", "NutritionData");

            //Join on the child collection of tags so we can query for tags within that question
            var query = @"SELECT Value NutritionData
                            FROM NutritionData
                            JOIN tags IN NutritionData.tags
                            WHERE tags.name = 'peach cobbler'";

            IQueryable<NutritionSummaryModel> results = client.CreateDocumentQuery<NutritionSummaryModel>(
                collectionUri,
                new SqlQuerySpec
                {
                    QueryText = query
                });

            var items = results.ToList();

            foreach (var item in items)
            {
                Console.WriteLine(item.ToString());
            }


        }

        private static void UpsertDocument(NutritionModel model)
        {
            DocumentClient client = new DocumentClient(new Uri(cosmsosUrl), cosmosKey);
            var collectionUri = UriFactory.CreateDocumentCollectionUri("NutritionDb", "NutritionData");

            model.version = model.version + 1;

            var result = client.UpsertDocumentAsync(collectionUri, model);
            result.Wait();

        }

        private static void QueryWithLocationPreferences()
        {
            ConnectionPolicy connectionPolicy = new ConnectionPolicy();

            connectionPolicy.PreferredLocations.Add(LocationNames.WestUS);
            connectionPolicy.PreferredLocations.Add(LocationNames.EastUS);

            DocumentClient client = new DocumentClient(new Uri(cosmsosUrl), cosmosKey, connectionPolicy);

            var collectionUri = UriFactory.CreateDocumentCollectionUri("NutritionDb", "NutritionData");

            // SQL Query Example
            var query = "Select top 10 nd.id, nd.history.createdBy as createdBy from NutritionData nd";

            IQueryable<NutritionSummaryModel> results = client.CreateDocumentQuery<NutritionSummaryModel>(
                collectionUri,
                new SqlQuerySpec
                {
                    QueryText = query
                });

            var items = results.ToList();

            foreach (var item in items)
            {
                Console.WriteLine(item.ToString());
            }

        }

        private static void QueryWithStats()
        {
            DocumentClient client = new DocumentClient(new Uri(cosmsosUrl), cosmosKey);

            var collectionUri = UriFactory.CreateDocumentCollectionUri("NutritionDb", "NutritionData");

            var feedOptions = new FeedOptions { PopulateQueryMetrics = true };

            //Slow Query - Note the use of the LOWER Function and the effect on the number of records queries, RU cost, and index usage
            var slowQuery = "Select top 20 * from NutritionData where CONTAINS(NutritionData.description, LOWER('Turkey'))";

            var sqlSlowQuery = new SqlQuerySpec { QueryText = slowQuery };

            IDocumentQuery<dynamic> slowMetricQuery = client.CreateDocumentQuery(collectionUri,slowQuery,feedOptions).AsDocumentQuery();

            var slowMetricQueryResult = slowMetricQuery.ExecuteNextAsync();
            slowMetricQueryResult.Wait();

            FeedResponse<dynamic> slowFeedResponse = slowMetricQueryResult.Result;

            // Returns metrics by partition key range Id 
            var metrics = slowFeedResponse.QueryMetrics;

            Console.WriteLine("");
            Console.WriteLine($"SLOW QUERY: ");
            Console.WriteLine($"Total Time: {metrics.Values.FirstOrDefault().TotalTime}");
            Console.WriteLine($"Index Hit Ratio: {metrics.Values.FirstOrDefault().IndexHitRatio}");
            Console.WriteLine($"Document Count: {metrics.Values.FirstOrDefault().RetrievedDocumentCount}");
            Console.WriteLine($"RU COST: {slowFeedResponse.RequestCharge}");
            Console.WriteLine($"RU Per Minute Used: {slowFeedResponse.IsRUPerMinuteUsed}");

            //Optimized Query - Going against a lower case field without using the Lower function

            var optimizedQuery = "Select top 20 * from NutritionData where CONTAINS(NutritionData.descriptionLowerCase, 'turkey')";

            var sqlOptimizedQuery = new SqlQuerySpec { QueryText = optimizedQuery };

            IDocumentQuery<dynamic> metricFastQuery = client.CreateDocumentQuery(
            collectionUri,
            optimizedQuery,
            feedOptions).AsDocumentQuery();

            var metricOptimizedQueryResult = metricFastQuery.ExecuteNextAsync();
            metricOptimizedQueryResult.Wait();

            FeedResponse<dynamic> optimizedFeedResponse = metricOptimizedQueryResult.Result;

            // Returns metrics by partition key range Id 
            IReadOnlyDictionary<string, QueryMetrics> fastmetrics = optimizedFeedResponse.QueryMetrics;

            Console.WriteLine($"OPTIMIZED QUERY: ");
            Console.WriteLine($"Total Time: {fastmetrics.Values.FirstOrDefault().TotalTime}");
            Console.WriteLine($"Index Hit Ratio: {fastmetrics.Values.FirstOrDefault().IndexHitRatio}");
            Console.WriteLine($"Document Count: {fastmetrics.Values.FirstOrDefault().RetrievedDocumentCount}");
            Console.WriteLine($"RU COST: {optimizedFeedResponse.RequestCharge}");
            Console.WriteLine($"RU Per Minute Used: {optimizedFeedResponse.IsRUPerMinuteUsed}");


        }

    }
}
