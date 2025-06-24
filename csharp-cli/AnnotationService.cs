using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

/// <summary>
/// Service d'annotation automatique de copies, inspiré du script PHP d'origine
/// </summary>
public class AnnotationService
{
    private readonly ILLMService _llmService;
    private readonly JsonDatabaseService _dbService;

    public AnnotationService(ILLMService llmService, JsonDatabaseService dbService)
    {
        _llmService = llmService;
        _dbService = dbService;
    }/// <summary>
    /// Génère des annotations pour une copie déjà corrigée
    /// </summary>
    public async Task<AnnotationResponse> GenererAnnotationsAsync(Correction correction)
    {
        if (correction == null || string.IsNullOrEmpty(correction.Copie))
        {
            throw new ArgumentException("La correction et la copie ne peuvent pas être vides");
        }

        // Préparer les points forts et points à améliorer sous forme de chaînes
        var pointsForts = correction.PointsForts?.Any() == true 
            ? string.Join(", ", correction.PointsForts) 
            : "Aucun point fort spécifique identifié";

        var pointsAmeliorer = correction.PointsAmeliorer?.Any() == true 
            ? string.Join(", ", correction.PointsAmeliorer) 
            : "Aucun point d'amélioration spécifique identifié";        // Construire un prompt plus direct et efficace
        var prompt = $@"Analysez cette copie de philosophie et identifiez exactement 5 passages à commenter de manière constructive.

COPIE D'ÉLÈVE :
{correction.Copie}

INFORMATIONS DE CORRECTION :
- Note : {correction.Note:F1}/20
- Points forts : {pointsForts}
- Points à améliorer : {pointsAmeliorer}

INSTRUCTIONS :
1. Sélectionnez 5 passages différents de la copie (citations exactes)
2. Pour chaque passage, donnez un commentaire constructif
3. Variez les types : structure, argumentation, concepts, style, etc.

RÉPONDEZ UNIQUEMENT au format JSON suivant (sans texte avant ou après) :
{{
    ""annotations"": [
        {{
            ""passage"": ""première citation exacte du texte"",
            ""commentaire"": ""commentaire constructif""
        }},
        {{
            ""passage"": ""deuxième citation exacte du texte"",
            ""commentaire"": ""autre commentaire constructif""
        }},
        {{
            ""passage"": ""troisième citation exacte du texte"",
            ""commentaire"": ""encore un commentaire""
        }},
        {{
            ""passage"": ""quatrième citation exacte du texte"",
            ""commentaire"": ""commentaire pédagogique""
        }},
        {{
            ""passage"": ""cinquième citation exacte du texte"",
            ""commentaire"": ""dernier commentaire""
        }}
    ]
}}";var systemMessage = @"Vous êtes un professeur de philosophie expérimenté. Votre tâche est d'annoter des copies d'élèves en identifiant des passages spécifiques et en fournissant des commentaires constructifs. Répondez UNIQUEMENT avec du JSON valide, sans texte supplémentaire.";

        string response = "";
        string cleanResponse = "";
        
        try
        {
            Console.WriteLine("\n🔍 Génération des annotations...");
            
            response = await _llmService.AskAsync(systemMessage, prompt, "Annotation");
            
            if (string.IsNullOrEmpty(response))
            {
                throw new Exception("Réponse vide de l'API OpenAI");
            }            Console.WriteLine($"📄 Réponse brute reçue (début) : {response.Substring(0, Math.Min(response.Length, 200))}...");

            // Extraire le contenu du message depuis la réponse OpenAI
            cleanResponse = ExtraireContenuMessage(response);
            
            Console.WriteLine($"🔧 Contenu extrait : {cleanResponse.Substring(0, Math.Min(cleanResponse.Length, 200))}...");

            // Tenter de désérialiser la réponse JSON
            var annotationResponse = JsonSerializer.Deserialize<AnnotationResponse>(cleanResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });            if (annotationResponse == null)
            {
                throw new Exception("Impossible de désérialiser la réponse JSON");
            }

            // S'assurer que le texte est présent
            if (string.IsNullOrEmpty(annotationResponse.Texte))
            {
                annotationResponse.Texte = correction.Copie ?? "";
            }

            if (annotationResponse.Annotations == null || !annotationResponse.Annotations.Any())
            {
                Console.WriteLine("⚠️  Aucune annotation dans la réponse, tentative avec un prompt simplifié...");
                return await GenererAnnotationsSimplifiees(correction);
            }

            // Sauvegarder les annotations dans la correction
            await SauvegarderAnnotationsAsync(correction.Id, cleanResponse);

            Console.WriteLine($"✅ {annotationResponse.Annotations.Count} annotations générées avec succès");
            
            return annotationResponse;
        }        catch (JsonException ex)
        {
            Console.WriteLine($"❌ Erreur JSON : {ex.Message}");
            Console.WriteLine($"Réponse problématique : {cleanResponse}");
            
            // Tentative de récupération avec un prompt simplifié
            return await GenererAnnotationsSimplifiees(correction);
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur lors de la génération des annotations : {ex.Message}");
        }
    }

    /// <summary>
    /// Génère des annotations avec un prompt simplifié en cas d'échec
    /// </summary>
    private async Task<AnnotationResponse> GenererAnnotationsSimplifiees(Correction correction)
    {
        try
        {
            Console.WriteLine("🔄 Tentative avec un prompt simplifié...");            var promptSimple = $@"Trouvez 3 passages dans cette copie de philosophie et commentez-les :

{correction.Copie}

Format de réponse JSON requis (UNIQUEMENT ce JSON) :
{{
    ""annotations"": [
        {{""passage"": ""première citation exacte"", ""commentaire"": ""premier commentaire""}},
        {{""passage"": ""deuxième citation exacte"", ""commentaire"": ""deuxième commentaire""}},
        {{""passage"": ""troisième citation exacte"", ""commentaire"": ""troisième commentaire""}}
    ]
}}";            var response = await _llmService.AskAsync(
                "Vous êtes professeur de philosophie. Répondez en JSON uniquement.", 
                promptSimple, 
                "Annotation simplifiée"
            );

            // Extraire le contenu du message depuis la réponse OpenAI
            var cleanResponse = ExtraireContenuMessage(response ?? "");

            var result = JsonSerializer.Deserialize<AnnotationResponse>(cleanResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            result ??= new AnnotationResponse { Annotations = new List<AnnotationItem>() };
            result.Texte = correction.Copie ?? "";

            if (result.Annotations?.Any() == true)
            {
                await SauvegarderAnnotationsAsync(correction.Id, cleanResponse);
                Console.WriteLine($"✅ {result.Annotations.Count} annotations générées (mode simplifié)");
            }
            else
            {
                Console.WriteLine("⚠️  Aucune annotation générée même en mode simplifié");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Échec du mode simplifié : {ex.Message}");
            return new AnnotationResponse 
            { 
                Texte = correction.Copie ?? "",
                Annotations = new List<AnnotationItem>()
            };
        }
    }

    /// <summary>
    /// Sauvegarde les annotations dans la base de données
    /// </summary>
    private async Task SauvegarderAnnotationsAsync(int correctionId, string annotationsJson)
    {
        try
        {
            // Lire les corrections existantes
            var corrections = await _dbService.LireCorrectionsAsync();
            var correctionIndex = corrections.FindIndex(c => c.Id == correctionId);
            
            if (correctionIndex == -1)
            {
                throw new Exception($"Correction avec l'ID {correctionId} introuvable");
            }

            // Ajouter les annotations à la correction (on pourrait ajouter une propriété Annotations à la classe Correction)
            // Pour l'instant, on stocke dans un fichier séparé
            var nomFichierAnnotations = $"annotations_{correctionId}.json";
            await System.IO.File.WriteAllTextAsync(nomFichierAnnotations, annotationsJson);
            
            Console.WriteLine($"💾 Annotations sauvegardées dans {nomFichierAnnotations}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Erreur lors de la sauvegarde des annotations : {ex.Message}");
        }
    }    /// <summary>
    /// Affiche les annotations de manière formatée par type
    /// </summary>
    public static void AfficherAnnotations(AnnotationResponse annotations)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║                     ANNOTATIONS                      ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");

        if (annotations.Annotations == null || !annotations.Annotations.Any())
        {
            Console.WriteLine("Aucune annotation disponible.");
            return;
        }

        Console.WriteLine($"\n📝 Nombre total d'annotations : {annotations.Annotations.Count}");
        
        // Grouper les annotations pour un affichage plus intelligent
        var groupes = new Dictionary<string, List<AnnotationItem>>();
        
        foreach (var annotation in annotations.Annotations)
        {
            // Déterminer le type probable basé sur le contenu du commentaire
            var type = DeterminerTypeAnnotation(annotation.Commentaire);
            
            if (!groupes.ContainsKey(type))
                groupes[type] = new List<AnnotationItem>();
            
            groupes[type].Add(annotation);
        }

        // Afficher par groupe
        foreach (var groupe in groupes.OrderBy(g => GetOrdreType(g.Key)))
        {
            var emoji = groupe.Key switch
            {
                "Structure" => "🏗️",
                "Argumentation" => "💭", 
                "Concepts" => "🔍",
                "Points positifs" => "✨",
                _ => "📝"
            };

            Console.WriteLine($"\n{emoji} {groupe.Key.ToUpper()} ({groupe.Value.Count} annotations)");
            Console.WriteLine(new string('─', 60));

            for (int i = 0; i < groupe.Value.Count; i++)
            {
                var annotation = groupe.Value[i];
                Console.WriteLine($"\n� {groupe.Key} {i + 1}:");
                Console.WriteLine($"   📄 Passage : \"{TronquerTexte(annotation.Passage, 80)}\"");
                Console.WriteLine($"   💬 Commentaire : {annotation.Commentaire}");
                
                if (i < groupe.Value.Count - 1)
                    Console.WriteLine(new string('·', 40));
            }
        }
    }

    /// <summary>
    /// Détermine le type d'annotation basé sur le contenu du commentaire
    /// </summary>
    private static string DeterminerTypeAnnotation(string commentaire)
    {
        var commentaireLower = commentaire.ToLower();
        
        if (commentaireLower.Contains("structure") || commentaireLower.Contains("introduction") || 
            commentaireLower.Contains("conclusion") || commentaireLower.Contains("plan") ||
            commentaireLower.Contains("transition") || commentaireLower.Contains("organisation"))
            return "Structure";
            
        if (commentaireLower.Contains("argument") || commentaireLower.Contains("raisonnement") || 
            commentaireLower.Contains("exemple") || commentaireLower.Contains("démonstration") ||
            commentaireLower.Contains("thèse") || commentaireLower.Contains("objection"))
            return "Argumentation";
            
        if (commentaireLower.Contains("concept") || commentaireLower.Contains("notion") || 
            commentaireLower.Contains("définition") || commentaireLower.Contains("terme") ||
            commentaireLower.Contains("vocabulaire") || commentaireLower.Contains("clarifier"))
            return "Concepts";
            
        if (commentaireLower.Contains("bien") || commentaireLower.Contains("bon") || 
            commentaireLower.Contains("pertinent") || commentaireLower.Contains("intéressant") ||
            commentaireLower.Contains("réussi") || commentaireLower.Contains("prometteur"))
            return "Points positifs";
            
        return "Autres";
    }

    /// <summary>
    /// Retourne l'ordre d'affichage des types
    /// </summary>
    private static int GetOrdreType(string type)
    {
        return type switch
        {
            "Structure" => 1,
            "Argumentation" => 2,
            "Concepts" => 3,
            "Points positifs" => 4,
            _ => 5
        };
    }

    /// <summary>
    /// Charge les annotations depuis un fichier
    /// </summary>
    public async Task<AnnotationResponse?> ChargerAnnotationsAsync(int correctionId)
    {
        try
        {
            var nomFichierAnnotations = $"annotations_{correctionId}.json";
            
            if (!System.IO.File.Exists(nomFichierAnnotations))
            {
                return null;
            }

            var contenu = await System.IO.File.ReadAllTextAsync(nomFichierAnnotations);
            
            return JsonSerializer.Deserialize<AnnotationResponse>(contenu, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Erreur lors du chargement des annotations : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Exporte les annotations vers un fichier texte lisible
    /// </summary>
    public static async Task ExporterAnnotationsAsync(AnnotationResponse annotations, Correction correction, Devoir devoir)
    {
        try
        {
            var nomFichier = $"annotations_correction_{correction.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var contenu = new StringBuilder();

            contenu.AppendLine("═══════════════════════════════════════════════════════════");
            contenu.AppendLine("                        ANNOTATIONS                        ");
            contenu.AppendLine("═══════════════════════════════════════════════════════════");
            contenu.AppendLine();
            contenu.AppendLine($"📚 Devoir : {devoir.Titre}");
            contenu.AppendLine($"📝 Type : {devoir.Type}");
            contenu.AppendLine($"📅 Date d'annotation : {DateTime.Now:dd/MM/yyyy à HH:mm}");
            contenu.AppendLine($"🎯 Note obtenue : {correction.Note:F1}/20");
            contenu.AppendLine();
            contenu.AppendLine("───────────────────────────────────────────────────────────");
            contenu.AppendLine("                      COPIE DE L'ÉLÈVE");
            contenu.AppendLine("───────────────────────────────────────────────────────────");
            contenu.AppendLine();
            contenu.AppendLine(correction.Copie);
            contenu.AppendLine();
            contenu.AppendLine("───────────────────────────────────────────────────────────");
            contenu.AppendLine("                       ANNOTATIONS");
            contenu.AppendLine("───────────────────────────────────────────────────────────");

            if (annotations.Annotations?.Any() == true)
            {
                for (int i = 0; i < annotations.Annotations.Count; i++)
                {
                    var annotation = annotations.Annotations[i];
                    contenu.AppendLine();
                    contenu.AppendLine($"🔍 ANNOTATION {i + 1}");
                    contenu.AppendLine($"   📄 Passage surligné :");
                    contenu.AppendLine($"      \"{annotation.Passage}\"");
                    contenu.AppendLine();
                    contenu.AppendLine($"   💬 Commentaire :");
                    contenu.AppendLine($"      {annotation.Commentaire}");
                    contenu.AppendLine(new string('·', 50));
                }
            }
            else
            {
                contenu.AppendLine("\nAucune annotation disponible.");
            }

            contenu.AppendLine();
            contenu.AppendLine("═══════════════════════════════════════════════════════════");
            contenu.AppendLine($"Fichier généré par Philosophix CLI v2.0 - {DateTime.Now:dd/MM/yyyy HH:mm}");
            contenu.AppendLine("═══════════════════════════════════════════════════════════");

            await System.IO.File.WriteAllTextAsync(nomFichier, contenu.ToString());
            Console.WriteLine($"📄 Annotations exportées vers : {nomFichier}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erreur lors de l'export : {ex.Message}");
        }
    }

    /// <summary>
    /// Extrait le contenu du message depuis la réponse OpenAI complète
    /// </summary>
    private string ExtraireContenuMessage(string responseOpenAI)
    {
        try
        {
            // Parse la réponse OpenAI pour extraire le contenu du message
            using var document = JsonDocument.Parse(responseOpenAI);
            var root = document.RootElement;

            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message))
                {
                    if (message.TryGetProperty("content", out var content))
                    {
                        var contentText = content.GetString() ?? "";
                        
                        // Nettoyer le contenu (enlever les éventuels backticks markdown)
                        contentText = contentText.Trim();
                        if (contentText.StartsWith("```json"))
                        {
                            contentText = contentText.Substring(7);
                        }
                        if (contentText.StartsWith("```"))
                        {
                            contentText = contentText.Substring(3);
                        }
                        if (contentText.EndsWith("```"))
                        {
                            contentText = contentText.Substring(0, contentText.Length - 3);
                        }
                        
                        return contentText.Trim();
                    }
                }
            }

            // Si on ne trouve pas la structure attendue, retourner la réponse brute
            Console.WriteLine("⚠️  Structure OpenAI non reconnue, utilisation de la réponse brute");
            return responseOpenAI;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"⚠️  Erreur lors de l'extraction du contenu : {ex.Message}");
            return responseOpenAI;
        }
    }

    /// <summary>
    /// Utilitaire pour tronquer le texte
    /// </summary>
    private static string TronquerTexte(string texte, int longueurMax)
    {
        if (string.IsNullOrEmpty(texte) || texte.Length <= longueurMax)
            return texte;

        return texte.Substring(0, longueurMax) + "...";
    }

    /// <summary>
    /// Génère des annotations pour un type spécifique (comme dans le script PHP)
    /// </summary>
    private async Task<List<AnnotationItem>> GenererAnnotationsParType(Correction correction, string type)
    {
        // Préparer les points forts et points à améliorer
        var pointsForts = correction.PointsForts?.Any() == true 
            ? string.Join(", ", correction.PointsForts) 
            : "Aucun point fort spécifique identifié";

        var pointsAmeliorer = correction.PointsAmeliorer?.Any() == true 
            ? string.Join(", ", correction.PointsAmeliorer) 
            : "Aucun point d'amélioration spécifique identifié";

        // Définir le prompt selon le type (exactement comme dans le PHP)
        var promptType = type switch
        {
            "structure" => @"En tant que professeur de philosophie expérimenté, analysez en détail la structure du texte. Identifiez au moins 8-10 éléments parmi :
            - Introduction : accroche, problématisation, annonce du plan
            - Transitions entre les parties et paragraphes
            - Structure interne des paragraphes
            - Organisation logique des arguments
            - Conclusion : synthèse et ouverture
            - Articulation des idées
            - Cohérence du développement
            - Équilibre des parties",

            "argument" => @"En tant que professeur de philosophie expérimenté, analysez en détail l'argumentation. Identifiez au moins 8-10 éléments parmi :
            - Arguments principaux à renforcer
            - Raisonnements à compléter
            - Exemples à développer
            - Thèses à approfondir
            - Objections à anticiper
            - Références philosophiques à exploiter davantage (pas de nouvelles)
            - Démonstrations à enrichir
            - Analyses à poursuivre
            - Implications à explorer",

            "concept" => @"En tant que professeur de philosophie expérimenté, analysez en détail les concepts utilisés. Identifiez au moins 8-10 éléments parmi :
            - Concepts philosophiques à définir
            - Notions clés à clarifier
            - Distinctions conceptuelles à établir
            - Présupposés à expliciter
            - Termes ambigus à préciser
            - Champs lexicaux à enrichir
            - Relations entre concepts à développer
            - Implications théoriques à approfondir
            - Usage des concepts dans le raisonnement",

            "good" => @"En tant que professeur de philosophie expérimenté, identifiez au moins 8-10 points prometteurs parmi :
            - Intuitions philosophiques pertinentes
            - Arguments bien construits
            - Exemples bien choisis
            - Références philosophiques appropriées à exploiter davantage (pas de nouvelles)
            - Analyses nuancées
            - Réflexions originales
            - Raisonnements rigoureux
            - Problématisations intéressantes
            - Transitions réussies
            - Développements prometteurs",

            _ => "Analysez cette copie et identifiez des passages importants."
        };

        var prompt = $@"{promptType}

Voici une copie d'élève :
{correction.Copie}

Note: {correction.Note:F1}/20
Appréciation: {correction.Appreciation}

IMPORTANT : 
1. Chaque passage identifié doit être une citation EXACTE du texte.
2. Les commentaires doivent être constructifs et donner des pistes d'amélioration concrètes.
3. Adaptez le niveau des commentaires à un élève de terminale de 16 ans.
4. Je veux minimum 8 annotations si c'est possible (mais plus c'est mieux).
5. Tu ne dois pas conseiller une référence philosophique nouvelle, mais tu peux suggérer d'approfondir une référence déjà utilisée.

Répondez UNIQUEMENT au format JSON suivant :
{{
    ""annotations"": [
        {{
            ""passage"": ""<citation exacte du texte>"",
            ""commentaire"": ""<commentaire constructif et piste d'amélioration>""
        }}
    ]
}}";

        var systemMessage = $"Vous êtes un professeur de philosophie qui annote des copies selon le type '{type}'. Répondez UNIQUEMENT avec du JSON valide.";        try
        {
            var response = await _llmService.AskAsync(systemMessage, prompt, $"Annotation {type}");
            
            if (string.IsNullOrEmpty(response))
            {
                return new List<AnnotationItem>();
            }

            var cleanResponse = ExtraireContenuMessage(response);
            
            var annotationResponse = JsonSerializer.Deserialize<AnnotationResponse>(cleanResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            return annotationResponse?.Annotations ?? new List<AnnotationItem>();
        }
        catch (Exception)
        {
            // Erreurs gérées au niveau supérieur pour la barre de progression
            return new List<AnnotationItem>();
        }
    }    /// <summary>
    /// NOUVELLE VERSION : Génère des annotations pour une copie déjà corrigée (avec 4 types comme PHP)
    /// </summary>
    public async Task<AnnotationResponse> GenererAnnotationsAvecTypes(Correction correction)
    {
        if (correction == null || string.IsNullOrEmpty(correction.Copie))
        {
            throw new ArgumentException("La correction et la copie ne peuvent pas être vides");
        }

        Console.WriteLine("\n🔍 Génération des annotations par type (en parallèle)...");

        var types = new[] { "structure", "argument", "concept", "good" };
        
        // Afficher la barre de progression
        AfficherBarreProgression(0, types.Length, "Initialisation...");

        // Générer les 4 types d'annotations en parallèle
        var taches = types.Select(async (type, index) =>
        {
            try
            {
                var annotationsType = await GenererAnnotationsParType(correction, type);
                
                // Mettre à jour la barre de progression
                AfficherBarreProgression(index + 1, types.Length, $"Terminé: {type} ({annotationsType.Count} annotations)");
                
                return new { Type = type, Annotations = annotationsType };
            }
            catch (Exception ex)
            {
                AfficherBarreProgression(index + 1, types.Length, $"Erreur: {type}");
                Console.WriteLine($"\n⚠️  Erreur pour le type {type} : {ex.Message}");
                return new { Type = type, Annotations = new List<AnnotationItem>() };
            }
        }).ToArray();

        var resultats = await Task.WhenAll(taches);

        // Combiner toutes les annotations
        var tousesAnnotations = new List<AnnotationItem>();
        foreach (var resultat in resultats)
        {
            tousesAnnotations.AddRange(resultat.Annotations);
        }

        // Effacer la ligne de progression et afficher le résumé
        Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
        
        var response = new AnnotationResponse
        {
            Texte = correction.Copie ?? "",
            Annotations = tousesAnnotations
        };

        if (tousesAnnotations.Any())
        {
            // Sauvegarder toutes les annotations
            await SauvegarderAnnotationsAsync(correction.Id, JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));

            // Afficher le résumé par type
            Console.WriteLine("🎯 RÉSUMÉ DES ANNOTATIONS GÉNÉRÉES :");
            foreach (var resultat in resultats)
            {
                var emoji = resultat.Type switch
                {
                    "structure" => "🏗️",
                    "argument" => "💭",
                    "concept" => "🔍",
                    "good" => "✨",
                    _ => "📝"
                };
                Console.WriteLine($"   {emoji} {resultat.Type.PadRight(12)} : {resultat.Annotations.Count,2} annotations");
            }
            Console.WriteLine($"   🎯 TOTAL           : {tousesAnnotations.Count,2} annotations");
        }
        else
        {
            Console.WriteLine("⚠️  Aucune annotation générée, tentative avec mode simplifié...");
            return await GenererAnnotationsSimplifiees(correction);
        }

        return response;
    }

    /// <summary>
    /// Affiche une barre de progression élégante dans la console
    /// </summary>
    private static void AfficherBarreProgression(int actuel, int total, string message = "")
    {
        var largeur = Math.Min(50, Console.WindowWidth - 30);
        var pourcentage = (double)actuel / total;
        var barreRemplie = (int)(pourcentage * largeur);
        
        var barre = new StringBuilder();
        barre.Append('[');
        
        for (int i = 0; i < largeur; i++)
        {
            if (i < barreRemplie)
                barre.Append('█');
            else
                barre.Append('░');
        }
        
        barre.Append(']');
        
        var pourcentageStr = $"{pourcentage * 100:F0}%".PadLeft(4);
        var compteur = $"{actuel}/{total}".PadLeft(7);
        
        // Tronquer le message si nécessaire
        var maxMessageLength = Console.WindowWidth - barre.Length - pourcentageStr.Length - compteur.Length - 10;
        if (message.Length > maxMessageLength && maxMessageLength > 0)
        {
            message = message.Substring(0, maxMessageLength - 3) + "...";
        }
        
        Console.Write($"\r{barre} {pourcentageStr} {compteur} {message}");
    }

    // ...existing code...
}

/// <summary>
/// Classe pour la réponse d'annotation de l'API
/// </summary>
public class AnnotationResponse
{
    [JsonPropertyName("texte")]
    public string Texte { get; set; } = string.Empty;

    [JsonPropertyName("annotations")]
    public List<AnnotationItem> Annotations { get; set; } = new();
}

/// <summary>
/// Classe pour un élément d'annotation individuel
/// </summary>
public class AnnotationItem
{
    [JsonPropertyName("passage")]
    public string Passage { get; set; } = string.Empty;

    [JsonPropertyName("commentaire")]
    public string Commentaire { get; set; } = string.Empty;
}
