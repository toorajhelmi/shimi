using Newtonsoft.Json;
using Rag;

namespace CosmosAgentGuide
{
    internal static class Utility
    {
        public static List<Agent> ParseDocuments(string Folderpath)
        {
            List<Agent> ret = new();

            Directory.GetFiles(Folderpath).ToList().ForEach(f =>
                {
                    var jsonString= System.IO.File.ReadAllText(f);
                    Agent agent = JsonConvert.DeserializeObject<Agent>(jsonString);
                    agent.id = agent.Name.ToLower().Replace(" ", "");
                    ret.Add(agent);

                }
            );


            return ret;

        }
    }
}
