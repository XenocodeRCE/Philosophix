using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class Correction
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("devoir_id")]
    public int DevoirId { get; set; }

    [JsonPropertyName("note")]
    public decimal Note { get; set; }

    [JsonPropertyName("appreciation")]
    public string? Appreciation { get; set; }

    [JsonPropertyName("points_forts")]
    public List<string>? PointsForts { get; set; }

    [JsonPropertyName("points_ameliorer")]
    public List<string>? PointsAmeliorer { get; set; }

    [JsonPropertyName("competences")]
    public List<EvaluationCompetence>? Competences { get; set; }

    [JsonPropertyName("copie")]
    public string? Copie { get; set; }

    [JsonPropertyName("date_correction")]
    public DateTime DateCorrection { get; set; }
}

public class EvaluationCompetence
{
    [JsonPropertyName("nom")]
    public string? Nom { get; set; }

    [JsonPropertyName("note")]
    public decimal Note { get; set; }

    [JsonPropertyName("analyse")]
    public string? Analyse { get; set; }

    [JsonPropertyName("points_forts")]
    public List<string>? PointsForts { get; set; }

    [JsonPropertyName("points_ameliorer")]
    public List<string>? PointsAmeliorer { get; set; }
}

// Classes pour désérialiser les réponses de l'API OpenAI
public class EvaluationApiResponse
{
    [JsonPropertyName("note")]
    public decimal Note { get; set; }

    [JsonPropertyName("analyse")]
    public string? Analyse { get; set; }

    [JsonPropertyName("points_forts")]
    public List<string>? PointsForts { get; set; }

    [JsonPropertyName("points_ameliorer")]
    public List<string>? PointsAmeliorer { get; set; }
}

public class EvaluationFinaleApiResponse
{
    [JsonPropertyName("appreciation")]
    public string? Appreciation { get; set; }

    [JsonPropertyName("points_forts")]
    public List<string>? PointsForts { get; set; }

    [JsonPropertyName("points_ameliorer")]
    public List<string>? PointsAmeliorer { get; set; }
}
