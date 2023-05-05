using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Spatial;
using Newtonsoft.Json;
using Bogus;

var cosmosClient = new CosmosClient(
    "https://localhost:8081", 
    "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");

var database = cosmosClient.GetDatabase("rainfalldb");

var containerResponse = await database.CreateContainerIfNotExistsAsync("rainfallcollection", "/town", 400);
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

var rainfallData = container.GetItemLinqQueryable<Rainfall>(allowSynchronousQueryExecution: true);

var featureCollection = new FeatureCollection
{
    Type = "FeatureCollection",
    Features = new List<Feature>()
};

foreach (var rain in rainfallData)
{
    var feature = new Feature
    {
        Type = "Feature",
        Geometry = new Geometry
        {
            Type = "Point",
            Coordinates = new List<object> { rain.Location.Position.Longitude, rain.Location.Position.Latitude }
        },
        Properties = new Dictionary<string, object>
        {
            { "id", rain.Id },
            { "town", rain.Town },
            { "state", rain.State },
            { "date", rain.Date },
            { "rainfallininches", rain.RainfallInInches }
        }
    };

    featureCollection.Features.Add(feature);
}

var json = JsonConvert.SerializeObject(featureCollection);
System.IO.File.WriteAllText(@"rainfall.json", json);


class Rainfall
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

public class UserProfile
{
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("login")]
    public string Login { get; set; }

    [JsonProperty("location")]
    public Point Location { get; set; }

    // More properties
}

// A class to represent a feature collection
[Serializable]
public class FeatureCollection
{
    [JsonProperty("type")]
    public string Type { get; set; } // The type of the feature collection
    [JsonProperty("features")]
    public List<Feature> Features { get; set; } // The list of features in the collection
}

// A class to represent a feature
[Serializable]
public class Feature
{
    [JsonProperty("type")]
    public string Type { get; set; } // The type of the feature
    [JsonProperty("geometry")]
    public Geometry Geometry { get; set; } // The geometry of the feature
    [JsonProperty("properties")]
    public Dictionary<string, object> Properties { get; set; } // The properties of the feature
}

// A class to represent a geometry
[Serializable]
public class Geometry
{
    [JsonProperty("type")]
    public string Type { get; set; } // The type of the geometry
    [JsonProperty("coordinates")]
    public List<object> Coordinates { get; set; } // The coordinates of the geometry
}