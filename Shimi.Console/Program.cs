using Shimi.Shared;
using Spectre.Console;
using AnsiConsole = Spectre.Console.AnsiConsole;
using Color = Spectre.Console.Color;

namespace Shimi.Console
{
    internal class Program
    {
        private static string ShimiFilePath = "ShimiTree.json";
        private static Algo2 shimiEngine;

        static async Task Main(string[] args)
        {
            AnsiConsole.Write(
               new FigletText("SHIMI")
               .Color(Color.Red));

            AnsiConsole.WriteLine("");

            //var configuration = new ConfigurationBuilder()
            //    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            //    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true);
            //var config = configuration.Build();

            const string buildTree = "1.\tBuild agents tree";
            const string search = "2.\tAsk AI Assistant to accomplish a task for you.";
            const string exit = "3.\tExit this Application";

            while (true)
            {
                var selectedOption = AnsiConsole.Prompt(
                      new SelectionPrompt<string>()
                          .Title("Select an option to continue")
                          .PageSize(10)
                          .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                          .AddChoices([
                            buildTree, search, exit
                          ]));

                switch (selectedOption)
                {
                    case buildTree:
                        await BuildTree();
                        break;
                    case search:
                        await PerformSearch();
                        break;
                    case exit:
                        return;
                }
            }
        }

        private static async Task BuildTree()
        {
            List<string> roots = ["Wealth Management", "Personal Productivity", "Travel", "Shopping", "Legal"];

            var debugService = new DebugService(true, true);
            shimiEngine = new Algo2(debugService, roots, "We are categorizing non-human agents accomplising tasks. The categorization is based on the industry sector and sub sectors an agent belongs to.");
            var agents = DataManager<Agent>.LoadFromCsv<AgentCsvMap>("Data.Agents.csv");

            var start = DateTime.Now;

            debugService.WriteLine($"Starting at: {DateTime.Now}");
            foreach (var agent in agents)
            {
                debugService.WriteLine($"Adding Agent {agents.IndexOf(agent)}, {agent.AgentName}");

                await shimiEngine.AddEntity(agent);
            }

            shimiEngine.SaveTree("ShimiTree.json");
            File.WriteAllText("shimiTree.txt", $"Processed {agents.Count} in {(DateTime.Now - start).TotalSeconds} secs");
            File.AppendAllText("shimiTree.txt", shimiEngine.PrintTree());
            
            AnsiConsole.MarkupLine($"Uploaded [green]{agents.Count}[/] agent(s).");
            AnsiConsole.WriteLine(ShimiFilePath);
        }

        private static async Task PerformSearch()
        {
            if (shimiEngine == null)
            {
                List<string> roots = ["Wealth Management", "Personal Productivity", "Travel", "Shopping", "Legal"];
                var debugService = new DebugService(true, true);
                shimiEngine = new Algo2(debugService, roots, "We are categorizing non-human agents accomplising tasks. The categorization is based on the industry sector and sub sectors an agent belongs to.");
                shimiEngine.Load(ShimiFilePath);
            }

            string userQuery = AnsiConsole.Prompt(
                new TextPrompt<string>("Provide the task description, hit enter when ready.")
                    .PromptStyle("teal")
            );

            var agents = DataManager<Agent>.LoadFromCsv<AgentCsvMap>("Data.Agents.csv");

            await AnsiConsole.Status()
                .StartAsync("Processing...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Star);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    // Start timing
                    var start = DateTime.Now;

                    // This is the only part you want to measure
                    var response = await shimiEngine.Query(
                        "Group '{0} agents' has a high change of containing an agent to help accomplish this task: " + userQuery,
                        "Agent {0} can help accomplish this task: " + userQuery,
                        RequiredMatchLevel.Medium,
                        RequiredMatchLevel.High
                    );

                    // Stop timing
                    var elapsed = DateTime.Now - start;

                    // Now print elapsed time inside spinner block (light color)
                    AnsiConsole.MarkupLine($"[grey]Query completed in {elapsed.TotalSeconds:F2} seconds.[/]");
                    AnsiConsole.MarkupLine($"[grey]Made {response.ApiCalls} API calls to GPT.[/]");

                    var selectedAgentIds = response.MatchedEntities.Select(me => me.Id).ToList();
                    var selectedAgents = selectedAgentIds.Select(id => agents[id]);

                    // Optional: Tiny delay to keep the spinner alive for smooth UX
                    await Task.Delay(200);

                    AnsiConsole.WriteLine("");
                    AnsiConsole.Write(new Rule($"[silver]AI Assistant Response[/]") { Justification = Justify.Center });
                    AnsiConsole.MarkupLine(string.Join(',', selectedAgents.Select(e => e.Concept)));
                    AnsiConsole.WriteLine("");
                    AnsiConsole.WriteLine("");
                    AnsiConsole.Write(new Rule($"[yellow]****[/]") { Justification = Justify.Center });
                    AnsiConsole.WriteLine("");
                });

        }
    }
}