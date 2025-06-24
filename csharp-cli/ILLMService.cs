using System.Threading.Tasks;

/// <summary>
/// Interface commune pour les services de modèles de langage (LLM)
/// Permet de supporter OpenAI, Ollama, ou d'autres fournisseurs
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Envoie une requête au modèle de langage avec un prompt système et utilisateur
    /// </summary>
    /// <param name="systemMessage">Message système définissant le contexte et le rôle</param>
    /// <param name="userPrompt">Prompt de l'utilisateur</param>
    /// <param name="operationType">Type d'opération pour le tracking des coûts</param>
    /// <returns>Réponse du modèle</returns>
    Task<string> AskAsync(string systemMessage, string userPrompt, string operationType);

    /// <summary>
    /// Calculateur de coûts (peut être null pour les services gratuits comme Ollama)
    /// </summary>
    CostCalculator? CostTracker { get; }

    /// <summary>
    /// Nom du service LLM utilisé
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Modèle utilisé par le service
    /// </summary>
    string ModelName { get; }
}
