using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CorrectionService
{
    private readonly OpenAiService _openAiService;
    private readonly JsonDatabaseService _dbService;

    private readonly string Sévérité = "Degré de sévérité : 3 / 5"; // Degré de sévérité par défaut
    public CorrectionService(OpenAiService openAiService, JsonDatabaseService dbService)
    {
        _openAiService = openAiService;
        _dbService = dbService;
    }    /// <summary>
    /// Lance le processus complet de correction d'une copie
    /// </summary>
    public async Task<Correction> CorrigerCopieAsync(Devoir devoir, string copie, bool aPAP = false)
    {        Console.WriteLine("\n" + new string('═', 60));
        Console.WriteLine("🤖 CORRECTION EN COURS...");
        Console.WriteLine(new string('═', 60));

        var competences = devoir.Bareme?.Competences ?? new List<Competence>();
          // Filtrer les compétences si PAP (exclure la compétence "Maîtrise de la langue française" ou "Expression et rédaction")
        if (aPAP)
        {
            Console.WriteLine("ℹ️  PAP activé : Les compétences d'expression ne seront pas évaluées.");
            if (devoir.Type?.ToLower() == "explication")
            {
                // retirer la compétence 6, Nom = "Expression et rédaction"
                competences = competences.Where(c => c.Nom != "Expression et rédaction").ToList();
            }
            else
            {
                // retirer la compétence 5, Nom = "Maîtrise de la langue française"
                competences = competences.Where(c => c.Nom != "Maîtrise de la langue française").ToList();
            }
        }
        
        var evaluations = new List<EvaluationCompetence>();

        // Évaluation par compétence
        for (int i = 0; i < competences.Count; i++)
        {
            var competence = competences[i];
            Console.WriteLine($"\n📋 Évaluation de la compétence {i + 1}/{competences.Count}:");
            Console.WriteLine($"   {competence.Nom}");
            Console.Write("   Analyse en cours");

            var evaluation = await EvaluerCompetenceAsync(competence, copie, devoir.Enonce ?? "", devoir.Type ?? "dissertation", devoir.TypeBac ?? "général", aPAP);
            evaluations.Add(evaluation);

            Console.WriteLine($" ✅ Note: {evaluation.Note:F1}/20");
        }        // Évaluation finale
        Console.WriteLine("\n🎯 Génération de l'évaluation finale...");
        var evaluationFinale = await EvaluerFinalAsync(evaluations, competences, copie, devoir.Type ?? "dissertation", devoir.TypeBac ?? "général", aPAP);

        // Calcul de la note moyenne
        var notesAjustees = evaluations.Select(e => AjusterNoteSelonNiveau(Convert.ToDouble(e.Note), devoir.TypeBac ?? "général")).ToList();
        
        decimal noteMoyenne = (decimal)notesAjustees.Average();

        // Afficher l'ajustement si applicable
        if (devoir.TypeBac == "technologique")
        {
            var noteSansAjustement = evaluations.Average(e => e.Note);
            Console.WriteLine($"📊 Note avant ajustement bac techno : {noteSansAjustement:F1}/20");
            Console.WriteLine($"📊 Note après ajustement bac techno : {noteMoyenne:F1}/20 (+{noteMoyenne - noteSansAjustement:F1})");
        }

        // Création de la correction
        var corrections = await _dbService.LireCorrectionsAsync();
        var newId = corrections.Count > 0 ? corrections.Max(c => c.Id) + 1 : 1;

        var correction = new Correction
        {
            Id = newId,
            DevoirId = devoir.Id,
            Note = noteMoyenne,
            Appreciation = evaluationFinale.Appreciation,
            PointsForts = evaluationFinale.PointsForts,
            PointsAmeliorer = evaluationFinale.PointsAmeliorer,
            Competences = evaluations,
            Copie = copie,
            DateCorrection = DateTime.Now
        };

        corrections.Add(correction);
        await _dbService.SauvegarderCorrectionsAsync(corrections);

        return correction;
    }    /// <summary>
    /// Évalue une compétence spécifique
    /// </summary>
    private async Task<EvaluationCompetence> EvaluerCompetenceAsync(Competence competence, string copie, string enonce, string typeDevoir, string TypeBac, bool aPAP = false)    {
        var system = "Vous êtes un professeur de philosophie expérimenté qui corrige des rédactions.";
        
        var messagePAP = aPAP ? "\n\nIMPORTANT : Cet élève dispose d'un PAP (Plan d'Accompagnement Personnalisé). Ne tenez pas compte de la qualité de l'orthographe, de la grammaire ou de l'expression écrite dans votre évaluation. Concentrez-vous uniquement sur le contenu philosophique et la réflexion." : "";
        
        // Adapter le message selon le type de bac
        string messageNiveau = "";
        if (TypeBac == "technologique")
        {
            messageNiveau = "\n📊 NIVEAU : Bac technologique - Adaptez vos attentes au niveau et soyez bienveillant sur les imperfections mineures de forme. Privilégiez la compréhension et les idées.";
        }

var prompt = $@"Évaluez la compétence ""{competence.Nom}"" .

**COMPÉTENCE À ÉVALUER :** 
{competence.Nom}

**CRITÈRES D'ÉVALUATION :**
{string.Join("\n", competence.Criteres ?? new List<string>())}

**ÉNONCÉ DU DEVOIR :** 
{enonce}

**STYLE D'APPRÉCIATION :**
Formel, vouvoie l'apprenant.

**TYPE DE DEVOIR:**
{typeDevoir}{messagePAP}

**COPIE DE L'ÉLÈVE :**
{copie}
{messagePAP}{messageNiveau}

Répondez UNIQUEMENT au format JSON suivant :
{{
    ""note"": <note sur 20>,
    ""analyse"": ""<analyse détaillée qui cite des éléments de la copie>"",
    ""points_forts"": [""point fort 1"", ""point fort 2"", ...],
    ""points_ameliorer"": [""point à améliorer 1"", ""point à améliorer 2"", ...]
}}

Évaluez UNIQUEMENT cette compétence, rien d'autre.
Pour l'analyse, cites des éléments de la copie pour justifier ta note, et addresses-toi à l'élève directement.

{Sévérité}";

        var response = await _openAiService.AskGptAsync(system, prompt);
        var evaluation = _openAiService.ParseEvaluationResponse(response);
        
        // Ajouter le nom de la compétence à l'évaluation
        evaluation.Nom = competence.Nom;
        
        return evaluation;
    }    /// <summary>
    /// Génère l'évaluation finale globale
    /// </summary>
    private async Task<EvaluationFinaleApiResponse> EvaluerFinalAsync(List<EvaluationCompetence> evaluations, List<Competence> competences, string copie, string typeDevoir, string TypeBac, bool aPAP = false)    {
        var system = "Vous êtes un professeur de philosophie expérimenté qui corrige des rédactions.";
        
        var echelleNotation = GetEchelleNotation(typeDevoir);
        var messagePAP = aPAP ? "\n\nIMPORTANT : Cet élève dispose d'un PAP (Plan d'Accompagnement Personnalisé). Dans votre appréciation générale, ne tenez pas compte de la qualité de l'orthographe, de la grammaire ou de l'expression écrite. Concentrez-vous uniquement sur le contenu philosophique et la réflexion." : "";

        string messageNiveau = "";
        if (TypeBac == "technologique")
        {
            messageNiveau = "\n📊 NIVEAU : Bac technologique - Adaptez vos attentes au niveau et soyez bienveillant sur les imperfections mineures de forme. Privilégiez la compréhension et les idées.";
        }

        
        var evaluationsText = string.Join("\n", evaluations.Zip(competences, (eval, comp) => 
            $"{comp.Nom}: {eval.Note}/20 - {eval.Analyse}"));

        var prompt = $@"Type de devoir : {typeDevoir}
En tant que professeur de philosophie, faites une évaluation globale de cette copie.

Voici les évaluations par compétence :
{evaluationsText}

Copie de l'élève :
{copie}

Style d'appréciation : 
Formelle, vouvoie l'apprenant.

{messagePAP}

{messageNiveau}

{echelleNotation}

Répondez UNIQUEMENT au format JSON suivant :
{{
    ""appreciation"": ""<appréciation générale détaillée>"",
    ""points_forts"": [""point fort 1"", ""point fort 2"", ""point fort 3""],
    ""points_ameliorer"": [""point 1"", ""point 2"", ""point 3""]
}}

Pour l'appreciation addresses-toi à l'élève directement.
{Sévérité}";

        var response = await _openAiService.AskGptAsync(system, prompt);
        return _openAiService.ParseEvaluationFinaleResponse(response);
    }

    /// <summary>
    /// Retourne l'échelle de notation selon le type de devoir
    /// </summary>
    private static string GetEchelleNotation(string typeDevoir)
    {
        if (typeDevoir?.ToLower() == "dissertation")
        {
            return @"Échelle d'évaluation pour guider la notation des copies :
""""""
- Ce qui est valorisé : une problématisation du sujet, une argumentation cohérente et progressive, l'analyse de concepts (notions, distinctions) et d'exemples précisément étudiés, la mobilisation d'éléments de culture philosophique au service du traitement du sujet, la capacité de la réflexion à entrer en dialogue avec elle-même. 
- Ce qui est sanctionné : la paraphrase du texte, la récitation de cours sans lien avec le sujet, l'accumulation de lieux communs, la juxtaposition d'exemples sans réflexion, l'absence de problématisation, l'absence de rigueur dans le raisonnement, l'absence de culture philosophique mobilisée pour traiter le sujet.

# Échelle de notation :
- Entre 0 et 5 → copie très insuffisante : inintelligible ; non structurée ; excessivement brève ; marquant un refus manifeste de faire l'exercice.
- De 06 à 09 → Copie intelligible mais qui ne répond pas aux critères attestés de l'épreuve : propos excessivement général ou restant sans rapport avec la question posée ; juxtaposition d'exemples sommaires ou anecdotiques ; accumulation de lieux communs ; paraphrase ou répétition du texte ; récitation de cours sans traitement du sujet ;- copie qui aurait pu être rédigée au début de l'année, sans aucun cours de philosophie ou connaissances acquises.
- Pas moins de 10 → Copie témoignant d'un réel effort de réflexion, et, même si le résultat n'est pas abouti, de traitement du sujet : effort de problématisation ; effort de définition des notions ; examen de réponses possibles ; cohérence globale du propos.
- Pas moins de 12 → Si, en plus, il y a mobilisation de références et d'exemples pertinents pour le sujet.
- Pas moins de 14 → Si, en plus, le raisonnement est construit, progressif, et que les affirmations posées sont rigoureusement justifiées.
- Pas moins de 16 → Si, en plus, la copie témoigne de la maîtrise de concepts philosophiques utiles pour le sujet (notions, repères), d'une démarche de recherche et du souci des enjeux de la question, d'une précision dans l'utilisation d'une culture au service du traitement du sujet. 
""""""";
        }
        else
        {
            return @"Échelle d'évaluation pour guider la notation des copies :
""""""
- Ce qui est valorisé : une détermination du problème du texte, une explication de ses éléments signifiants, une explicitation des articulations du texte, une caractérisation  de la position philosophique élaborée par  l'auteur dans le texte, et, plus généralement,  du questionnement auquel elle s'articule.
- Ce qui est sanctionné : la paraphrase du texte, la récitation de cours sans lien avec le texte de l'auteur, l'accumulation de lieux communs, la juxtaposition d'exemples sans réflexion, l'absence de problématisation du texte, l'absence de rigueur dans le raisonnement, l'absence de culture philosophique mobilisée pour traiter le sujet.

# Échelle de notation :
- Entre 0 et 5 → copie très insuffisante : inintelligible ; non structurée ; excessivement brève ; marquant un refus manifeste de faire l'exercice.
- De 06 à 09 → Copie intelligible mais qui ne répond pas aux critères attestés de l'épreuve : propos excessivement général ou restant sans rapport avec la question posée ; juxtaposition d'exemples sommaires ou anecdotiques ; accumulation de lieux communs ; paraphrase ou répétition du texte ; récitation de cours sans traitement du sujet ;- copie qui aurait pu être rédigée au début de l'année, sans aucun cours de philosophie ou connaissances acquises.
- Pas moins de 10 → Copie faisant l'effort de réaliser l'exercice, même si l'explication demeure maladroite et inaboutie : explication commençante ; pas de contresens majeur sur le propos et la démarche de l'auteur.
- Pas moins de 12 → Si, en plus, le texte est interrogé avec un effort d'attention au détail du propos, ainsi qu'à sa structure logique.
- Pas moins de 14 → Si, en plus, les éléments du texte sont mis en perspective, avec des éléments de connaissance permettant de déterminer et d'examiner le problème.
- Pas moins de 16 → Si, en plus, l'explication est développée avec amplitude et justesse : l'ensemble du texte est examiné et bien situé dans une problématique  et un questionnement pertinents.
""""""";
        }
    }


    /// <summary>
    /// Ajuste les notes selon le niveau d'évaluation pour plus de bienveillance
    /// </summary>
    private double AjusterNoteSelonNiveau(double note, string typeBac)
    {
        return typeBac switch
        {
            "technologique" => Math.Min(20, note + 1.5), // Bonus de bienveillance pour bac techno
            "général" => note, // Pas d'ajustement pour le bac général
            _ => note // Par défaut, pas d'ajustement
        };
    }

    /// <summary>
    /// Valide qu'une copie respecte les critères minimums
    /// </summary>
    public static bool ValiderCopie(string copie, int longueurMinimum = 500)
    {
        return !string.IsNullOrWhiteSpace(copie) && copie.Length >= longueurMinimum;
    }

    /// <summary>
    /// Affiche les résultats de correction de manière formatée
    /// </summary>
    public static void AfficherResultatsCorrection(Correction correction, List<Competence> competences)
    {
        //Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║                RÉSULTATS DE CORRECTION               ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        
        Console.WriteLine($"\n🎯 NOTE FINALE : {correction.Note:F1}/20");
        Console.WriteLine($"📅 Date de correction : {correction.DateCorrection:dd/MM/yyyy HH:mm}");
        
        Console.WriteLine("\n" + new string('═', 60));
        Console.WriteLine("💬 APPRÉCIATION GÉNÉRALE");
        Console.WriteLine(new string('═', 60));
        Console.WriteLine(correction.Appreciation);

        Console.WriteLine("\n" + new string('─', 60));
        Console.WriteLine("✅ POINTS FORTS");
        Console.WriteLine(new string('─', 60));
        if (correction.PointsForts != null)
        {
            foreach (var point in correction.PointsForts)
            {
                Console.WriteLine($"• {point}");
            }
        }

        Console.WriteLine("\n" + new string('─', 60));
        Console.WriteLine("📈 POINTS À AMÉLIORER");
        Console.WriteLine(new string('─', 60));
        if (correction.PointsAmeliorer != null)
        {
            foreach (var point in correction.PointsAmeliorer)
            {
                Console.WriteLine($"• {point}");
            }
        }

        Console.WriteLine("\n" + new string('═', 60));
        Console.WriteLine("📊 DÉTAIL PAR COMPÉTENCE");
        Console.WriteLine(new string('═', 60));
        
        if (correction.Competences != null)
        {
            for (int i = 0; i < correction.Competences.Count; i++)
            {
                var eval = correction.Competences[i];
                Console.WriteLine($"\n{i + 1}. {eval.Nom} - {eval.Note:F1}/20");
                Console.WriteLine($"   {eval.Analyse}");
                
                if (eval.PointsForts?.Count > 0)
                {
                    Console.WriteLine("   ✅ Points forts :");
                    foreach (var point in eval.PointsForts)
                    {
                        Console.WriteLine($"      • {point}");
                    }
                }
                
                if (eval.PointsAmeliorer?.Count > 0)
                {
                    Console.WriteLine("   📈 À améliorer :");
                    foreach (var point in eval.PointsAmeliorer)
                    {
                        Console.WriteLine($"      • {point}");
                    }
                }
            }
        }

        Console.WriteLine($"\n✅ Correction sauvegardée avec l'ID : {correction.Id}");
    }
}
