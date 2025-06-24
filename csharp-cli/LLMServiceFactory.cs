using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Factory pour créer le service LLM approprié selon la configuration
/// </summary>
public static class LLMServiceFactory
{
    /// <summary>
    /// Crée une instance du service LLM configuré
    /// </summary>
    /// <returns>Service LLM (OpenAI ou Ollama)</returns>
    public static ILLMService CreateService()
    {
        try
        {
            var config = LoadConfiguration();
            var provider = config.LLM?.Provider?.ToLower();

            switch (provider)
            {
                case "openai":
                    Console.WriteLine("🔧 Configuration: Utilisation d'OpenAI");
                    return new OpenAiService();

                case "ollama":
                    Console.WriteLine("🔧 Configuration: Utilisation d'Ollama (local)");
                    return new OllamaService();

                default:
                    Console.WriteLine($"⚠️  Provider '{provider}' non reconnu, utilisation d'OpenAI par défaut");
                    return new OpenAiService();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erreur lors de la création du service LLM : {ex.Message}");
            Console.WriteLine("📋 Vérifiez votre configuration dans appsettings.json");
            throw;
        }
    }

    /// <summary>
    /// Affiche des informations sur la configuration LLM active
    /// </summary>
    public static void AfficherInfoConfiguration()
    {
        try
        {
            var config = LoadConfiguration();
            var provider = config.LLM?.Provider ?? "OpenAI";

            Console.WriteLine("\n" + new string('═', 50));
            Console.WriteLine("🤖 CONFIGURATION LLM ACTIVE");
            Console.WriteLine(new string('═', 50));
            Console.WriteLine($"Provider : {provider}");

            if (provider.ToLower() == "openai")
            {
                Console.WriteLine($"Modèle : {config.OpenAI?.Model ?? "gpt-4o-mini"}");
                Console.WriteLine($"Max Tokens : {config.OpenAI?.MaxTokens ?? 8000}");
                Console.WriteLine($"Température : {config.OpenAI?.Temperature ?? 1.0}");
                Console.WriteLine("💰 Coût : Payant (suivi automatique)");
            }
            else if (provider.ToLower() == "ollama")
            {
                Console.WriteLine($"Modèle : {config.Ollama?.Model ?? "llama3.1:8b"}");
                Console.WriteLine($"URL : {config.Ollama?.BaseUrl ?? "http://localhost:11434"}");
                Console.WriteLine($"Température : {config.Ollama?.Temperature ?? 1.0}");
                Console.WriteLine("🆓 Coût : Gratuit (local)");
            }

            Console.WriteLine(new string('═', 50));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Impossible d'afficher la configuration : {ex.Message}");
        }
    }

    /// <summary>
    /// Vérifie si Ollama est accessible si configuré
    /// </summary>
    public static async Task<bool> VerifierConnexionOllamaAsync()
    {
        try
        {
            var config = LoadConfiguration();
            if (config.LLM?.Provider?.ToLower() == "ollama")
            {
                var ollamaService = new OllamaService();
                // Test simple de connexion
                await ollamaService.AskAsync("Tu es un assistant.", "Dis juste 'OK' pour tester la connexion.", "Test connexion");
                return true;
            }
            return true; // OpenAI ou autre provider
        }
        catch
        {
            return false;
        }
    }

    private static AppSettings LoadConfiguration()
    {
        var configPath = "appsettings.json";
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Le fichier {configPath} est introuvable.");
        }

        var jsonContent = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<AppSettings>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return config ?? throw new InvalidOperationException("Impossible de désérialiser la configuration.");
    }
}

/// <summary>
/// Configuration étendue pour supporter le choix de provider
/// </summary>
public class LLMSettings
{
    public string Provider { get; set; } = "OpenAI";
}
