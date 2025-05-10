using Shimi;

namespace Shimi
{
    public class MatchedEntity
    {
        public int Id { get; set; }
        public double Score { get; set; }
        public List<Node> Path { get; set; } = [];
    }

    public class QueryResponse
    {
        public List<MatchedEntity> MatchedEntities { get; set; } = [];
        public int ApiCalls { get; set; }
    }
}
