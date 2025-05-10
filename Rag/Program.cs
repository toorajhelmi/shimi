using Azure;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Rag;
using Shimi.Shared;
using Spectre.Console;
using Console = Spectre.Console.AnsiConsole;

namespace CosmosAgentGuide
{
    internal class Program
    {
        static OpenAIService openAIEmbeddingService = null;
        static CosmosDBMongoVCoreService cosmosMongoVCoreService = null;

        private static readonly string vectorSearchIndex = "vectorSearchIndex";
        static async Task Main(string[] args)
        {
            AnsiConsole.Write(
               new FigletText("RAG")
               .Color(Color.Red));

            Console.WriteLine("");

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true);

            var config = configuration.Build();
            
            InitCosmosDBService(config);
            
            const string cosmosUpload = "1.\tUpload agents(s) to Cosmos DB";
            const string vectorize = "2.\tVectorize the agent(s) and store it in Cosmos DB";
            const string search = "3.\tAsk AI Assistant to accomplish a task for you.";
            const string exit = "4.\tExit this Application";

            while (true)
            {
                var selectedOption = AnsiConsole.Prompt(
                      new SelectionPrompt<string>()
                          .Title("Select an option to continue")
                          .PageSize(10)
                          .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                          .AddChoices(new[] {
                            cosmosUpload,vectorize ,search, exit
                          }));

                switch (selectedOption)
                {
                    case cosmosUpload:
                        UploadAgents(config);
                        break;
                    case vectorize:
                        GenerateEmbeddings(config);                        
                        break;
                    case search:
                        PerformSearch(config);
                        break;
                    case exit:
                        return;                        
                }
            }                  
        }

        private static OpenAIService InitOpenAIService(IConfiguration config)
        {
            string endpoint = config["OpenAIEndpoint"];
            string key = config["OpenAIKey"];
            string embeddingDeployment = config["OpenAIEmbeddingDeployment"];
            string completionsDeployment = config["OpenAIcompletionsDeployment"];
            string maxToken = config["OpenAIMaxToken"];
            
            return new OpenAIService(endpoint, key, embeddingDeployment, completionsDeployment,maxToken);
        }

        private static void InitCosmosDBService( IConfiguration config)
        {
            
            long agentsWithEmbedding = 0;
            long agentsWithNoEmbedding = 0;

            AnsiConsole.Status()
                .Start("Processing...", ctx =>
                {
                    
                    ctx.Spinner(Spinner.Known.Star);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    ctx.Status("Creating Cosmos DB Client ..");
                    if (InitVCoreMongoService(config) == true)
                    {

                        ctx.Status("Getting Agent Stats");
                        agentsWithEmbedding = cosmosMongoVCoreService.GetAgentCountAsync(true).GetAwaiter().GetResult();
                        agentsWithNoEmbedding = cosmosMongoVCoreService.GetAgentCountAsync(false).GetAwaiter().GetResult();
                    }                                                
                });

            AnsiConsole.MarkupLine($"We have [green]{agentsWithEmbedding}[/] vectorized agents(s) and [red]{agentsWithNoEmbedding}[/] non vectorized agent(s).");
            Console.WriteLine("");
        }

        private static bool InitVCoreMongoService(IConfiguration config)
        {            

            string vcoreConn = config["MongoVcoreConnection"];
            string vCoreDB = config["MongoVcoreDatabase"];
            string vCoreColl = config["MongoVcoreCollection"];
            string maxResults = config["maxVectorSearchResults"];

            cosmosMongoVCoreService = new CosmosDBMongoVCoreService(vcoreConn, vCoreDB, vCoreColl, maxResults);

            return true;
        }

        private static void UploadAgents(IConfiguration config)
        {
            long agentsWithEmbedding = 0;
            long agentseWithNoEmbedding = 0;

            List<Agent> agents = null;

            AnsiConsole.Status()
               .Start("Processing...", ctx =>
               {
                   ctx.Spinner(Spinner.Known.Star);
                   ctx.SpinnerStyle(Style.Parse("green"));

                   ctx.Status("Parsing Agent files..");
                   agents = DataManager<Agent>.LoadFromCsv<AgentCsvMap>("Data.Agents.csv").DistinctBy(ai => ai.Name).Select(ai => new Agent
                   {
                       Name = ai.Name,
                       Description = ai.Description
                   }).ToList();             
                  

                   ctx.Status($"Uploading Agent(s)..");
                   foreach (Agent agent in agents)
                   {
                       cosmosMongoVCoreService.UpsertVectorAsync(agent).GetAwaiter().GetResult();
                   }

                   ctx.Status("Getting Updated Agent Stats");
                   agentsWithEmbedding = cosmosMongoVCoreService.GetAgentCountAsync(true).GetAwaiter().GetResult();
                   agentseWithNoEmbedding = cosmosMongoVCoreService.GetAgentCountAsync(false).GetAwaiter().GetResult();

               });

            AnsiConsole.MarkupLine($"Uploaded [green]{agents.Count}[/] agent(s).We have [teal]{agentsWithEmbedding}[/] vectorized agent(s) and [red]{agentseWithNoEmbedding}[/] non vectorized agent(s).");
            Console.WriteLine("");

        }

        private static void PerformSearch(IConfiguration config)
        {

            string chatCompletion = string.Empty;

            string userQuery = Console.Prompt(
                new TextPrompt<string>("Provide the task description, hit enter when ready.")
                    .PromptStyle("teal")
            );


            AnsiConsole.Status()
               .Start("Processing...", ctx =>
               {
                   ctx.Spinner(Spinner.Known.Star);
                   ctx.SpinnerStyle(Style.Parse("green"));

                   if (openAIEmbeddingService == null)
                   {
                       ctx.Status("Connecting to Open AI Service..");
                       openAIEmbeddingService = InitOpenAIService(config);
                   }

                   var start = DateTime.Now;

                   if (cosmosMongoVCoreService == null)
                   {
                       ctx.Status("Connecting to Azure Cosmos DB for MongoDB vCore..");
                       InitVCoreMongoService(config);

                       ctx.Status("Checking for Index in Azure Cosmos DB for MongoDB vCore..");
                       if (cosmosMongoVCoreService.CheckIndexIfExists(vectorSearchIndex) == false)
                       {
                           AnsiConsole.WriteException(new Exception("Vector Search Index not Found, Please build the index first."));
                           return;
                       }
                   }

                   ctx.Status("Converting User Query to Vector..");
                   var embeddingVector = openAIEmbeddingService.GetEmbeddingsAsync(userQuery).GetAwaiter().GetResult();

                   ctx.Status("Performing Vector Search from Cosmos DB (RAG pattern)..");
                   var retrivedDocs = cosmosMongoVCoreService.VectorSearchAsync(embeddingVector).GetAwaiter().GetResult();

                   ctx.Status($"Priocessing {retrivedDocs.Count} to generate Chat Response  using OpenAI Service..");

                   string retrivedReceipeNames = string.Empty;

                   foreach (var agent in retrivedDocs)
                   {
                       agent.Embedding = null; //removing embedding to reduce tokens during chat completion
                       retrivedReceipeNames += ", " + agent.Name; //to dispay agents submitted for Completion
                   }

                   ctx.Status($"Processing '{retrivedReceipeNames}' to generate Completion using OpenAI Service..");

                   (string completion, int promptTokens, int completionTokens) = openAIEmbeddingService.GetChatCompletionAsync(userQuery, JsonConvert.SerializeObject(retrivedDocs)).GetAwaiter().GetResult();
                   chatCompletion = completion;

                   var elapsed = DateTime.Now - start;

                   // Now print elapsed time inside spinner block (light color)
                   AnsiConsole.MarkupLine($"[grey]Query completed in {elapsed.TotalSeconds:F2} seconds.[/]");

                   Console.WriteLine("");
                   Console.Write(new Rule($"[silver]AI Assistant Response[/]") { Justification = Justify.Center });
                   AnsiConsole.MarkupLine(chatCompletion);
                   Console.WriteLine("");
                   Console.WriteLine("");
                   Console.Write(new Rule($"[yellow]****[/]") { Justification = Justify.Center });
                   Console.WriteLine("");
               });
        }

        private static void GenerateEmbeddings(IConfiguration config)
        {
            long agentsWithEmbedding = 0;
            long agentsWithNoEmbedding = 0;
            long agentCount = 0;

            AnsiConsole.Status()
               .Start("Processing...", ctx =>
               {
                   ctx.Spinner(Spinner.Known.Star);
                   ctx.SpinnerStyle(Style.Parse("green"));

                   if (openAIEmbeddingService == null)
                   {
                       ctx.Status("Connecting to Open AI Service..");
                       openAIEmbeddingService = InitOpenAIService(config);
                   }

                   if (cosmosMongoVCoreService == null)
                   {
                       ctx.Status("Connecting to VCore Mongo..");
                       InitVCoreMongoService(config);
                   }

                   ctx.Status("Building VCore Index..");
                   cosmosMongoVCoreService.CreateVectorIndexIfNotExists(vectorSearchIndex);

                   ctx.Status("Getting agent(s) to vectorize..");
                   var Agents = cosmosMongoVCoreService.GetAgentsToVectorizeAsync().GetAwaiter().GetResult();

                   foreach (var agent in Agents)
                   {
                       agentCount++;
                       ctx.Status($"Vectorizing Agent #{agentCount}..");
                       var embeddingVector = openAIEmbeddingService.GetEmbeddingsAsync(JsonConvert.SerializeObject(agent)).GetAwaiter().GetResult();
                       agent.Embedding = embeddingVector.ToList();
                   }

                   ctx.Status($"Indexing {Agents.Count} document(s) on Azure Cosmos DB for MongoDB vCore..");
                   foreach (var agent in Agents)
                   {
                       cosmosMongoVCoreService.UpsertVectorAsync(agent).GetAwaiter().GetResult();
                   }

                   ctx.Status("Getting Updated Agent Stats");
                   agentsWithEmbedding = cosmosMongoVCoreService.GetAgentCountAsync(true).GetAwaiter().GetResult();
                   agentsWithNoEmbedding = cosmosMongoVCoreService.GetAgentCountAsync(false).GetAwaiter().GetResult();

               });

            AnsiConsole.MarkupLine($"Vectorized [teal]{agentCount}[/] agent(s). We have [green]{agentsWithEmbedding}[/] vectorized agent(s) and [red]{agentsWithNoEmbedding}[/] non vectorized agent(s).");
            Console.WriteLine("");
        }
    }
}