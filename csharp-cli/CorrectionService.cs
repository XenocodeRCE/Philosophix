using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CorrectionService
{
    private readonly OpenAiService _openAiService;
    private readonly JsonDatabaseService _dbService;

    /// <summary>
    /// Obtient le niveau de sévérité selon le type de bac
    /// </summary>
    private string GetSeverite(string typeBac)
    {
        return typeBac switch
    {
        "technologique" => @"INSTRUCTIONS DE NOTATION pour BAC TECHNOLOGIQUE :
- Cette copie doit être évaluée selon les standards réels du bac technologique
- Ne donnez PAS la même note à toutes les compétences
- Soyez différencié : certaines compétences peuvent avoir 8-9/20, d'autres 11-13/20
- N'hésitez pas à donner des notes en dessous de 10/20 si la compétence est insuffisante
- Basez-vous sur l'échelle : 6-9 = insuffisant, 10-11 = correct, 12-14 = bien, 15+ = très bien",
        
        "général" => "Degré de sévérité : 3 / 5",
        _ => "Degré de sévérité : 3 / 5"
    };
    }

    public CorrectionService(OpenAiService openAiService, JsonDatabaseService dbService)
    {
        _openAiService = openAiService;
        _dbService = dbService;
    }    /// <summary>
    /// Lance le processus complet de correction d'une copie
    /// </summary>
    public async Task<Correction> CorrigerCopieAsync(Devoir devoir, string copie, bool aPAP = false)
    {
        Console.WriteLine("\n" + new string('═', 60));
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
        }

        // Évaluation finale
        Console.WriteLine("\n🎯 Génération de l'évaluation finale...");
        var evaluationFinale = await EvaluerFinalAsync(evaluations, competences, copie, devoir.Type ?? "dissertation", devoir.TypeBac ?? "général", aPAP);

        // Afficher le résumé des coûts
        Console.WriteLine("\n" + new string('─', 60));
        _openAiService.CostTracker.DisplayCostSummary();

        // Calcul de la note moyenne
        var notesAjustees = evaluations.Select(e => AjusterNoteSelonNiveau(Convert.ToDouble(e.Note), devoir.TypeBac ?? "général")).ToList();
        

         // Calcul de la note moyenne avec pondération intelligente
        var notesFinales = evaluations.Select(e => e.Note).ToList();
        var notesFinalesDouble = notesFinales.Select(n => Convert.ToDouble(n)).ToList();
        var noteMoyenne = AppliquerPonderation(notesFinalesDouble, devoir.TypeBac ?? "général", evaluations);

        // Afficher les détails pour le bac technologique
        if (devoir.TypeBac == "technologique")
        {
             var noteSansAjustement = evaluations.Average(e => e.Note);
            Console.WriteLine($"📊 Note moyenne des compétences : {noteSansAjustement:F1}/20");
            Console.WriteLine($"📊 Note finale après pondération bac techno : {noteMoyenne:F1}/20");
            
            // Debug : afficher quelques extraits d'analyse pour vérification
            Console.WriteLine("🔍 Extraits d'analyses pour vérification :");
            foreach (var eval in evaluations.Take(2))
            {
                var extrait = eval.Analyse?.Substring(0, Math.Min(eval.Analyse.Length, 100)) ?? "";
                Console.WriteLine($"   • {eval.Nom}: {extrait}...");
            }
        }

        // Création de la correction
        var corrections = await _dbService.LireCorrectionsAsync();
        var newId = corrections.Count > 0 ? corrections.Max(c => c.Id) + 1 : 1;

        var correction = new Correction
        {
            Id = newId,
            DevoirId = devoir.Id,
            Note = (decimal)noteMoyenne,
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
        var system = $@"Vous êtes un correcteur de philosophie qui évalue selon les standards RÉELS du bac {TypeBac}.
        
        ATTENTION : Cette copie doit être notée de manière DIFFÉRENCIÉE et RÉALISTE.
- Ne donnez PAS la même note à toutes les compétences
- Utilisez toute l'échelle de notation : 6-20/20";
        
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

{GetSeverite(TypeBac)}";

        var response = await _openAiService.AskGptAsync(system, prompt, $"Compétence: {competence.Nom}");
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
{GetSeverite(TypeBac)}";

        var response = await _openAiService.AskGptAsync(system, prompt, "Évaluation finale");
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
            "technologique" => Math.Min(20, note + 0.5),
            "général" => note,
            _ => note
        };
    }
    

    /// <summary>
    /// Détecte la qualité globale d'une copie basée sur les évaluations textuelles
    /// </summary>
    private string DetecterQualiteCopie(List<EvaluationCompetence> evaluations)
    {
        // Mots-clés pour copie de BONNE qualité
        var motsClesBons = new[] { 
            "pertinente", "pertinent", "solide", "structuré", "structurée", "claire", "clair", "clairement",
            "bon", "bonne", "réussi", "efficace", "approprié", "appropriée", "cohérent", "cohérente",
            "intéressant", "intéressante", "satisfaisant", "satisfaisante", "correct", "correcte",
            "bien", "références", "philosophiques", "variées", "argumentatif", "argumentative",
            "logique", "fluide", "plan", "problématique", "développé", "développée", "richesse",
            "qualité", "maîtrise", "réflexion", "construction", "organisation", "progression",
            "analyse", "synthèse", "articulation", "engagement", "effort", "capacité", "enrichit",
            "enrichissant", "montre", "témoigne", "démontre", "réussi à", "parvenez", "identifié"
        };
        
        // Mots-clés pour copie VRAIMENT faible (très restrictifs)
        var motsClesFaibles = new[] { 
            "très insuffisant", "insuffisant", "extrêmement faible", "grave lacune",
            "incompréhensible", "incohérent totalement", "absent totalement", "inexistant",
            "catastrophique", "désorganisé complètement", "inintelligible", 
            "hors sujet", "sans rapport avec", "refuse de faire", "très faible"
        };

        // Mots-clés d'amélioration (neutres - ne comptent ni pour ni contre)
        var motsClesAmeliorations = new[] {
            "améliorer", "clarifier", "préciser", "développer", "renforcer", "éviter",
            "corriger", "veiller", "attention", "pourrait", "aurait pu", "gagnerait",
            "bénéficier", "manque", "manquer", "perfectible"
        };

        int scoreBon = 0;
        int scoreFaible = 0;
        int scoreAméliorations = 0;
        int totalMots = 0;

        foreach (var eval in evaluations)
        {
            var analyseTexte = eval.Analyse?.ToLower() ?? "";
            var pointsForts = string.Join(" ", eval.PointsForts?.Select(p => p.ToLower()) ?? new List<string>());
            var pointsAmeliorer = string.Join(" ", eval.PointsAmeliorer?.Select(p => p.ToLower()) ?? new List<string>());
            
            var texteComplet = $"{analyseTexte} {pointsForts}";
            var mots = texteComplet.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            totalMots += mots.Length;
            
            // Compter les occurrences
            foreach (var mot in motsClesBons)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(texteComplet, $@"\b{mot}\b");
                scoreBon += matches.Count;
            }
            
            foreach (var mot in motsClesFaibles)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(texteComplet, $@"\b{mot}\b");
                scoreFaible += matches.Count;
            }

            foreach (var mot in motsClesAmeliorations)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(texteComplet, $@"\b{mot}\b");
                scoreAméliorations += matches.Count;
            }
        }

        // Calcul des densités (pourcentages)
        var densiteBon = totalMots > 0 ? (double)scoreBon / totalMots * 100 : 0;
        var densiteFaible = totalMots > 0 ? (double)scoreFaible / totalMots * 100 : 0;
        var densiteAmeliorations = totalMots > 0 ? (double)scoreAméliorations / totalMots * 100 : 0;

        // Calcul des moyennes de notes pour validation croisée
        var moyenneNotes = evaluations.Average(e => e.Note);

        Console.WriteLine($"🔍 Analyse qualité - Positif: {scoreBon}, Négatif: {scoreFaible}, Améliorations: {scoreAméliorations}");
        Console.WriteLine($"🔍 Densités - Positif: {densiteBon:F1}%, Négatif: {densiteFaible:F1}%, Améliorations: {densiteAmeliorations:F1}%");
        Console.WriteLine($"🔍 Moyenne des notes: {moyenneNotes:F1}/20");

        // NOUVELLE LOGIQUE CORRIGÉE
        // Une copie est bonne si elle a beaucoup de points positifs ET peu de vrais défauts
        // Une copie est faible si elle a beaucoup de vrais défauts ET peu de points positifs
        
        if (moyenneNotes >= 13 && densiteBon >= 3.5 && densiteFaible <= 1.5)
        {
            return "bonne";
        }
        else if ((double)moyenneNotes >= 11.5 && densiteBon >= 2.5 && densiteFaible <= 2.0)
        {
            return "bonne";
        }
        else if (moyenneNotes < 9 && densiteFaible >= 2.0 && densiteBon <= 1.5)
        {
            return "faible";
        }
        else
        {
            return "moyenne";
        }
    }

    /// <summary>
    /// Applique une pondération plus subtile selon le type de bac
    /// </summary>
    private double AppliquerPonderation(List<double> notes, string typeBac, List<EvaluationCompetence> evaluations)
    {
        var moyenne = notes.Average();
        var ecartType = CalculerEcartType(notes);
        var qualiteCopie = DetecterQualiteCopie(evaluations);
        
        Console.WriteLine($"📊 Qualité détectée : {qualiteCopie}");
        Console.WriteLine($"📊 Écart-type des notes : {ecartType:F2}");
        
        if (typeBac == "technologique")
        {
            switch (qualiteCopie)
            {
                case "bonne":
                    // Copie de bonne qualité : ajustement positif significatif
                    if (moyenne < 13)
                    {
                        moyenne = moyenne * 1.35; // +35% si sous-évaluée
                        Console.WriteLine("✅ Ajustement positif fort pour copie bonne qualité sous-évaluée");
                    }
                    else if (moyenne < 15)
                    {
                        moyenne = moyenne * 1.20; // +20%
                        Console.WriteLine("✅ Ajustement positif modéré pour copie bonne qualité");
                    }
                    else
                    {
                        moyenne = moyenne * 1.05; // +5% (déjà bien notée)
                        Console.WriteLine("✅ Ajustement positif léger pour copie déjà bien notée");
                    }
                    break;
                    
                case "faible":
                    // Copie vraiment faible : réduction
                    moyenne = moyenne * 0.80; // -20%
                    Console.WriteLine("📉 Ajustement négatif pour copie faible");
                    break;
                    
                default: // moyenne
                    // Copie moyenne : ajustement neutre
                    moyenne = moyenne * 1.02; // +2% (bienveillance bac techno)
                    Console.WriteLine("🔄 Ajustement neutre bienveillant pour copie moyenne");
                    break;
            }
            
            // Contraintes finales
            moyenne = Math.Max(moyenne, 6.0);  // Minimum 6/20
            moyenne = Math.Min(moyenne, 18.5); // Maximum 18.5/20
            
            return Math.Round(moyenne, 1);
        }
        
        return Math.Round(moyenne, 1);

    }

    /// <summary>
    /// Calcule l'écart-type pour détecter si les notes sont trop uniformes
    /// </summary>
    private double CalculerEcartType(List<double> notes)
    {
        if (notes.Count <= 1) return 0;
    
        var moyenne = notes.Average();
        var variance = notes.Sum(x => Math.Pow(x - moyenne, 2)) / notes.Count;
        return Math.Sqrt(variance);
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
