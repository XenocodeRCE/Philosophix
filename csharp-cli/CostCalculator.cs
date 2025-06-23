using System;
using System.Collections.Generic;
using System.Linq;

public class CostCalculator
{
    // Tarifs GPT-4o-mini par million de tokens (en USD)
    private const decimal INPUT_COST_PER_MILLION = 0.15m;
    private const decimal CACHED_INPUT_COST_PER_MILLION = 0.075m;
    private const decimal OUTPUT_COST_PER_MILLION = 0.60m;

    private List<RequestCost> _requests = new List<RequestCost>();

    public class RequestCost
    {
        public string Type { get; set; } = "";
        public int InputTokens { get; set; }
        public int CachedInputTokens { get; set; }
        public int OutputTokens { get; set; }
        public decimal Cost { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Calcule le coût d'une requête GPT
    /// </summary>
    public decimal CalculateRequestCost(int inputTokens, int outputTokens, int cachedInputTokens = 0)
    {
        decimal inputCost = (inputTokens * INPUT_COST_PER_MILLION) / 1_000_000m;
        decimal cachedInputCost = (cachedInputTokens * CACHED_INPUT_COST_PER_MILLION) / 1_000_000m;
        decimal outputCost = (outputTokens * OUTPUT_COST_PER_MILLION) / 1_000_000m;

        return inputCost + cachedInputCost + outputCost;
    }

    /// <summary>
    /// Ajoute une requête au tracker de coûts
    /// </summary>
    public void AddRequest(string type, int inputTokens, int outputTokens, int cachedInputTokens = 0)
    {
        var cost = CalculateRequestCost(inputTokens, outputTokens, cachedInputTokens);
        
        _requests.Add(new RequestCost
        {
            Type = type,
            InputTokens = inputTokens,
            CachedInputTokens = cachedInputTokens,
            OutputTokens = outputTokens,
            Cost = cost
        });

        // Afficher le coût de cette requête
        Console.WriteLine($"💰 Coût requête {type}: ${cost:F6} ({inputTokens} in + {outputTokens} out tokens)");
    }

    /// <summary>
    /// Affiche le résumé des coûts
    /// </summary>
    public void DisplayCostSummary()
    {
        if (!_requests.Any())
        {
            Console.WriteLine("Aucune requête enregistrée.");
            return;
        }

        Console.WriteLine("\n" + new string('═', 60));
        Console.WriteLine("💰 RÉSUMÉ DES COÛTS DE CORRECTION");
        Console.WriteLine(new string('═', 60));

        var totalCost = _requests.Sum(r => r.Cost);
        var totalInputTokens = _requests.Sum(r => r.InputTokens);
        var totalOutputTokens = _requests.Sum(r => r.OutputTokens);
        var totalCachedInputTokens = _requests.Sum(r => r.CachedInputTokens);

        Console.WriteLine($"📊 Nombre de requêtes: {_requests.Count}");
        Console.WriteLine($"📥 Total tokens d'entrée: {totalInputTokens:N0}");
        Console.WriteLine($"📤 Total tokens de sortie: {totalOutputTokens:N0}");
        if (totalCachedInputTokens > 0)
        {
            Console.WriteLine($"⚡ Total tokens cachés: {totalCachedInputTokens:N0}");
        }
        Console.WriteLine($"💵 COÛT TOTAL: ${totalCost:F6}");
        Console.WriteLine($"💶 COÛT TOTAL (EUR): €{totalCost * 0.92m:F6}"); // Approximation USD -> EUR

        Console.WriteLine("\n📋 Détail par type de requête:");
        var groupedRequests = _requests.GroupBy(r => r.Type)
            .Select(g => new {
                Type = g.Key,
                Count = g.Count(),
                TotalCost = g.Sum(r => r.Cost),
                AvgCost = g.Average(r => r.Cost)
            })
            .OrderByDescending(g => g.TotalCost);

        foreach (var group in groupedRequests)
        {
            Console.WriteLine($"  • {group.Type}: {group.Count}x requêtes, ${group.TotalCost:F6} (moy: ${group.AvgCost:F6})");
        }
    }

    /// <summary>
    /// Remet à zéro les coûts
    /// </summary>
    public void Reset()
    {
        _requests.Clear();
        Console.WriteLine("💰 Compteur de coûts remis à zéro.");
    }

    /// <summary>
    /// Estime les tokens d'un texte (approximation)
    /// </summary>
    public static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Approximation: 1 token ≈ 4 caractères en français
        // GPT utilise une tokenisation plus complexe, mais c'est une estimation
        return (int)Math.Ceiling(text.Length / 4.0);
    }
}
