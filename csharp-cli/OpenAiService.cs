using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

public class OpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAiService()
    {
        _httpClient = new HttpClient();
        // Mettez ici votre clef API OpenAI
        _apiKey = "sk-"; 
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> AskGptAsync(string system, string prompt)
    {
        var url = "https://api.openai.com/v1/chat/completions";

        var requestData = new
        {
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = prompt }
            },
            model = "gpt-4o-mini",
            temperature = 1,
            max_tokens = 8000,
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
