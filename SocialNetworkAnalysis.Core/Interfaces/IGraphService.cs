using SocialNetworkAnalysis.Core.Models;

public interface IGraphService
{
    Graph CurrentGraph { get; }

    void LoadGraph(string path);

    void AddNode(Node node);
    void AddNode(Node node, List<int> neighborIds);
    void AddNodeWithPersistence(Node node, List<int> neighborIds, string filePath);

    void UpdateNode(Node node);
    void UpdateNode(Node node, List<int> neighborIds);
    void UpdateNodeWithPersistence(Node node, List<int> neighborIds, string filePath);

    void RemoveNode(int nodeId);
    void RemoveNodeWithPersistence(int nodeId, string filePath);
    void RemoveEdge(int sourceId, int targetId);

    void AddEdge(Node source, Node target);
    void AddEdge(Node source, Node target, double weight);

    string ExportToJson();
    void ImportFromJson(string jsonContent);

    double[,] GenerateAdjacencyMatrix();
    void ClearGraph();
}
