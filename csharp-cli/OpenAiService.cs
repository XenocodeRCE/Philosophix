using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

public class OpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly double _temperature;
    public CostCalculator CostTracker { get; private set; }

    public OpenAiService()
    {
        _httpClient = new HttpClient();
        
        // Lire la configuration depuis appsettings.json
        var config = LoadConfiguration();
        _apiKey = config.OpenAI.ApiKey;
        _model = config.OpenAI.Model;
        _maxTokens = config.OpenAI.MaxTokens;
        _temperature = config.OpenAI.Temperature;
        
        if (string.IsNullOrEmpty(_apiKey) || _apiKey == "sk-")
        {
            throw new InvalidOperationException("Clé API OpenAI non configurée dans appsettings.json");
        }
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        CostTracker = new CostCalculator();
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

            return config ?? throw new InvalidOperationException("Impossible de désérialiser la configuration.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erreur lors du chargement de la configuration : {ex.Message}");
        }
    }public async Task<string> AskGptAsync(string system, string prompt, string requestType = "Requête")
    {
        var url = "https://api.openai.com/v1/chat/completions";

        // Estimer les tokens d'entrée
        var inputTokens = CostCalculator.EstimateTokens(system + prompt);        var requestData = new
        {
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = prompt }
            },
            model = _model,
            temperature = _temperature,
            max_tokens = _maxTokens,
            top_p = 1,
            stream = false,
            stop = (string?)null
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            
            // Extraire les informations de tokens de la réponse et calculer le coût
            try
            {
                var apiResponse = JsonSerializer.Deserialize<OpenAiApiResponse>(responseBody);
                var responseContent = apiResponse?.Choices?[0]?.Message?.Content ?? "";
                var outputTokens = CostCalculator.EstimateTokens(responseContent);
                
                // Ajouter le coût au tracker
                CostTracker.AddRequest(requestType, inputTokens, outputTokens);
            }
            catch
            {
                // En cas d'erreur, utiliser les estimations
                var outputTokens = CostCalculator.EstimateTokens("Erreur de réponse");
                CostTracker.AddRequest(requestType, inputTokens, outputTokens);
            }
            
            return responseBody;
        }
        else
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            return $"Erreur HTTP : {response.StatusCode} - {errorBody}";
        }
    }

    public EvaluationCompetence ParseEvaluationResponse(string apiResponse)
    {
        try
        {
            var response = JsonSerializer.Deserialize<OpenAiApiResponse>(apiResponse);
            if (response?.Choices?.Length > 0)
            {
                var content = response.Choices[0].Message?.Content;
                if (!string.IsNullOrEmpty(content))
                {
                    // Nettoyer la réponse des balises Markdown
                    var cleanJson = content.Replace("```json", "").Replace("```", "").Trim();
                    var evaluation = JsonSerializer.Deserialize<EvaluationApiResponse>(cleanJson);
                    
                    var result = new EvaluationCompetence();
                    result.Note = evaluation?.Note ?? 0;
                    result.Analyse = evaluation?.Analyse;
                    result.PointsForts = evaluation?.PointsForts;
                    result.PointsAmeliorer = evaluation?.PointsAmeliorer;
                    
                    return result;
                }
            }
            throw new Exception("Format de réponse invalide");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du parsing : {ex.Message}");
            Console.WriteLine($"Réponse brute : {apiResponse}");
            
            // Retourner une évaluation par défaut en cas d'erreur
            var errorResult = new EvaluationCompetence();
            errorResult.Note = 10;
            errorResult.Analyse = "Erreur lors de l'analyse automatique";
            errorResult.PointsForts = new List<string> { "Analyse non disponible" };
            errorResult.PointsAmeliorer = new List<string> { "Réessayer la correction" };
            
            return errorResult;
        }
    }

    public EvaluationFinaleApiResponse ParseEvaluationFinaleResponse(string apiResponse)
    {
        try
        {
            var response = JsonSerializer.Deserialize<OpenAiApiResponse>(apiResponse);
            if (response?.Choices?.Length > 0)
            {
                var content = response.Choices[0].Message?.Content;
                if (!string.IsNullOrEmpty(content))
                {
                    // Nettoyer la réponse des balises Markdown
                    var cleanJson = content.Replace("```json", "").Replace("```", "").Trim();
                    var evaluation = JsonSerializer.Deserialize<EvaluationFinaleApiResponse>(cleanJson);
                    
                    return evaluation ?? new EvaluationFinaleApiResponse
                    {
                        Appreciation = "Erreur lors de la génération de l'appréciation",
                        PointsForts = new List<string> { "Analyse non disponible" },
                        PointsAmeliorer = new List<string> { "Réessayer la correction" }
                    };
                }
            }
            throw new Exception("Format de réponse invalide");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du parsing de l'évaluation finale : {ex.Message}");
            Console.WriteLine($"Réponse brute : {apiResponse}");
            
            // Retourner une évaluation par défaut en cas d'erreur
            return new EvaluationFinaleApiResponse
            {
                Appreciation = "Erreur lors de la génération de l'appréciation automatique",
                PointsForts = new List<string> { "Analyse non disponible" },
                PointsAmeliorer = new List<string> { "Réessayer la correction" }
            };
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Classes pour désérialiser les réponses de l'API OpenAI
public class OpenAiApiResponse
{
    [JsonPropertyName("choices")]
    public Choice[]? Choices { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")]
    public Message? Message { get; set; }
}

public class Message
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

// Classes pour la configuration appsettings.json
public class AppSettings
{
    public OpenAISettings OpenAI { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
    public CorrectionSettings Correction { get; set; } = new();
}

public class OpenAISettings
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 8000;
    public double Temperature { get; set; } = 1.0;
}

public class DatabaseSettings
{
    public string DevoirsPath { get; set; } = "devoirs.json";
    public string CorrectionsPath { get; set; } = "devoirs_corrections.json";
}

public class CorrectionSettings
{
    public int MinimumCopyLength { get; set; } = 500;
    public int SeverityLevel { get; set; } = 3;
}
