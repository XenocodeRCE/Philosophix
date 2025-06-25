using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Threading;

public class CorrectionService
{
    private readonly ILLMService _llmService;
    private readonly JsonDatabaseService _dbService;

    public CorrectionService(ILLMService llmService, JsonDatabaseService dbService)
    {
        _llmService = llmService;
        _dbService = dbService;
    }

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
        };    }

    // Suppression de l'ancien constructeur OpenAI-spécifique
    // Il est maintenant remplacé par le constructeur ILLMService/// <summary>
         /// Lance le processus complet de correction d'une copie
         /// </summary>
    public async Task<Correction> CorrigerCopieAsync(Devoir devoir, string copie, bool aPAP = false)
    {
        Console.WriteLine("\n" + new string('═', 60));
        Console.WriteLine("🤖 CORRECTION EN COURS...");
        Console.WriteLine(new string('═', 60));

        // Analyse linguistique de la copie
        var (nombreMots, motsUniques, richesseVocabulaire, motsPlusFrequents) = AnalyserMetriquesLinguistiques(copie);
        var mtld = CalculerMTLD(copie);
        var analyseLinguistique = InterpreterMTLD(mtld, nombreMots, devoir.Type ?? "dissertation", devoir.TypeBac ?? "général");
        
        Console.WriteLine($"📊 ANALYSE LINGUISTIQUE :");
        Console.WriteLine($"   • Longueur : {nombreMots} mots ({motsUniques} uniques, {richesseVocabulaire:F1}% de richesse)");
        var qualiteMTLD = mtld >= 50 ? "excellent" : mtld >= 40 ? "très bon" : mtld >= 30 ? "correct" : mtld >= 20 ? "faible" : "très faible";
        Console.WriteLine($"   • Diversité lexicale (MTLD) : {mtld:F1} ({qualiteMTLD})");
        Console.WriteLine($"   • Mots les plus fréquents : {string.Join(", ", motsPlusFrequents.Take(5))}");
        Console.WriteLine();

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
        var evaluationFinale = await EvaluerFinalAsync(evaluations, competences, copie, devoir.Type ?? "dissertation", devoir.TypeBac ?? "général", aPAP);        // Afficher le résumé des coûts
        Console.WriteLine("\n" + new string('─', 60));
        _llmService.CostTracker?.DisplayCostSummary();

        // Calcul de la note moyenne
        var notesAjustees = evaluations.Select(e => AjusterNoteSelonNiveau(Convert.ToDouble(e.Note), devoir.TypeBac ?? "général")).ToList();


        // Calcul de la note moyenne avec pondération intelligente
        var notesFinales = evaluations.Select(e => e.Note).ToList();
        var notesFinalesDouble = notesFinales.Select(n => Convert.ToDouble(n)).ToList();
        var noteMoyenne = AppliquerPonderation(notesFinalesDouble, devoir.TypeBac ?? "général", evaluations, devoir.Type ?? "dissertation");

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
            Appreciation = evaluationFinale.Appreciation + $"\n\n[Analyse linguistique MTLD: {analyseLinguistique}]",
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
    private async Task<EvaluationCompetence> EvaluerCompetenceAsync(Competence competence, string copie, string enonce, string typeDevoir, string TypeBac, bool aPAP = false)
    {        var system = $@"Vous êtes un correcteur de philosophie qui évalue selon les standards RÉELS du bac {TypeBac}.
        
        ATTENTION : Cette copie doit être notée de manière DIFFÉRENCIÉE et RÉALISTE.
- Ne donnez PAS la même note à toutes les compétences
- Utilisez toute l'échelle de notation : 6-20/20

IMPORTANT : Vous DEVEZ répondre uniquement avec un objet JSON valide, sans texte supplémentaire avant ou après.";

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

Répondez UNIQUEMENT au format JSON suivant (pas de texte avant ou après) :
{{
    ""note"": <nombre entre 6 et 20>,
    ""analyse"": ""<analyse détaillée qui cite des éléments de la copie>"",
    ""points_forts"": [""point fort 1"", ""point fort 2"", ...],
    ""points_ameliorer"": [""point à améliorer 1"", ""point à améliorer 2"", ...]
}}

Évaluez UNIQUEMENT cette compétence, rien d'autre.
Pour l'analyse, citez des éléments de la copie pour justifier votre note, et adressez-vous à l'élève directement.

{GetSeverite(TypeBac)}";var response = await _llmService.AskAsync(system, prompt, $"Compétence: {competence.Nom}");
        var evaluation = ParseEvaluationResponse(response);

        // Ajouter le nom de la compétence à l'évaluation
        evaluation.Nom = competence.Nom;

        return evaluation;
    }    /// <summary>
         /// Génère l'évaluation finale globale
         /// </summary>
    private async Task<EvaluationFinaleApiResponse> EvaluerFinalAsync(List<EvaluationCompetence> evaluations, List<Competence> competences, string copie, string typeDevoir, string TypeBac, bool aPAP = false)
    {
        var system = "Vous êtes un professeur de philosophie expérimenté qui corrige des rédactions. Vous DEVEZ répondre uniquement avec un objet JSON valide, sans texte supplémentaire avant ou après.";

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

Répondez UNIQUEMENT au format JSON suivant (pas de texte avant ou après) :
{{
    ""appreciation"": ""<appréciation générale détaillée>"",
    ""points_forts"": [""point fort 1"", ""point fort 2"", ""point fort 3""],
    ""points_ameliorer"": [""point 1"", ""point 2"", ""point 3""]
}}

Pour l'appréciation, adressez-vous à l'élève directement.
{GetSeverite(TypeBac)}";var response = await _llmService.AskAsync(system, prompt, "Évaluation finale");
        return ParseEvaluationFinaleResponse(response);
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

        // LOGIQUE AJUSTÉE : plus restrictive pour les moyennes notes
        if (moyenneNotes >= 14 && densiteBon >= 4.0 && densiteFaible <= 1.0)
        {
            return "bonne";
        }
        else if ((double)moyenneNotes >= 12.5 && densiteBon >= 3.0 && densiteFaible <= 1.5)
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
    private double AppliquerPonderation(List<double> notes, string typeBac, List<EvaluationCompetence> evaluations, string typeDevoir = "dissertation")
    {
        var moyenne = notes.Average();
        var ecartType = CalculerEcartType(notes);
        var qualiteCopie = DetecterQualiteCopie(evaluations);
        
        Console.WriteLine($"📊 Qualité détectée : {qualiteCopie}");
        Console.WriteLine($"📊 Écart-type des notes : {ecartType:F2}");
        Console.WriteLine($"📊 Type de devoir : {typeDevoir}");
        
        if (typeBac == "technologique")
        {
            // Ajustement différent selon le type de devoir
            if (typeDevoir?.ToLower() == "explication")
            {
                // Pour les explications : ajustements TRÈS modérés
                switch (qualiteCopie)
                {
                    case "bonne":
                        if (moyenne < 10)
                        {
                            moyenne = moyenne * 1.10; // +10% seulement si très sous-évaluée
                            Console.WriteLine("✅ Ajustement modéré pour explication bonne qualité sous-évaluée");
                        }
                        else if (moyenne < 12)
                        {
                            moyenne = moyenne * 1.05; // +5%
                            Console.WriteLine("✅ Ajustement léger pour explication bonne qualité");
                        }
                        else
                        {
                            moyenne = moyenne * 1.00; // Pas d'ajustement (déjà correcte)
                            Console.WriteLine("✅ Pas d'ajustement pour explication déjà bien notée");
                        }
                        break;
                        
                    case "faible":
                        moyenne = moyenne * 0.85; // -15%
                        Console.WriteLine("📉 Ajustement négatif pour explication faible");
                        break;
                        
                    default: // moyenne
                        moyenne = moyenne * 1.00; // Pas d'ajustement
                        Console.WriteLine("🔄 Pas d'ajustement pour explication moyenne");
                        break;
                }
            }
            else
            {
                // Pour les dissertations : ajustements plus significatifs
                switch (qualiteCopie)
                {
                    case "bonne":
                        if (moyenne < 13)
                        {
                            moyenne = moyenne * 1.35; // +35% si sous-évaluée
                            Console.WriteLine("✅ Ajustement positif fort pour dissertation bonne qualité sous-évaluée");
                        }
                        else if (moyenne < 15)
                        {
                            moyenne = moyenne * 1.20; // +20%
                            Console.WriteLine("✅ Ajustement positif modéré pour dissertation bonne qualité");
                        }
                        else
                        {
                            moyenne = moyenne * 1.05; // +5% (déjà bien notée)
                            Console.WriteLine("✅ Ajustement positif léger pour dissertation déjà bien notée");
                        }
                        break;
                        
                    case "faible":
                        moyenne = moyenne * 0.80; // -20%
                        Console.WriteLine("📉 Ajustement négatif pour dissertation faible");
                        break;
                        
                    default: // moyenne
                        moyenne = moyenne * 1.02; // +2% (bienveillance bac techno)
                        Console.WriteLine("🔄 Ajustement neutre bienveillant pour dissertation moyenne");
                        break;
                }
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

        // Afficher les métriques linguistiques si disponibles dans l'appréciation
        if (correction.Copie != null)
        {
            var (nombreMots, motsUniques, richesseVocabulaire, motsPlusFrequents) = AnalyserMetriquesLinguistiquesStatic(correction.Copie);
            var mtld = CalculerMTLDStatic(correction.Copie);
            
            Console.WriteLine("\n" + new string('═', 60));
            Console.WriteLine("📊 MÉTRIQUES LINGUISTIQUES");
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"📝 Longueur : {nombreMots} mots ({motsUniques} uniques, {richesseVocabulaire:F1}% de richesse)");
            Console.WriteLine($"� Mots les plus utilisés : {string.Join(", ", motsPlusFrequents.Take(8))}");
            Console.WriteLine($"🎯 Diversité lexicale (MTLD) : {mtld:F1}");
            
            // Afficher l'interprétation MTLD si elle a été stockée dans l'appréciation
            if (correction.Appreciation?.Contains("[Analyse linguistique MTLD:") == true)
            {
                var startIndex = correction.Appreciation.IndexOf("[Analyse linguistique MTLD:") + "[Analyse linguistique MTLD:".Length;
                var endIndex = correction.Appreciation.IndexOf("]", startIndex);
                if (endIndex > startIndex)
                {
                    var analyseMTLD = correction.Appreciation.Substring(startIndex, endIndex - startIndex).Trim();
                    Console.WriteLine($"🎓 Interprétation pédagogique : {analyseMTLD}");
                }
            }
            else
            {
                // Calculer l'interprétation MTLD si elle n'est pas stockée
                var interpretationMTLD = InterpreterMTLDStatic(mtld, nombreMots, "dissertation", "général");
                Console.WriteLine($"🎓 Interprétation pédagogique : {interpretationMTLD}");
            }
        }

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

    /// <summary>
    /// Exporte une correction vers un fichier .txt bien formaté
    /// </summary>
    public static async Task<string> ExporterCorrectionAsync(Correction correction, Devoir devoir, string cheminDossier = "")
    {
        // Toujours utiliser le dossier en cours
        var dossierExport = Environment.CurrentDirectory;

        // Créer le nom du fichier
        var dateCorrection = correction.DateCorrection.ToString("yyyy-MM-dd_HH-mm");
        var sujetCourt = devoir.Titre?.Replace(" ", "_").Replace("?", "").Replace(":", "").Replace("/", "_") ?? "Sujet";
        var nomFichier = $"Correction_{sujetCourt}_{dateCorrection}.txt";

        // Chemin complet
        var cheminComplet = Path.Combine(dossierExport, "Exports", nomFichier);

        // Créer le dossier s'il n'existe pas
        Directory.CreateDirectory(Path.GetDirectoryName(cheminComplet)!);

        var contenu = new StringBuilder();

        // En-tête
        contenu.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        contenu.AppendLine("║                           CORRECTION DE COPIE                               ║");
        contenu.AppendLine("║                             PHILOSOPHIX                                     ║");
        contenu.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        contenu.AppendLine();

        // Informations générales
        contenu.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        contenu.AppendLine("📋 INFORMATIONS GÉNÉRALES");
        contenu.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        contenu.AppendLine($"📅 Date de correction : {correction.DateCorrection:dd/MM/yyyy à HH:mm}");
        contenu.AppendLine($"📝 Sujet : {devoir.Titre}");
        contenu.AppendLine($"📖 Énoncé : {devoir.Enonce}");
        contenu.AppendLine($"🎯 Type de devoir : {devoir.Type}");
        contenu.AppendLine($"🎓 Type de bac : {devoir.TypeBac}");
        contenu.AppendLine($"🏆 NOTE FINALE : {correction.Note:F1}/20");
        contenu.AppendLine();

        // Appréciation générale
        contenu.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        contenu.AppendLine("💬 APPRÉCIATION GÉNÉRALE");
        contenu.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        contenu.AppendLine(correction.Appreciation);
        contenu.AppendLine();

        // Points forts
        contenu.AppendLine("───────────────────────────────────────────────────────────────────────────────");
        contenu.AppendLine("✅ POINTS FORTS");
        contenu.AppendLine("───────────────────────────────────────────────────────────────────────────────");
        if (correction.PointsForts != null && correction.PointsForts.Count > 0)
        {
            foreach (var point in correction.PointsForts)
            {
                contenu.AppendLine($"• {point}");
            }
        }
        else
        {
            contenu.AppendLine("Aucun point fort spécifique identifié.");
        }
        contenu.AppendLine();

        // Points à améliorer
        contenu.AppendLine("───────────────────────────────────────────────────────────────────────────────");
        contenu.AppendLine("📈 POINTS À AMÉLIORER");
        contenu.AppendLine("───────────────────────────────────────────────────────────────────────────────");
        if (correction.PointsAmeliorer != null && correction.PointsAmeliorer.Count > 0)
        {
            foreach (var point in correction.PointsAmeliorer)
            {
                contenu.AppendLine($"• {point}");
            }
        }
        else
        {
            contenu.AppendLine("Aucun point d'amélioration spécifique identifié.");
        }
        contenu.AppendLine();

        // Détail par compétence
        contenu.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        contenu.AppendLine("📊 ÉVALUATION DÉTAILLÉE PAR COMPÉTENCE");
        contenu.AppendLine("═══════════════════════════════════════════════════════════════════════════════");

        if (correction.Competences != null && correction.Competences.Count > 0)
        {
            for (int i = 0; i < correction.Competences.Count; i++)
            {
                var eval = correction.Competences[i];

                contenu.AppendLine($"\n{i + 1}. {(eval.Nom != null ? eval.Nom.ToUpper() : "COMPÉTENCE SANS NOM")}");
                contenu.AppendLine($"   Note : {eval.Note:F1}/20");
                contenu.AppendLine("   " + new string('─', 75));

                // Analyse détaillée
                contenu.AppendLine("   📝 Analyse :");
                var lignesAnalyse = eval.Analyse?.Split('\n') ?? new[] { "Aucune analyse disponible." };
                foreach (var ligne in lignesAnalyse)
                {
                    contenu.AppendLine($"   {ligne}");
                }
                contenu.AppendLine();

                // Points forts de la compétence
                if (eval.PointsForts?.Count > 0)
                {
                    contenu.AppendLine("   ✅ Points forts :");
                    foreach (var point in eval.PointsForts)
                    {
                        contenu.AppendLine($"      • {point}");
                    }
                    contenu.AppendLine();
                }

                // Points à améliorer de la compétence
                if (eval.PointsAmeliorer?.Count > 0)
                {
                    contenu.AppendLine("   📈 À améliorer :");
                    foreach (var point in eval.PointsAmeliorer)
                    {
                        contenu.AppendLine($"      • {point}");
                    }
                    contenu.AppendLine();
                }
            }
        }

        // Copie de l'élève
        contenu.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        contenu.AppendLine("📄 COPIE DE L'ÉLÈVE");
        contenu.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        contenu.AppendLine(correction.Copie);
        contenu.AppendLine();

        // Pied de page
        contenu.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        contenu.AppendLine($"Correction générée par Philosophix le {DateTime.Now:dd/MM/yyyy à HH:mm}");
        contenu.AppendLine("═══════════════════════════════════════════════════════════════════════════════");

        // Écrire le fichier
        await File.WriteAllTextAsync(cheminComplet, contenu.ToString(), Encoding.UTF8);

        return cheminComplet;
    }
    
    /// <summary>
    /// Parse la réponse de l'évaluation d'une compétence
    /// </summary>
    private EvaluationCompetence ParseEvaluationResponse(string apiResponse)
    {
        try
        {
            // Extraire le contenu JSON depuis la réponse (OpenAI ou Ollama)
            var content = ExtraireContenuMessage(apiResponse);
            if (!string.IsNullOrEmpty(content))
            {
                // Nettoyer la réponse des balises Markdown
                var cleanJson = content.Replace("```json", "").Replace("```", "").Trim();
                var evaluation = JsonSerializer.Deserialize<EvaluationApiResponse>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                var result = new EvaluationCompetence();
                result.Note = evaluation?.Note ?? 0;
                result.Analyse = evaluation?.Analyse;
                result.PointsForts = evaluation?.PointsForts;
                result.PointsAmeliorer = evaluation?.PointsAmeliorer;
                
                return result;
            }
            throw new Exception("Contenu vide");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du parsing : {ex.Message}");
            Console.WriteLine($"Réponse brute : {apiResponse.Substring(0, Math.Min(200, apiResponse.Length))}...");
            
            // Retourner une évaluation par défaut en cas d'erreur
            var errorResult = new EvaluationCompetence();
            errorResult.Note = 10;
            errorResult.Analyse = "Erreur lors de l'analyse automatique";
            errorResult.PointsForts = new List<string> { "Analyse non disponible" };
            errorResult.PointsAmeliorer = new List<string> { "Réessayer la correction" };
            
            return errorResult;
        }
    }

    /// <summary>
    /// Parse la réponse de l'évaluation finale
    /// </summary>
    private EvaluationFinaleApiResponse ParseEvaluationFinaleResponse(string apiResponse)
    {
        try
        {
            var content = ExtraireContenuMessage(apiResponse);
            if (!string.IsNullOrEmpty(content))
            {
                // Nettoyer la réponse des balises Markdown
                var cleanJson = content.Replace("```json", "").Replace("```", "").Trim();
                var evaluation = JsonSerializer.Deserialize<EvaluationFinaleApiResponse>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return evaluation ?? new EvaluationFinaleApiResponse
                {
                    Appreciation = "Erreur lors de la génération de l'appréciation",
                    PointsForts = new List<string> { "Analyse non disponible" },
                    PointsAmeliorer = new List<string> { "Réessayer la correction" }
                };
            }
            throw new Exception("Contenu vide");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du parsing de l'évaluation finale : {ex.Message}");
            Console.WriteLine($"Réponse brute : {apiResponse.Substring(0, Math.Min(200, apiResponse.Length))}...");
            
            // Retourner une évaluation par défaut en cas d'erreur
            return new EvaluationFinaleApiResponse
            {
                Appreciation = "Erreur lors de la génération de l'appréciation automatique",
                PointsForts = new List<string> { "Analyse non disponible" },
                PointsAmeliorer = new List<string> { "Réessayer la correction" }
            };
        }
    }    /// <summary>
    /// Extrait le contenu du message depuis une réponse LLM (OpenAI ou Ollama)
    /// </summary>
    private string ExtraireContenuMessage(string response)
    {
        try
        {
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;

            // Format OpenAI
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message))
                {
                    if (message.TryGetProperty("content", out var content))
                    {
                        return content.GetString() ?? "";
                    }
                }
            }

            // Format Ollama
            if (root.TryGetProperty("message", out var ollamaMessage))
            {
                if (ollamaMessage.TryGetProperty("content", out var ollamaContent))
                {
                    var contentStr = ollamaContent.GetString() ?? "";
                    
                    // Si la réponse Ollama contient du texte non-JSON, essayer d'extraire le JSON
                    if (!contentStr.Trim().StartsWith("{"))
                    {
                        // Chercher un bloc JSON dans la réponse
                        var jsonStart = contentStr.IndexOf('{');
                        var jsonEnd = contentStr.LastIndexOf('}');
                        
                        if (jsonStart >= 0 && jsonEnd > jsonStart)
                        {
                            contentStr = contentStr.Substring(jsonStart, jsonEnd - jsonStart + 1);
                        }
                    }
                    
                    return contentStr;
                }
            }

            // Si on ne trouve pas la structure attendue, retourner la réponse brute
            return response;
        }
        catch (JsonException)
        {            
            // Si ce n'est pas du JSON, c'est peut-être déjà le contenu pur
            // Essayer d'extraire un JSON de la réponse texte
            var responseText = response;
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }
            
            return response;
        }
    }    /// <summary>
    /// Lance le processus de correction avec consensus (multi-évaluations)
    /// </summary>
    public async Task<Correction> CorrigerCopieAvecConsensusAsync(Devoir devoir, string copie, int nombreEvaluations = 20, int maxParallelism = 5, bool aPAP = false)
    {
        Console.WriteLine("\n" + new string('═', 60));
        Console.WriteLine($"🤖 CORRECTION AVEC CONSENSUS ({nombreEvaluations} évaluations par compétence)");
        Console.WriteLine("🏛️  Simulation d'une commission d'harmonisation");
        Console.WriteLine(new string('═', 60));

        // Analyse linguistique de la copie
        var (nombreMots, motsUniques, richesseVocabulaire, motsPlusFrequents) = AnalyserMetriquesLinguistiques(copie);
        var mtld = CalculerMTLD(copie);
        var analyseLinguistique = InterpreterMTLD(mtld, nombreMots, devoir.Type ?? "dissertation", devoir.TypeBac ?? "général");
        
        Console.WriteLine($"📊 ANALYSE LINGUISTIQUE :");
        Console.WriteLine($"   • Longueur : {nombreMots} mots ({motsUniques} uniques, {richesseVocabulaire:F1}% de richesse)");
        var qualiteMTLD = mtld >= 50 ? "excellent" : mtld >= 40 ? "très bon" : mtld >= 30 ? "correct" : mtld >= 20 ? "faible" : "très faible";
        Console.WriteLine($"   • Diversité lexicale (MTLD) : {mtld:F1} ({qualiteMTLD})");
        Console.WriteLine($"   • Mots les plus fréquents : {string.Join(", ", motsPlusFrequents.Take(5))}");
        Console.WriteLine();

        var competences = devoir.Bareme?.Competences ?? new List<Competence>();
        
        // Filtrer les compétences si PAP
        if (aPAP)
        {
            Console.WriteLine("ℹ️  PAP activé : Les compétences d'expression ne seront pas évaluées.");
            if (devoir.Type?.ToLower() == "explication")
            {
                competences = competences.Where(c => c.Nom != "Expression et rédaction").ToList();
            }
            else
            {
                competences = competences.Where(c => c.Nom != "Maîtrise de la langue française").ToList();
            }
        }

        var evaluationsFinales = new List<EvaluationCompetence>();

        // Évaluation par compétence avec consensus
        for (int i = 0; i < competences.Count; i++)
        {
            var competence = competences[i];
            Console.WriteLine($"\n📋 Consensus pour la compétence {i + 1}/{competences.Count}:");
            Console.WriteLine($"   {competence.Nom}");            // Générer N corrections pour cette compétence EN PARALLÈLE
            var evaluationsMultiples = new List<EvaluationCompetence>();
            var startTime = DateTime.Now;
            
            // Configuration du parallélisme
            var semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);
            var tasks = new List<Task<EvaluationCompetence>>();
            var progressCounter = 0;
            
            Console.Write($"\r   📊 Démarrage de {nombreEvaluations} évaluations parallèles (max {maxParallelism} simultanées)");
            
            // Créer toutes les tâches
            for (int j = 0; j < nombreEvaluations; j++)
            {
                tasks.Add(EvaluerCompetenceAvecSemaphoreAsync(competence, copie, devoir.Enonce ?? "", devoir.Type ?? "dissertation", devoir.TypeBac ?? "général", aPAP, semaphore, () =>
                {
                    var current = Interlocked.Increment(ref progressCounter);
                    if (current % 10 == 0 || current == nombreEvaluations)
                    {
                        Console.Write($"\r   📊 Progression: {current}/{nombreEvaluations} évaluations terminées");
                    }
                }));
            }
            
            // Attendre que toutes les tâches se terminent
            var resultats = await Task.WhenAll(tasks);
            evaluationsMultiples.AddRange(resultats);
            
            var endTime = DateTime.Now;
            var duration = endTime - startTime;
              // Calculer le consensus
            var consensus = CalculerConsensus(evaluationsMultiples, competence.Nom ?? "Compétence sans nom");
            evaluationsFinales.Add(consensus);
            
            // Afficher les statistiques
            var notes = evaluationsMultiples.Select(e => e.Note).ToList();
            var ecartType = CalculerEcartTypeNotes(notes);
            var noteMin = notes.Min();
            var noteMax = notes.Max();
            var mediane = CalculerMediane(notes);
            
            Console.WriteLine($"\r   ✅ Consensus: {consensus.Note:F1}/20");
            Console.WriteLine($"      📊 Min: {noteMin:F1} | Max: {noteMax:F1} | Médiane: {mediane:F1} | Écart-type: {ecartType:F2}");
            Console.WriteLine($"      ⏱️  Temps: {duration.TotalSeconds:F1}s");
        }

        // Évaluation finale
        Console.WriteLine("\n🎯 Génération de l'évaluation finale...");
        var evaluationFinale = await EvaluerFinalAsync(evaluationsFinales, competences, copie, devoir.Type ?? "dissertation", devoir.TypeBac ?? "général", aPAP);

        // Afficher le résumé des coûts
        Console.WriteLine("\n" + new string('─', 60));
        _llmService.CostTracker?.DisplayCostSummary();

        // Calcul de la note moyenne avec pondération
        var notesFinales = evaluationsFinales.Select(e => e.Note).ToList();
        var notesFinalesDouble = notesFinales.Select(n => Convert.ToDouble(n)).ToList();
        var noteMoyenne = AppliquerPonderation(notesFinalesDouble, devoir.TypeBac ?? "général", evaluationsFinales, devoir.Type ?? "dissertation");

        // Affichage des statistiques finales
        Console.WriteLine($"\n📊 STATISTIQUES FINALES :");
        Console.WriteLine($"   • Note moyenne des compétences : {evaluationsFinales.Average(e => e.Note):F1}/20");
        Console.WriteLine($"   • Note finale après pondération : {noteMoyenne:F1}/20");
        Console.WriteLine($"   • Écart-type des notes finales : {CalculerEcartTypeNotes(notesFinales):F2}");

        // Création de la correction
        var corrections = await _dbService.LireCorrectionsAsync();
        var newId = corrections.Count > 0 ? corrections.Max(c => c.Id) + 1 : 1;

        var correction = new Correction
        {
            Id = newId,
            DevoirId = devoir.Id,
            Note = (decimal)noteMoyenne,
            Appreciation = evaluationFinale.Appreciation + $"\n\n[Analyse linguistique MTLD: {analyseLinguistique}]" + $"\n\n[Note : Cette correction a été réalisée avec un consensus de {nombreEvaluations} évaluations par compétence pour garantir une notation équitable et harmonisée.]",
            PointsForts = evaluationFinale.PointsForts,
            PointsAmeliorer = evaluationFinale.PointsAmeliorer,
            Competences = evaluationsFinales,
            Copie = copie,
            DateCorrection = DateTime.Now
        };

        corrections.Add(correction);
        await _dbService.SauvegarderCorrectionsAsync(corrections);

        return correction;
    }

    /// <summary>
    /// Évalue une compétence avec gestion du parallélisme via semaphore
    /// </summary>
    private async Task<EvaluationCompetence> EvaluerCompetenceAvecSemaphoreAsync(
        Competence competence, string copie, string enonce, string typeDevoir, string TypeBac, 
        bool aPAP, SemaphoreSlim semaphore, Action? onCompleted = null)
    {
        await semaphore.WaitAsync();
        try
        {
            var evaluation = await EvaluerCompetenceAsync(competence, copie, enonce, typeDevoir, TypeBac, aPAP);
            onCompleted?.Invoke();
            return evaluation;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Calcule le consensus à partir de multiples évaluations d'une compétence
    /// </summary>
    private EvaluationCompetence CalculerConsensus(List<EvaluationCompetence> evaluations, string nomCompetence)
    {
        var notes = evaluations.Select(e => e.Note).ToList();
        
        // Éliminer les outliers (méthode IQR)
        var notesFiltrees = EliminerOutliers(notes);
        
        return new EvaluationCompetence
        {
            Nom = nomCompetence,
            Note = notesFiltrees.Count > 0 ? notesFiltrees.Average() : notes.Average(),
            Analyse = SynthetiserAnalyses(evaluations),
            PointsForts = ExtrairePointsRecurrents(evaluations.SelectMany(e => e.PointsForts ?? new List<string>())),
            PointsAmeliorer = ExtrairePointsRecurrents(evaluations.SelectMany(e => e.PointsAmeliorer ?? new List<string>()))
        };
    }

    /// <summary>
    /// Élimine les outliers en utilisant la méthode IQR (Interquartile Range)
    /// </summary>
    private List<decimal> EliminerOutliers(List<decimal> notes)
    {
        if (notes.Count < 4) return notes; // Pas assez de données pour éliminer des outliers
        
        var sorted = notes.OrderBy(n => n).ToList();
        var q1Index = (int)(sorted.Count * 0.25);
        var q3Index = (int)(sorted.Count * 0.75);
        
        var q1 = sorted[q1Index];
        var q3 = sorted[q3Index];
        var iqr = q3 - q1;
        var lowerBound = q1 - 1.5m * iqr;
        var upperBound = q3 + 1.5m * iqr;
        
        var filtrees = sorted.Where(n => n >= lowerBound && n <= upperBound).ToList();
        
        // Si on élimine plus de 20% des données, on garde toutes les données
        if (filtrees.Count < notes.Count * 0.8)
        {
            return notes;
        }
        
        return filtrees;
    }

    /// <summary>
    /// Synthétise les analyses de multiples évaluations en une analyse consensus
    /// </summary>
    private string SynthetiserAnalyses(List<EvaluationCompetence> evaluations)
    {
        // Extraire les phrases les plus récurrentes
        var toutesAnalyses = evaluations.Select(e => e.Analyse ?? "").ToList();
        
        // Pour l'instant, on prend l'analyse médiane en termes de longueur
        // Dans une version plus avancée, on pourrait faire de l'analyse de sentiments
        var analysesTriees = toutesAnalyses.OrderBy(a => a.Length).ToList();
        var indexMedian = analysesTriees.Count / 2;
        
        var analyseBase = analysesTriees[indexMedian];
        
        return $"{analyseBase}\n\n[Cette analyse représente le consensus de {evaluations.Count} évaluations pour garantir l'objectivité.]";
    }

    /// <summary>
    /// Extrait les points récurrents d'une liste de points
    /// </summary>
    private List<string> ExtrairePointsRecurrents(IEnumerable<string> points)
    {
        // Compter la fréquence des points similaires
        var pointsFrequence = new Dictionary<string, int>();
        
        foreach (var point in points)
        {
            var pointNormalise = NormaliserTexte(point);
            if (pointsFrequence.ContainsKey(pointNormalise))
            {
                pointsFrequence[pointNormalise]++;
            }
            else
            {
                pointsFrequence[pointNormalise] = 1;
            }
        }
        
        // Retourner les points qui apparaissent au moins 2 fois (pour éviter les points uniques)
        var seuilMinimum = Math.Max(2, pointsFrequence.Count / 10); // Au moins 2, ou 10% des points
        
        return pointsFrequence
            .Where(kv => kv.Value >= seuilMinimum)
            .OrderByDescending(kv => kv.Value)
            .Take(5) // Maximum 5 points
            .Select(kv => kv.Key)
            .ToList();
    }

    /// <summary>
    /// Normalise un texte pour la comparaison (minuscules, suppression ponctuation, etc.)
    /// </summary>
    private string NormaliserTexte(string texte)
    {
        if (string.IsNullOrEmpty(texte)) return "";
        
        return texte.ToLower()
                   .Replace(".", "")
                   .Replace(",", "")
                   .Replace("!", "")
                   .Replace("?", "")
                   .Replace(";", "")
                   .Replace(":", "")
                   .Trim();
    }

    /// <summary>
    /// Calcule l'écart-type d'une liste de notes
    /// </summary>
    private double CalculerEcartTypeNotes(List<decimal> notes)
    {
        if (notes.Count <= 1) return 0;

        var moyenne = notes.Average();
        var variance = notes.Sum(x => Math.Pow((double)(x - moyenne), 2)) / notes.Count;
        return Math.Sqrt(variance);
    }

    /// <summary>
    /// Calcule la médiane d'une liste de notes
    /// </summary>
    private decimal CalculerMediane(List<decimal> notes)
    {
        var sorted = notes.OrderBy(n => n).ToList();
        var count = sorted.Count;
        
        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
        }
        else
        {
            return sorted[count / 2];
        }
    }

    /// <summary>
    /// Analyse les métriques linguistiques d'une copie
    /// </summary>
    private (int nombreMots, int motsUniques, double richesseVocabulaire, List<string> motsPlusFrequents) AnalyserMetriquesLinguistiques(string copie)
    {
        if (string.IsNullOrWhiteSpace(copie))
            return (0, 0, 0, new List<string>());

        // Nettoyer et diviser le texte en mots
        var mots = copie
            .ToLower()
            .Split(new char[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '-', '—', '…' }, 
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(mot => mot.Length > 2) // Ignorer les mots trop courts
            .Where(mot => !EstMotVideStatic(mot)) // Ignorer les mots vides
            .ToList();

        var nombreMots = mots.Count;
        var motsUniques = mots.Distinct().Count();
        var richesseVocabulaire = nombreMots > 0 ? (double)motsUniques / nombreMots * 100 : 0;

        // Analyser la fréquence des mots
        var frequenceMots = mots
            .GroupBy(mot => mot)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();

        return (nombreMots, motsUniques, richesseVocabulaire, frequenceMots);
    }

    /// <summary>
    /// Analyse les métriques linguistiques d'une copie (version publique statique)
    /// </summary>
    public static (int nombreMots, int motsUniques, double richesseVocabulaire, List<string> motsPlusFrequents) AnalyserMetriquesLinguistiquesStatic(string copie)
    {
        if (string.IsNullOrWhiteSpace(copie))
            return (0, 0, 0, new List<string>());

        // Nettoyer et diviser le texte en mots
        var mots = copie
            .ToLower()
            .Split(new char[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '-', '—', '…' }, 
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(mot => mot.Length > 2) // Ignorer les mots trop courts
            .Where(mot => !EstMotVideStatic(mot)) // Ignorer les mots vides
            .ToList();

        var nombreMots = mots.Count;
        var motsUniques = mots.Distinct().Count();
        var richesseVocabulaire = nombreMots > 0 ? (double)motsUniques / nombreMots * 100 : 0;

        // Analyser la fréquence des mots
        var frequenceMots = mots
            .GroupBy(mot => mot)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();

        return (nombreMots, motsUniques, richesseVocabulaire, frequenceMots);
    }

    /// <summary>
    /// Détermine si un mot est un mot vide (version statique)
    /// </summary>
    private static bool EstMotVideStatic(string mot)
    {
        var motsVides = new HashSet<string>
        {
            "le", "la", "les", "un", "une", "des", "du", "de", "d'", "et", "ou", "où", "est", "sont", "était", "étaient",
            "a", "ai", "as", "ont", "avait", "avaient", "aura", "auront", "sera", "seront", "serait", "seraient",
            "ce", "cette", "ces", "cet", "se", "s'", "si", "sa", "son", "ses", "leur", "leurs", "notre", "nos", "votre", "vos",
            "je", "tu", "il", "elle", "nous", "vous", "ils", "elles", "me", "te", "lui", "nous", "vous", "leur",
            "que", "qui", "quoi", "dont", "lequel", "laquelle", "lesquels", "lesquelles",
            "dans", "sur", "sous", "avec", "sans", "pour", "par", "vers", "chez", "entre", "parmi", "selon", "malgré",
            "mais", "car", "donc", "or", "ni", "cependant", "néanmoins", "toutefois", "pourtant", "ainsi", "alors", "aussi",
            "très", "plus", "moins", "assez", "trop", "bien", "mal", "mieux", "beaucoup", "peu", "tant", "autant",
            "ici", "là", "hier", "aujourd'hui", "demain", "maintenant", "déjà", "encore", "toujours", "jamais", "parfois"
        };

        return motsVides.Contains(mot);
    }

    /// <summary>
    /// Évalue la qualité linguistique d'une copie (version statique)
    /// </summary>
    public static string EvaluerQualiteLinguistiqueStatic(int nombreMots, double richesseVocabulaire)
    {
        string evaluationLongueur;
        string evaluationRichesse;

        // Évaluation de la longueur
        if (nombreMots < 300)
            evaluationLongueur = "très courte";
        else if (nombreMots < 500)
            evaluationLongueur = "courte";
        else if (nombreMots < 800)
            evaluationLongueur = "correcte";
        else if (nombreMots < 1200)
            evaluationLongueur = "développée";
        else
            evaluationLongueur = "très développée";

        // Évaluation de la richesse vocabulaire
        if (richesseVocabulaire < 30)
            evaluationRichesse = "vocabulaire limité";
        else if (richesseVocabulaire < 40)
            evaluationRichesse = "vocabulaire correct";
        else if (richesseVocabulaire < 50)
            evaluationRichesse = "vocabulaire riche";
        else if (richesseVocabulaire < 60)
            evaluationRichesse = "vocabulaire très riche";
        else
            evaluationRichesse = "vocabulaire exceptionnel";

        return $"Copie {evaluationLongueur} ({nombreMots} mots) avec un {evaluationRichesse} ({richesseVocabulaire:F1}% de mots uniques)";
    }

    /// <summary>
    /// Analyse les métriques linguistiques avec l'aide du LLM pour une interprétation pédagogique
    /// </summary>
    /// <summary>
    /// Calcule le MTLD (Measure of Textural Lexical Diversity) d'un texte
    /// </summary>
    private double CalculerMTLD(string copie, double seuil = 0.72)
    {
        if (string.IsNullOrWhiteSpace(copie))
            return 0;

        // Nettoyer et diviser le texte en mots
        var mots = copie
            .ToLower()
            .Split(new char[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '-', '—', '…' }, 
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(mot => mot.Length > 2)
            .Where(mot => !EstMotVide(mot))
            .ToList();

        if (mots.Count < 10) return 0; // Trop peu de mots pour calculer le MTLD

        // Analyse de gauche à droite
        var segmentsGaucheDroite = CalculerSegmentsMTLD(mots, seuil);
        
        // Analyse de droite à gauche
        var motsInverses = mots.ToList();
        motsInverses.Reverse();
        var segmentsDroiteGauche = CalculerSegmentsMTLD(motsInverses, seuil);

        // Calcul du MTLD final (moyenne des deux directions)
        var mtldGaucheDroite = segmentsGaucheDroite.Count > 0 ? segmentsGaucheDroite.Average() : 0;
        var mtldDroiteGauche = segmentsDroiteGauche.Count > 0 ? segmentsDroiteGauche.Average() : 0;

        return (mtldGaucheDroite + mtldDroiteGauche) / 2.0;
    }

    /// <summary>
    /// Calcule les segments MTLD pour une direction donnée
    /// </summary>
    private List<int> CalculerSegmentsMTLD(List<string> mots, double seuil)
    {
        var segments = new List<int>();
        var motsVus = new HashSet<string>();
        var indexDebut = 0;

        for (int i = 0; i < mots.Count; i++)
        {
            motsVus.Add(mots[i]);
            var ttr = (double)motsVus.Count / (i - indexDebut + 1);

            if (ttr < seuil)
            {
                // Fin d'un segment
                segments.Add(i - indexDebut + 1);
                motsVus.Clear();
                indexDebut = i + 1;
            }
        }

        // Ajouter le dernier segment partiel s'il y en a un
        if (indexDebut < mots.Count)
        {
            var dernierSegment = mots.Count - indexDebut;
            if (dernierSegment > 5) // Seulement si le segment est assez long
            {
                segments.Add(dernierSegment);
            }
        }

        return segments;
    }

    /// <summary>
    /// Version statique du calcul MTLD
    /// </summary>
    public static double CalculerMTLDStatic(string copie, double seuil = 0.72)
    {
        if (string.IsNullOrWhiteSpace(copie))
            return 0;

        // Nettoyer et diviser le texte en mots
        var mots = copie
            .ToLower()
            .Split(new char[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '-', '—', '…' }, 
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(mot => mot.Length > 2)
            .Where(mot => !EstMotVideStatic(mot))
            .ToList();

        if (mots.Count < 10) return 0;

        // Analyse de gauche à droite
        var segmentsGaucheDroite = CalculerSegmentsMTLDStatic(mots, seuil);
        
        // Analyse de droite à gauche
        var motsInverses = mots.ToList();
        motsInverses.Reverse();
        var segmentsDroiteGauche = CalculerSegmentsMTLDStatic(motsInverses, seuil);

        var mtldGaucheDroite = segmentsGaucheDroite.Count > 0 ? segmentsGaucheDroite.Average() : 0;
        var mtldDroiteGauche = segmentsDroiteGauche.Count > 0 ? segmentsDroiteGauche.Average() : 0;

        return (mtldGaucheDroite + mtldDroiteGauche) / 2.0;
    }

    /// <summary>
    /// Version statique du calcul des segments MTLD
    /// </summary>
    private static List<int> CalculerSegmentsMTLDStatic(List<string> mots, double seuil)
    {
        var segments = new List<int>();
        var motsVus = new HashSet<string>();
        var indexDebut = 0;

        for (int i = 0; i < mots.Count; i++)
        {
            motsVus.Add(mots[i]);
            var ttr = (double)motsVus.Count / (i - indexDebut + 1);

            if (ttr < seuil)
            {
                segments.Add(i - indexDebut + 1);
                motsVus.Clear();
                indexDebut = i + 1;
            }
        }

        if (indexDebut < mots.Count)
        {
            var dernierSegment = mots.Count - indexDebut;
            if (dernierSegment > 5)
            {
                segments.Add(dernierSegment);
            }
        }

        return segments;
    }

    /// <summary>
    /// Version statique de l'analyse linguistique avec LLM (pour l'affichage des résultats)
    /// </summary>
    public static async Task<string> AnalyserMetriquesAvecLLMStaticAsync(ILLMService llmService, int nombreMots, double richesseVocabulaire, List<string> motsPlusFrequents, string typeDevoir, string typeBac)
    {
        var system = @"Vous êtes un professeur de philosophie expérimenté qui analyse les métriques linguistiques des copies d'élèves du baccalauréat. 
Votre rôle est d'interpréter les données quantitatives pour donner des conseils pédagogiques pertinents et constructifs.
Répondez de manière concise et bienveillante, en vous adressant directement à l'élève (vouvoiement).";

        var motsPlusFrequentsText = string.Join(", ", motsPlusFrequents.Take(8));
        var niveauAttendu = typeBac == "technologique" ? "bac technologique" : "bac général";
        
        var prompt = $@"Analysez ces métriques linguistiques d'une copie de {typeDevoir} de {niveauAttendu} :

📊 DONNÉES :
- Nombre de mots : {nombreMots}
- Richesse du vocabulaire : {richesseVocabulaire:F1}% de mots uniques
- Mots les plus fréquents : {motsPlusFrequentsText}

CONTEXTE :
- Type de devoir : {typeDevoir}
- Niveau : {niveauAttendu}

Donnez une analyse pédagogique en 2-3 phrases maximum qui :
1. Évalue la longueur par rapport aux attentes du bac
2. Commente la richesse vocabulaire (attention aux répétitions)
3. Identifie si les mots fréquents révèlent une bonne maîtrise du sujet
4. Donne un conseil constructif si nécessaire

Soyez bienveillant mais précis. Répondez directement sans préambule.";

        try
        {
            var response = await llmService.AskAsync(system, prompt, "Analyse linguistique");
            
            // Extraire le contenu du message
            string analyseLLM = "";
            try
            {
                using var document = JsonDocument.Parse(response);
                var root = document.RootElement;

                // Format OpenAI
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message))
                    {
                        if (message.TryGetProperty("content", out var content))
                        {
                            analyseLLM = content.GetString() ?? "";
                        }
                    }
                }

                // Format Ollama
                if (string.IsNullOrEmpty(analyseLLM) && root.TryGetProperty("message", out var ollamaMessage))
                {
                    if (ollamaMessage.TryGetProperty("content", out var ollamaContent))
                    {
                        analyseLLM = ollamaContent.GetString() ?? "";
                    }
                }
            }
            catch (JsonException)
            {
                // Si ce n'est pas du JSON, c'est peut-être le contenu direct
                analyseLLM = response;
            }
            
            return analyseLLM?.Trim() ?? "Analyse linguistique non disponible.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Erreur lors de l'analyse linguistique LLM : {ex.Message}");

            // Fallback vers l'analyse basique
            return EvaluerQualiteLinguistiqueStatic(nombreMots, richesseVocabulaire);
        }
    }

    /// <summary>
    /// Détermine si un mot est un mot vide (version instance)
    /// </summary>
    /// <summary>
    /// Interprète le score MTLD pour donner des conseils pédagogiques
    /// </summary>
    private string InterpreterMTLD(double mtld, int nombreMots, string typeDevoir, string typeBac)
    {
        return InterpreterMTLDStatic(mtld, nombreMots, typeDevoir, typeBac);
    }

    /// <summary>
    /// Version statique de l'interprétation du score MTLD
    /// </summary>
    public static string InterpreterMTLDStatic(double mtld, int nombreMots, string typeDevoir, string typeBac)
    {
        string niveauAttendu = typeBac == "technologique" ? "bac technologique" : "bac général";
        string contexte = typeDevoir == "explication" ? "explication de texte" : "dissertation philosophique";
        
        // Interprétation du score MTLD selon les standards académiques
        string interpretation;
        
        if (mtld >= 50)
        {
            interpretation = "excellent";
        }
        else if (mtld >= 40)
        {
            interpretation = "très satisfaisant";
        }
        else if (mtld >= 30)
        {
            interpretation = "satisfaisant";
        }
        else if (mtld >= 20)
        {
            interpretation = "insuffisant";
        }
        else
        {
            interpretation = "très insuffisant";
        }

        return $"Diversité lexicale {interpretation} (MTLD: {mtld:F1}).";
    }

    private bool EstMotVide(string mot)
    {
        var motsVides = new HashSet<string>
        {
            "le", "la", "les", "un", "une", "des", "du", "de", "d'", "et", "ou", "où", "est", "sont", "était", "étaient",
            "a", "ai", "as", "ont", "avait", "avaient", "aura", "auront", "sera", "seront", "serait", "seraient",
            "ce", "cette", "ces", "cet", "se", "s'", "si", "sa", "son", "ses", "leur", "leurs", "notre", "nos", "votre", "vos",
            "je", "tu", "il", "elle", "nous", "vous", "ils", "elles", "me", "te", "lui", "nous", "vous", "leur",
            "que", "qui", "quoi", "dont", "lequel", "laquelle", "lesquels", "lesquelles",
            "dans", "sur", "sous", "avec", "sans", "pour", "par", "vers", "chez", "entre", "parmi", "selon", "malgré",
            "mais", "car", "donc", "or", "ni", "cependant", "néanmoins", "toutefois", "pourtant", "ainsi", "alors", "aussi",
            "très", "plus", "moins", "assez", "trop", "bien", "mal", "mieux", "beaucoup", "peu", "tant", "autant",
            "ici", "là", "hier", "aujourd'hui", "demain", "maintenant", "déjà", "encore", "toujours", "jamais", "parfois"
        };

        return motsVides.Contains(mot);
    }
}
