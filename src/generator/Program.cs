using Bogus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Spatial;
using Newtonsoft.Json;

var faker = new Faker();
//this is the local cosmos emulator instance so stop giving me grief about the connection string
var client = new CosmosClient("https://localhost:8081", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
await client.CreateDatabaseIfNotExistsAsync("rainfalldb");
var database = client.GetDatabase("rainfalldb");
ContainerResponse containerResponse = await database.CreateContainerIfNotExistsAsync("rainfallcollection", "/town");
GeospatialConfig geospatialConfig = new GeospatialConfig(GeospatialType.Geography);
containerResponse.Resource.GeospatialConfig = geospatialConfig;

SpatialPath spatialPath = new SpatialPath
{
    Path = "/location/*"
};
spatialPath.SpatialTypes.Add(SpatialType.Point);
containerResponse.Resource.IndexingPolicy.SpatialIndexes.Clear();
containerResponse.Resource.IndexingPolicy.SpatialIndexes.Add(spatialPath);

var container = database.GetContainer("rainfallcollection");
await container.ReplaceContainerAsync(containerResponse.Resource);

var rainfallFaker = new Faker<Rainfall>()
    .RuleFor(r => r.Id, f => Guid.NewGuid().ToString())
    .RuleFor(r => r.Town, f => f.Address.City())
    .RuleFor(r => r.State, f => f.Address.State())
    .RuleFor(r => r.Date, f => f.Date.Past(1))
    .RuleFor(r => r.RainfallInInches, f => f.Random.Double())
    .RuleFor(r => r.Location, f => new Point(f.Address.Longitude(), f.Address.Latitude()));
var rainfallData = rainfallFaker.Generate(10000);
foreach (var item in rainfallData)
{
    await container.CreateItemAsync(item, new PartitionKey(item.Town));
}

public class Rainfall
{
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("town")]
    public string Town { get; set; }
    [JsonProperty("state")]
    public string State { get; set; }
    [JsonProperty("date")]
    public DateTime Date { get; set; }
    [JsonProperty("rainfallininches")]
    public double RainfallInInches { get; set; }

    [JsonProperty("location")]
    public Point Location { get; set; }
}
