using Shimi.Shared;

namespace Shimi.Samples.Agents 
{
    public class RagCompareSample
    {
        private List<string> roots = ["Finance", "Productivity", "Travel", "Shopping", "Lifestyle", "Legal"];
        private DebugService debugService = new DebugService(true, true);
        private Algo2 engine;

        public RagCompareSample()
        {
            engine = new Algo2(debugService, roots, "We are categorizing non-human agents accomplising tasks. The categorization is based on the industry sector and sub sectors an agent belongs to.");
        }

        public async Task BuildTree()
        {
            var agents = DataManager<Agent>.LoadFromCsv<AgentCsvMap>("Data.Agents.csv");

            foreach (var agent in agents)
            {
                agent.Id = agents.IndexOf(agent);
                debugService.Highlight($"Adding Agent {agents.IndexOf(agent)}, {agent.AgentName}");
                
                await engine.AddEntity(agent);
                debugService.WriteLine(engine.PrintTree());
            }

            File.WriteAllText($"rag-comp-agents-{DateTime.Now:yyyyMMdd_HHmmss}.txt", engine.PrintTree());
            engine.SaveTree("ShimiTree.json");
        }

        public async Task Query()
        {
            var agents = DataManager<Agent>.LoadFromCsv<AgentCsvMap>("Data.Agents.csv");
            agents.ForEach(a => a.Id = agents.IndexOf(a));

            var task = "'Create a budget-aware holiday shopping list generator that prioritizes deals across child-specific toy preferences, gift category caps, and flash-sale timing.'";
            engine.Load("ShimiTree.json");
            var response = await engine.Query("Group '{0} agents' has a high change of containing an agent to help accomplish this task: " + task,
                "Agent {0} can help accomplish this task: " + task, RequiredMatchLevel.Medium, RequiredMatchLevel.High);

            Console.WriteLine($"API Call Count: {response.ApiCalls}");

            if (response.MatchedEntities.Any())
            {
                foreach (var match in response.MatchedEntities)
                {
                    Console.WriteLine($"{agents.First(a => a.Id == match.Id)}, ({match.Score}), |{string.Join('/', match.Path.Select(n => n.Summary))}|");
                }
            }
            else
            {
                Console.WriteLine("No matching agents found.");
            }
        }
    }
}
