using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class Devoir
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("titre")]
    public string? Titre { get; set; }

    [JsonPropertyName("enonce")]
    public string? Enonce { get; set; }

    [JsonPropertyName("contenu")]
    public string? Contenu { get; set; }

    [JsonPropertyName("date_creation")]
    public DateTime DateCreation { get; set; }

    [JsonPropertyName("bareme")]
    public Bareme? Bareme { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("type_bac")]
    public string? TypeBac { get; set; }
}

public class Bareme
{
    [JsonPropertyName("competences")]
    public List<Competence>? Competences { get; set; }
}

public class Competence
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nom")]
    public string? Nom { get; set; }

    [JsonPropertyName("criteres")]
    public List<string>? Criteres { get; set; }
}
