using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public class JsonDatabaseService
{
    private readonly string _filePath;

    public JsonDatabaseService(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<List<Devoir>> LireDevoirsAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new List<Devoir>();
        }

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<Devoir>>(json) ?? new List<Devoir>();
    }

    public async Task SauvegarderDevoirsAsync(List<Devoir> devoirs)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(devoirs, options);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<List<Correction>> LireCorrectionsAsync()
    {
        var correctionsFilePath = Path.ChangeExtension(_filePath, null) + "_corrections.json";
        if (!File.Exists(correctionsFilePath))
        {
            return new List<Correction>();
        }

        var json = await File.ReadAllTextAsync(correctionsFilePath);
        return JsonSerializer.Deserialize<List<Correction>>(json) ?? new List<Correction>();
    }

    public async Task SauvegarderCorrectionsAsync(List<Correction> corrections)
    {
        var correctionsFilePath = Path.ChangeExtension(_filePath, null) + "_corrections.json";
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(corrections, options);
        await File.WriteAllTextAsync(correctionsFilePath, json);
    }
}
