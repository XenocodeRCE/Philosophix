using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Service pour interagir avec Ollama (modèles locaux)
/// Alternative gratuite et privée à OpenAI
/// </summary>
public class OllamaService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly double _temperature;

    public CostCalculator? CostTracker => null; // Ollama est gratuit
    public string ServiceName => "Ollama";
    public string ModelName => _model;

    public OllamaService()
    {
        _httpClient = new HttpClient();
        
        // Lire la configuration depuis appsettings.json
        var config = LoadConfiguration();
        _baseUrl = config.Ollama.BaseUrl;
        _model = config.Ollama.Model;
        _temperature = config.Ollama.Temperature;
        
        // Timeout plus long pour les modèles locaux qui peuvent être plus lents
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        
        Console.WriteLine($"🤖 Utilisation d'Ollama : {_model} sur {_baseUrl}");
    }

    private AppSettings LoadConfiguration()
    {
        try
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

            if (config?.Ollama == null)
            {
                throw new InvalidOperationException("Configuration Ollama manquante dans appsettings.json");
            }

            return config;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur lors du chargement de la configuration Ollama : {ex.Message}");
        }
    }

    public async Task<string> AskAsync(string systemMessage, string userPrompt, string operationType)
    {
        try
        {
            Console.WriteLine($"📡 {operationType} - Envoi vers Ollama ({_model})...");
            
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userPrompt }
                },
                stream = false,
                options = new
                {
                    temperature = _temperature,
                    num_predict = 8000 // Équivalent à MaxTokens
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/chat", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erreur Ollama {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Parser la réponse Ollama
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            if (root.TryGetProperty("message", out var message))
            {
                if (message.TryGetProperty("content", out var messageContent))
                {
                    var result = messageContent.GetString() ?? "";
                    Console.WriteLine($"✅ {operationType} - Réponse Ollama reçue ({result.Length} caractères)");
                    return result;
                }
            }

            throw new Exception("Format de réponse Ollama inattendu");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Erreur de connexion à Ollama. Vérifiez qu'Ollama est démarré sur {_baseUrl}. Erreur: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"Timeout lors de la requête Ollama (modèle peut-être lent ou non disponible). Erreur: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur lors de la requête Ollama : {ex.Message}");
        }
    }
}

/// <summary>
/// Modèles de configuration étendus pour supporter Ollama
/// </summary>
public class OllamaSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.1:8b";
    public double Temperature { get; set; } = 1.0;
}
