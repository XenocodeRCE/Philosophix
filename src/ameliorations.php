<?php
require_once 'config.php';

if (!isset($_GET['id'])) {
    header('Location: index.php');
    exit;
}

try {
    // Chercher d'abord dans la base de données
    $stmt = $pdo->prepare("SELECT * FROM ameliorations WHERE correction_id = ?");
    $stmt->execute([$_GET['id']]);
    $amelioration = $stmt->fetch(PDO::FETCH_ASSOC);

    // Si pas trouvé, on récupère les données de correction pour générer l'amélioration
    if (!$amelioration) {
        // Récupérer les données de correction
        $stmt = $pdo->prepare("
            SELECT c.*, d.titre as devoir_titre, d.enonce as devoir_enonce, d.type as devoir_type 
            FROM corrections c 
            JOIN devoirs d ON c.devoir_id = d.id 
            WHERE c.id = ?
        ");
        $stmt->execute([$_GET['id']]);
        $correction = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$correction) {
            throw new Exception('Correction non trouvée');
        }

        // Récupérer les évaluations par compétence
        $stmt = $pdo->prepare("SELECT * FROM evaluations_competences WHERE correction_id = ?");
        $stmt->execute([$_GET['id']]);
        $competences = $stmt->fetchAll(PDO::FETCH_ASSOC);

        // Préparer le prompt pour l'API
        $prompt = "Un élève a reçu une correction de son devoir.
Voici les détails de sa copie :
Type de devoir : {$correction['devoir_type']}
Note : {$correction['note']}/20
Appréciation : '{$correction['appreciation']}'

Points forts : " . implode(", ", json_decode($correction['points_forts'], true)) . "
Points à améliorer : " . implode(", ", json_decode($correction['points_ameliorer'], true)) . "

Évaluations par compétence :
";

        foreach ($competences as $comp) {
            $prompt .= "\n{$comp['nom_competence']}: {$comp['note']}/20
Analyse: {$comp['analyse']}";
        }

        $prompt .= "\n\n
        Tu dois donc donner du contenu concret sur lequel peut s appuyer cet élève de 16 ans. Le contenu doit être adapté à son niveau et à sa situation. Tu dois donc lui donner des objectifs, des exercices, des ressources et des conseils méthodologiques pour qu'il puisse s'améliorer. Il doit comprendre ce qu'il doit faire pour progresser. Tu t'adresses à lui directement.

        Réponds UNIQUEMENT au format JSON suivant:
{
    \"objectifs\": [\"objectif 1\", \"objectif 2\", \"objectif 3\" etc.],
    \"exercices_suggeres\": [\"exercice 1\", \"exercice 2\", \"exercice 3\" etc.],
    \"ressources_recommandees\": [\"ressource 1\", \"ressource 2\", \"ressource 3\" etc.],
    \"methodologie\": [\"conseil méthodologique 1\", \"conseil méthodologique 2\", \"conseil méthodologique 3\" etc.]
}";

        // Appel à l'API OpenAI
        $response = json_decode(file_get_contents('openai.php', false, stream_context_create([
            'http' => [
                'method' => 'POST',
                'header' => 'Content-Type: application/x-www-form-urlencoded',
                'content' => http_build_query([
                    'system' => 'Tu es une IA spécialiste de pédagogie et spécialiste de l apprentissage qui guide les élèves en vue de s améliorer après avoir reçu leur copie de contrôle.',
                    'prompt' => $prompt
                ])
            ]
        ])), true);

        $responseData = json_decode($response['response'], true)['choices'][0]['message']['content'];
        $ameliorationData = json_decode($responseData, true);

        // Sauvegarder dans la base de données
        $stmt = $pdo->prepare("
            INSERT INTO ameliorations (correction_id, objectifs, exercices_suggeres, ressources_recommandees, methodologie)
            VALUES (?, ?, ?, ?, ?)
        ");
        $stmt->execute([
            $_GET['id'],
            json_encode($ameliorationData['objectifs']),
            json_encode($ameliorationData['exercices_suggeres']),
            json_encode($ameliorationData['ressources_recommandees']),
            json_encode($ameliorationData['methodologie'])
        ]);

        $amelioration = [
            'objectifs' => $ameliorationData['objectifs'],
            'exercices_suggeres' => $ameliorationData['exercices_suggeres'],
            'ressources_recommandees' => $ameliorationData['ressources_recommandees'],
            'methodologie' => $ameliorationData['methodologie']
        ];
    } else {
        // Décoder les champs JSON
        $amelioration['objectifs'] = json_decode($amelioration['objectifs'], true);
        $amelioration['exercices_suggeres'] = json_decode($amelioration['exercices_suggeres'], true);
        $amelioration['ressources_recommandees'] = json_decode($amelioration['ressources_recommandees'], true);
        $amelioration['methodologie'] = json_decode($amelioration['methodologie'], true);
    }
?>
<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Plan d'amélioration</title>
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
            font-family: 'Poppins', sans-serif;
        }

        body {
            background-color: #f5f5f5;
            min-height: 100vh;
        }

        .top-bar {
            background-color: #152f20;
            padding: 1rem;
            margin: 0.8rem;
            border-radius: 15px;
            box-shadow: 0 4px 15px rgba(21, 47, 32, 0.15);
            position: relative;
        }

        .nav-container {
            display: flex;
            justify-content: space-between;
            align-items: center;
            max-width: 1200px;
            margin: 0 auto;
            position: relative;
        }

        .logo {
            color: white;
            font-size: 1.3rem;
            font-weight: 600;
            letter-spacing: 0.5px;
            z-index: 2;
        }

        .hamburger {
            display: none;
            flex-direction: column;
            gap: 5px;
            cursor: pointer;
            z-index: 2;
            padding: 10px;
        }

        .hamburger span {
            display: block;
            width: 25px;
            height: 3px;
            background-color: white;
            border-radius: 3px;
            transition: all 0.3s ease;
        }

        .nav-links {
            display: flex;
            align-items: center;
            gap: 2rem;
        }

        .nav-links a {
            color: white;
            text-decoration: none;
            font-weight: 500;
            transition: all 0.3s ease;
            position: relative;
            padding: 0.5rem 0;
        }

        .content-container {
            max-width: 1200px;
            margin: 2rem auto;
            padding: 0 1rem;
        }

        .section {
            background: white;
            padding: 2rem;
            border-radius: 15px;
            margin-bottom: 2rem;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }

        .section-title {
            color: #152f20;
            font-size: 1.5rem;
            margin-bottom: 1.5rem;
            border-bottom: 2px solid #152f20;
            padding-bottom: 0.5rem;
        }

        .list-item {
            margin-bottom: 1rem;
            padding: 1rem;
            background: #f8f9fa;
            border-radius: 8px;
            transition: all 0.3s ease;
        }

        .list-item:hover {
            transform: translateX(10px);
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        }

        @media (max-width: 768px) {
            .hamburger {
                display: flex;
            }

            .nav-links {
                position: absolute;
                flex-direction: column;
                background-color: #152f20;
                top: 100%;
                left: 0;
                right: 0;
                padding: 1rem;
                border-radius: 0 0 15px 15px;
                gap: 1rem;
                transform: translateY(-200%);
                transition: transform 0.3s ease;
                opacity: 0;
                visibility: hidden;
                z-index: 1;
            }

            .nav-links.active {
                transform: translateY(0);
                opacity: 1;
                visibility: visible;
            }
        }
    </style>
</head>
<body>
    <nav class="top-bar">
        <div class="nav-container">
            <div class="logo">Plan d'amélioration</div>
            <div class="hamburger">
                <span></span>
                <span></span>
                <span></span>
            </div>
            <div class="nav-links">
            <a href="javascript:history.back()">Retour</a>
            </div>
        </div>
    </nav>

    <div class="content-container">
        <div class="section">
            <h2 class="section-title">Objectifs</h2>
            <?php foreach ($amelioration['objectifs'] as $objectif): ?>
                <div class="list-item">
                    <?php echo htmlspecialchars($objectif); ?>
                </div>
            <?php endforeach; ?>
        </div>

        <div class="section">
            <h2 class="section-title">Exercices suggérés</h2>
            <?php foreach ($amelioration['exercices_suggeres'] as $exercice): ?>
                <div class="list-item">
                    <?php echo htmlspecialchars($exercice); ?>
                </div>
            <?php endforeach; ?>
        </div>

        <div class="section">
            <h2 class="section-title">Ressources recommandées</h2>
            <?php foreach ($amelioration['ressources_recommandees'] as $ressource): ?>
                <div class="list-item">
                    <?php echo htmlspecialchars($ressource); ?>
                </div>
            <?php endforeach; ?>
        </div>

        <div class="section">
            <h2 class="section-title">Méthodologie</h2>
            <?php foreach ($amelioration['methodologie'] as $methode): ?>
                <div class="list-item">
                    <?php echo htmlspecialchars($methode); ?>
                </div>
            <?php endforeach; ?>
        </div>
    </div>

    <script>
        const hamburger = document.querySelector('.hamburger');
        const navLinks = document.querySelector('.nav-links');
        
        hamburger.addEventListener('click', () => {
            hamburger.classList.toggle('active');
            navLinks.classList.toggle('active');
        });

        document.addEventListener('click', (e) => {
            if (!hamburger.contains(e.target) && !navLinks.contains(e.target)) {
                hamburger.classList.remove('active');
                navLinks.classList.remove('active');
            }
        });

        navLinks.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', () => {
                hamburger.classList.remove('active');
                navLinks.classList.remove('active');
            });
        });
    </script>
</body>
</html>
<?php
} catch (Exception $e) {
    echo 'Erreur : ' . $e->getMessage();
    exit;
}
?>