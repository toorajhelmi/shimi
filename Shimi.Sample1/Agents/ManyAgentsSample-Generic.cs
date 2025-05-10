namespace Shimi.Samples.Agents
{
    public class ManyAgentsSample_Generic
    {
        public async Task BuildTree()
        {
            List<string> roots = [
                "All"
            ];

            var debugService = new DebugService();
            var engine = new Algo2(debugService, roots, "We are categorizing non-human agents accomplising mathematic tasks. The categorization is based on the type of  opereration the agent performs.");

            var agents = LoadAgents();
            
            foreach (var agent in agents)
            {
                await engine.AddEntity(agent);

                debugService.Highlight($"Adding Agent {agents.IndexOf(agent)}, {agent.AgentName}");
                debugService.WriteLine(engine.PrintTree());
            }

            File.WriteAllText("agents-gen.txt", engine.PrintTree());
        }
        private static List<Agent> LoadAgents()
        {
            return [
                new Agent { AgentName = "Agent greets new users" },
                new Agent { AgentName = "Agent greets new users" },
                ];
        }
    }
}
