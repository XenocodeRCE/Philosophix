using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

class Program
{    static async Task Main(string[] args)
    {
        var dbService = new JsonDatabaseService("devoirs.json");
        var openAiService = new OpenAiService();
        var correctionService = new CorrectionService(openAiService, dbService);
        var annotationService = new AnnotationService(openAiService, dbService);

        while (true)
        {
            //Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║              Philosophix CLI v2.0              ║");
            Console.WriteLine("║        Correction automatisée de copies        ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝");
            Console.WriteLine();            Console.WriteLine("1. Créer un devoir");
            Console.WriteLine("2. Voir les devoirs");
            Console.WriteLine("3. Corriger une copie");
            Console.WriteLine("4. Voir les corrections");
            Console.WriteLine("5. Annoter une copie");
            Console.WriteLine("6. Voir les annotations");
            Console.WriteLine("7. Réinitialiser le compteur de coûts");
            Console.WriteLine("8. Quitter");
            Console.WriteLine();
            Console.Write("Votre choix : ");

            var choix = Console.ReadLine();

            switch (choix)
            {
                case "1":
                    await CreerDevoirAsync(dbService);
                    break;
                case "2":
                    await VoirDevoirsAsync(dbService);
                    break;                case "3":
                    await CorrigerCopieAsync(dbService, correctionService);
                    break;
                case "4":
                    await VoirCorrectionsAsync(dbService);
                    break;
                case "5":
                    await AnnoterCopieAsync(dbService, annotationService);
                    break;
                case "6":
                    await VoirAnnotationsAsync(dbService, annotationService);
                    break;
                case "7":
                    openAiService.CostTracker.Reset();
                    break;
                case "8":
                    Console.WriteLine("Au revoir !");
                    return;
                default:
                    Console.WriteLine("Choix invalide.");
                    break;
            }            if (choix != "8")
            {
                Console.WriteLine("\nAppuyez sur une touche pour continuer...");
                Console.ReadKey();
            }
        }
    }

    static async Task CreerDevoirAsync(JsonDatabaseService dbService)
    {
        Console.WriteLine("\n--- Création d'un nouveau devoir ---");

        Console.Write("Titre : ");
        var titre = Console.ReadLine();

        Console.Write("Énoncé : ");
        var enonce = Console.ReadLine() ?? string.Empty;

        Console.Write("Type (1 = dissertation, 2 = explication) : ");
        var typeInput = Console.ReadLine();
        string type = typeInput == "2" ? "explication" : "dissertation";

        // Demander si c'est un devoir pour le bac techno ou général, 1 ou 2
        Console.Write("Type de bac (1 = général, 2 = techno) : ");
        var typeBacInput = Console.ReadLine();
        string bacType = typeBacInput == "2" ? "technologique" : "général";

        var devoirs = await dbService.LireDevoirsAsync();
        var newId = devoirs.Count > 0 ? devoirs[devoirs.Count - 1].Id + 1 : 1;

        var bareme = GetBaremeForType(type);

        var nouveauDevoir = new Devoir
        {
            Id = newId,
            Titre = titre,
            Enonce = enonce,
            Contenu = "n/a",
            DateCreation = DateTime.Now,
            Bareme = bareme,
            Type = type,
            TypeBac = bacType
        };

        devoirs.Add(nouveauDevoir);
        await dbService.SauvegarderDevoirsAsync(devoirs);

        Console.WriteLine("\nDevoir créé avec succès !");
    }

    static async Task VoirDevoirsAsync(JsonDatabaseService dbService)
    {
        Console.WriteLine("\n--- Liste des devoirs ---");
        var devoirs = await dbService.LireDevoirsAsync();

        if (devoirs.Count == 0)
        {
            Console.WriteLine("Aucun devoir pour le moment.");
            return;
        }

        foreach (var devoir in devoirs)
        {
            Console.WriteLine($"ID: {devoir.Id} | Type: {devoir.Type} | Titre: {devoir.Titre} | Créé le: {devoir.DateCreation.ToShortDateString()}");
            Console.WriteLine($"  Énoncé: {devoir.Enonce?.Substring(0, Math.Min(devoir.Enonce.Length, 50))}...");
            Console.WriteLine("-----------------------------------------------------");
        }
    }

    static Bareme GetBaremeForType(string? type)
    {
        if (type?.ToLower() == "explication")
        {
            return new Bareme
            {
                Competences = new List<Competence>
                {
                    new Competence { Id = 1, Nom = "Lecture analytique et compréhension globale", Criteres = new List<string> { "Identification de la thèse principale", "Repérage de la structure générale", "Compréhension des enjeux du texte" } },
                    new Competence { Id = 2, Nom = "Analyse de la structure argumentative", Criteres = new List<string> { "Découpage en séquences logiques", "Repérage des articulations", "Identification des mouvements du texte" } },
                    new Competence { Id = 3, Nom = "Analyse conceptuelle", Criteres = new List<string> { "Définition des concepts clés", "Compréhension des distinctions conceptuelles", "Mise en relation des notions" } },
                    new Competence { Id = 4, Nom = "Analyse argumentative", Criteres = new List<string> { "Reconstruction des raisonnements", "Identification des types d'arguments", "Repérage des exemples et illustrations" } },
                    new Competence { Id = 5, Nom = "Contextualisation philosophique", Criteres = new List<string> { "Situation historique", "Liens avec d'autres auteurs", "Mobilisation des connaissances du cours" } },
                    new Competence { Id = 6, Nom = "Expression et rédaction", Criteres = new List<string> { "Clarté de l'explication", "Précision du vocabulaire", "Structure de l'explication" } },
                    new Competence { Id = 7, Nom = "Appropriation critique", Criteres = new List<string> { "Évaluation de la cohérence", "Discussion des arguments", "Prolongements pertinents" } }
                }
            };
        }
        else
        {
            return new Bareme
            {
                Competences = new List<Competence>
                {
                    new Competence { Id = 1, Nom = "Compréhension et analyse du sujet", Criteres = new List<string> { "Identifier les termes clés d'un sujet", "Reformuler le sujet avec ses propres mots", "Formuler une problématique pertinente" } },
                    new Competence { Id = 2, Nom = "Élaboration d'un plan structuré", Criteres = new List<string> { "Organiser ses idées de manière logique", "Connaître les différents types de plans", "Annoncer clairement son plan dans l'introduction" } },
                    new Competence { Id = 3, Nom = "Rédaction de l'introduction et de la conclusion", Criteres = new List<string> { "Rédiger une accroche efficace", "Maîtriser les étapes clés de l'introduction", "Synthétiser et ouvrir la réflexion" } },
                    new Competence { Id = 4, Nom = "Développement de l'argumentation", Criteres = new List<string> { "Construire des paragraphes argumentatifs", "Utiliser des exemples pertinents", "Intégrer des références" } },
                    new Competence { Id = 5, Nom = "Maîtrise de la langue française", Criteres = new List<string> { "Orthographe et grammaire", "Vocabulaire spécifique", "Fluidité de la syntaxe" } },
                    new Competence { Id = 6, Nom = "Cohérence et cohésion textuelle", Criteres = new List<string> { "Utiliser des connecteurs logiques", "Assurer la cohérence entre les parties", "Contribuer à la problématique" } },
                    new Competence { Id = 7, Nom = "Esprit critique et réflexion personnelle", Criteres = new List<string> { "Prise de position argumentée", "Évaluer les arguments", "Réflexion personnelle" } }
                }
            };
        }
    }

    static async Task CorrigerCopieAsync(JsonDatabaseService dbService, CorrectionService correctionService)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║                CORRECTION DE COPIE                   ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");

        // Afficher les devoirs disponibles
        var devoirs = await dbService.LireDevoirsAsync();
        if (devoirs.Count == 0)
        {
            Console.WriteLine("Aucun devoir disponible. Créez d'abord un devoir.");
            return;
        }

        Console.WriteLine("\nDevoirs disponibles :");
        foreach (var devoir in devoirs)
        {
            Console.WriteLine($"{devoir.Id}. {devoir.Titre} ({devoir.Type})");
        }

        Console.Write("\nSélectionnez l'ID du devoir : ");
        if (!int.TryParse(Console.ReadLine(), out int devoirId))
        {
            Console.WriteLine("ID invalide.");
            return;
        }

        var devoirSelectionne = devoirs.FirstOrDefault(d => d.Id == devoirId);
        if (devoirSelectionne == null)
        {
            Console.WriteLine("Devoir introuvable.");
            return;
        }
        Console.WriteLine($"\nDevoir sélectionné : {devoirSelectionne.Titre}");
        Console.WriteLine($"Énoncé : {devoirSelectionne.Enonce}");
        Console.WriteLine($"Type : {devoirSelectionne.Type}");

        // Question sur le PAP
        Console.WriteLine("\n" + new string('─', 60));
        Console.WriteLine("INFORMATIONS SUR L'ÉLÈVE");
        Console.WriteLine(new string('─', 60));
        Console.Write("L'élève dispose-t-il d'un PAP (Plan d'Accompagnement Personnalisé) ? (1 = Oui, 2 = Non) : ");
        var reponsePAP = Console.ReadLine();
        bool aPAP = reponsePAP == "1";

        if (aPAP)
        {
            Console.WriteLine("ℹ️  PAP détecté : La qualité de l'expression ne sera pas évaluée.");
        }

        // Saisie de la copie
        Console.WriteLine("\n" + new string('─', 60));
        Console.WriteLine("SAISIE DE LA COPIE À CORRIGER");
        Console.WriteLine(new string('─', 60));
        Console.WriteLine("1. Saisir le texte directement");
        Console.WriteLine("2. Charger depuis un fichier");
        Console.Write("Votre choix : ");

        var choixCopie = Console.ReadLine();
        string copie = "";

        switch (choixCopie)
        {
            case "1":
                Console.WriteLine("\nSaisissez la copie (terminez par une ligne vide) :");                var lignes = new List<string>();
                string? ligne;
                while (!string.IsNullOrWhiteSpace(ligne = Console.ReadLine()))
                {
                    lignes.Add(ligne);
                }
                copie = string.Join("\n", lignes);
                break;

            case "2":
                Console.Write("Chemin du fichier : ");
                var cheminFichier = Console.ReadLine();
                if (File.Exists(cheminFichier))
                {
                    copie = await File.ReadAllTextAsync(cheminFichier);
                }
                else
                {
                    Console.WriteLine("Fichier introuvable.");
                    return;
                }
                break;

            default:
                Console.WriteLine("Choix invalide.");
                return;
        }

        if (!CorrectionService.ValiderCopie(copie))
        {
            Console.WriteLine("La copie doit contenir au moins 500 caractères.");
            return;
        }        // Correction de la copie
        try
        {
            var correction = await correctionService.CorrigerCopieAsync(devoirSelectionne, copie, aPAP);
            CorrectionService.AfficherResultatsCorrection(correction, devoirSelectionne.Bareme?.Competences ?? new List<Competence>());
            CorrectionService.ExporterCorrectionAsync(correction, devoirSelectionne).Wait();

            // Proposer l'annotation automatique après la correction
            Console.WriteLine("\n" + new string('─', 60));
            Console.Write("🔍 Voulez-vous générer des annotations pour cette copie ? (o/n) : ");
            var choixAnnotation = Console.ReadLine()?.ToLower();
            
            if (choixAnnotation == "o" || choixAnnotation == "oui")
            {
                await GenererAnnotationsPourCorrection(correction, devoirSelectionne);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erreur lors de la correction : {ex.Message}");
        }
    }

    static async Task VoirCorrectionsAsync(JsonDatabaseService dbService)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║                 LISTE DES CORRECTIONS                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");

        var corrections = await dbService.LireCorrectionsAsync();
        var devoirs = await dbService.LireDevoirsAsync();

        if (corrections.Count == 0)
        {
            Console.WriteLine("Aucune correction pour le moment.");
            return;
        }

        foreach (var correction in corrections.OrderByDescending(c => c.DateCorrection))
        {
            var devoir = devoirs.FirstOrDefault(d => d.Id == correction.DevoirId);
            Console.WriteLine($"\n📝 Correction #{correction.Id}");
            Console.WriteLine($"   Devoir : {devoir?.Titre ?? "Inconnu"}");
            Console.WriteLine($"   Note : {correction.Note:F1}/20");
            Console.WriteLine($"   Date : {correction.DateCorrection:dd/MM/yyyy HH:mm}");
            Console.WriteLine($"   Appréciation : {correction.Appreciation?.Substring(0, Math.Min(correction.Appreciation.Length, 100))}...");
            Console.WriteLine(new string('─', 50));
        }

        Console.Write("\nVoulez-vous voir le détail d'une correction ? (ID ou 'n' pour non) : ");
        var choix = Console.ReadLine();
        
        if (int.TryParse(choix, out int correctionId))
        {
            var correction = corrections.FirstOrDefault(c => c.Id == correctionId);
            if (correction != null)
            {
                var devoir = devoirs.FirstOrDefault(d => d.Id == correction.DevoirId);
                CorrectionService.AfficherResultatsCorrection(correction, devoir?.Bareme?.Competences ?? new List<Competence>());
            }
            else
            {
                Console.WriteLine("Correction introuvable.");
            }
        }
    }

    /// <summary>
    /// Génère des annotations pour une correction donnée
    /// </summary>
    static async Task GenererAnnotationsPourCorrection(Correction correction, Devoir devoir)
    {        try
        {
            var openAiService = new OpenAiService();
            var dbService = new JsonDatabaseService("devoirs.json");
            var annotationService = new AnnotationService(openAiService, dbService);

            Console.WriteLine("\n" + new string('═', 60));
            Console.WriteLine("🔍 GÉNÉRATION D'ANNOTATIONS AUTOMATIQUES");
            Console.WriteLine(new string('═', 60));

            var startTime = DateTime.Now;
            var annotations = await annotationService.GenererAnnotationsAvecTypes(correction);
            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            Console.WriteLine($"\n⏱️  Durée de génération : {duration.TotalSeconds:F1}s");
            Console.WriteLine($"💰 Coût estimé affiché dans les logs API");
            
            AnnotationService.AfficherAnnotations(annotations);

            // Proposer l'export des annotations
            Console.Write("\n📄 Voulez-vous exporter les annotations ? (o/n) : ");
            var choixExport = Console.ReadLine()?.ToLower();
            
            if (choixExport == "o" || choixExport == "oui")
            {
                await AnnotationService.ExporterAnnotationsAsync(annotations, correction, devoir);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erreur lors de la génération d'annotations : {ex.Message}");
        }
    }

    /// <summary>
    /// Menu pour annoter une copie déjà corrigée
    /// </summary>
    static async Task AnnoterCopieAsync(JsonDatabaseService dbService, AnnotationService annotationService)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║                ANNOTATION DE COPIE                   ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");

        var corrections = await dbService.LireCorrectionsAsync();
        if (corrections.Count == 0)
        {
            Console.WriteLine("Aucune correction disponible. Corrigez d'abord une copie.");
            return;
        }

        Console.WriteLine("\nCorrections disponibles :");
        var devoirs = await dbService.LireDevoirsAsync();
        
        foreach (var correction in corrections.OrderByDescending(c => c.DateCorrection))
        {
            var devoir = devoirs.FirstOrDefault(d => d.Id == correction.DevoirId);
            Console.WriteLine($"{correction.Id}. {devoir?.Titre ?? "Inconnu"} - Note: {correction.Note:F1}/20 - {correction.DateCorrection:dd/MM/yyyy}");
        }

        Console.Write("\nSélectionnez l'ID de la correction à annoter : ");
        if (!int.TryParse(Console.ReadLine(), out int correctionId))
        {
            Console.WriteLine("ID invalide.");
            return;
        }

        var correctionSelectionnee = corrections.FirstOrDefault(c => c.Id == correctionId);
        if (correctionSelectionnee == null)
        {
            Console.WriteLine("Correction introuvable.");
            return;
        }

        var devoirAssocie = devoirs.FirstOrDefault(d => d.Id == correctionSelectionnee.DevoirId);
        if (devoirAssocie == null)
        {
            Console.WriteLine("Devoir associé introuvable.");
            return;
        }

        // Vérifier si des annotations existent déjà
        var annotationsExistantes = await annotationService.ChargerAnnotationsAsync(correctionId);
        if (annotationsExistantes != null)
        {
            Console.WriteLine("\n⚠️  Des annotations existent déjà pour cette correction.");
            Console.Write("Voulez-vous les régénérer ? (o/n) : ");
            var choixRegenerer = Console.ReadLine()?.ToLower();
            
            if (choixRegenerer != "o" && choixRegenerer != "oui")
            {
                AnnotationService.AfficherAnnotations(annotationsExistantes);
                return;
            }
        }

        await GenererAnnotationsPourCorrection(correctionSelectionnee, devoirAssocie);
    }

    /// <summary>
    /// Menu pour voir les annotations existantes
    /// </summary>
    static async Task VoirAnnotationsAsync(JsonDatabaseService dbService, AnnotationService annotationService)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║                 LISTE DES ANNOTATIONS                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");

        var corrections = await dbService.LireCorrectionsAsync();
        var devoirs = await dbService.LireDevoirsAsync();

        if (corrections.Count == 0)
        {
            Console.WriteLine("Aucune correction disponible.");
            return;
        }

        // Lister les corrections qui ont des annotations
        var correctionsAvecAnnotations = new List<(Correction correction, Devoir? devoir, AnnotationResponse annotations)>();

        foreach (var correction in corrections)
        {
            var annotations = await annotationService.ChargerAnnotationsAsync(correction.Id);
            if (annotations != null)
            {
                var devoir = devoirs.FirstOrDefault(d => d.Id == correction.DevoirId);
                correctionsAvecAnnotations.Add((correction, devoir, annotations));
            }
        }

        if (!correctionsAvecAnnotations.Any())
        {
            Console.WriteLine("Aucune annotation disponible. Annotez d'abord une copie.");
            return;
        }

        Console.WriteLine("\nCorrections avec annotations :");
        foreach (var (correction, devoir, annotations) in correctionsAvecAnnotations.OrderByDescending(x => x.correction.DateCorrection))
        {
            Console.WriteLine($"{correction.Id}. {devoir?.Titre ?? "Inconnu"} - {annotations.Annotations?.Count ?? 0} annotations - {correction.DateCorrection:dd/MM/yyyy}");
        }

        Console.Write("\nSélectionnez l'ID de la correction (ou 'n' pour annuler) : ");
        var choix = Console.ReadLine();
        
        if (int.TryParse(choix, out int correctionId))
        {
            var element = correctionsAvecAnnotations.FirstOrDefault(x => x.correction.Id == correctionId);
            if (element.correction != null)
            {
                AnnotationService.AfficherAnnotations(element.annotations);
                
                Console.Write("\n📄 Voulez-vous exporter ces annotations ? (o/n) : ");
                var choixExport = Console.ReadLine()?.ToLower();
                
                if (choixExport == "o" || choixExport == "oui")
                {
                    await AnnotationService.ExporterAnnotationsAsync(element.annotations, element.correction, element.devoir ?? new Devoir());
                }
            }
            else
            {
                Console.WriteLine("Correction introuvable.");
            }
        }
    }
}
