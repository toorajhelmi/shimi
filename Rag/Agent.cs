using CsvHelper.Configuration;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Rag
{

    [BsonIgnoreExtraElements]
    public class Agent
    {
        [BsonId]
        [JsonProperty("id")]
        public string? id { get; set; }

        public string? Name { get; set; }
        public string? Description { get; set; }

        [BsonElement("embedding")]
        public List<float>? Embedding { get; set; }
    }

    public class AgentCsvMap : ClassMap<Agent>
    {
        public AgentCsvMap()
        {
            Map(m => m.Name);
            Map(m => m.Description);
           // Map(m => m.Skills);
        }
    }
}
