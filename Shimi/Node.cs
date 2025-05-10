using Shimi.Shared;

namespace Shimi
{
    public class Node
    {
        public string Summary { get; set; }         
        public string Details { get; set; }
        public string Description => Summary + (Details == null ? "" : $" ({Details})");
        public List<Node> Children { get; set; } = [];       
        public Node Parent { get; set; }
        public int? EntityId { get; set; }

        public Node(string summary, string details = null)
        {
            Summary = summary;
            Details = details;
        }
    }
}
