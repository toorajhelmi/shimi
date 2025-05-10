using CsvHelper.Configuration;
using Shimi.Shared;

namespace Shimi.Samples.Agents
{
    public class Agent : Entity
    {
        public string AgentName { get; set; }
        public string Description { get; set; }
        public string Skills { get; set; }
        public override string Concept => AgentName;
        public override string Explantion => Description;
    }

    public class AgentCsvMap : ClassMap<Agent>
    {
        public AgentCsvMap()
        {
            Map(m => m.AgentName).Name("Name");
            Map(m => m.Description);
            //Map(m => m.Skills);
        }
    }
}
