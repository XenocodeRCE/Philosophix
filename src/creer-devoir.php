<?php
require_once 'config.php';

if ($_SERVER['REQUEST_METHOD'] == 'POST') {
    $titre = $_POST['titre'];
    $enonce = $_POST['enonce'];
    $type = $_POST['type'];
    $contenu = "n/a";

    $bareme_dissertation = '{ "competences": [ { "id": 1, "nom": "Compréhension et analyse du sujet", "criteres": [ "Identifier les termes clés d\'un sujet", "Reformuler le sujet avec ses propres mots", "Formuler une problématique pertinente" ] }, { "id": 2, "nom": "Élaboration d\'un plan structuré", "criteres": [ "Organiser ses idées de manière logique", "Connaître les différents types de plans", "Annoncer clairement son plan dans l\'introduction" ] }, { "id": 3, "nom": "Rédaction de l\'introduction et de la conclusion", "criteres": [ "Rédiger une accroche efficace", "Maîtriser les étapes clés de l\'introduction", "Synthétiser et ouvrir la réflexion" ] }, { "id": 4, "nom": "Développement de l\'argumentation", "criteres": [ "Construire des paragraphes argumentatifs", "Utiliser des exemples pertinents", "Intégrer des références" ] }, { "id": 5, "nom": "Maîtrise de la langue française", "criteres": [ "Orthographe et grammaire", "Vocabulaire spécifique", "Fluidité de la syntaxe" ] }, { "id": 6, "nom": "Cohérence et cohésion textuelle", "criteres": [ "Utiliser des connecteurs logiques", "Assurer la cohérence entre les parties", "Contribuer à la problématique" ] }, { "id": 7, "nom": "Esprit critique et réflexion personnelle", "criteres": [ "Prise de position argumentée", "Évaluer les arguments", "Réflexion personnelle" ] } ] }';

    $bareme_explication = '{ "competences": [ { "id": 1, "nom": "Lecture analytique et compréhension globale", "criteres": [ "Identification de la thèse principale", "Repérage de la structure générale", "Compréhension des enjeux du texte" ] }, { "id": 2, "nom": "Analyse de la structure argumentative", "criteres": [ "Découpage en séquences logiques", "Repérage des articulations", "Identification des mouvements du texte" ] }, { "id": 3, "nom": "Analyse conceptuelle", "criteres": [ "Définition des concepts clés", "Compréhension des distinctions conceptuelles", "Mise en relation des notions" ] }, { "id": 4, "nom": "Analyse argumentative", "criteres": [ "Reconstruction des raisonnements", "Identification des types d\'arguments", "Repérage des exemples et illustrations" ] }, { "id": 5, "nom": "Contextualisation philosophique", "criteres": [ "Situation historique", "Liens avec d\'autres auteurs", "Mobilisation des connaissances du cours" ] }, { "id": 6, "nom": "Expression et rédaction", "criteres": [ "Clarté de l\'explication", "Précision du vocabulaire", "Structure de l\'explication" ] }, { "id": 7, "nom": "Appropriation critique", "criteres": [ "Évaluation de la cohérence", "Discussion des arguments", "Prolongements pertinents" ] } ] }';

    $bareme = $type == 'explication' ? $bareme_explication : $bareme_dissertation;

    try {
        $stmt = $pdo->prepare("INSERT INTO devoirs (titre, enonce, contenu, date_creation, bareme, type) VALUES (?, ?, ?, NOW(), ?, ?)");
        $stmt->execute([$titre, $enonce, $contenu, $bareme, $type]);
        
        header("Location: voir-devoirs.php");
        exit();
    } catch (PDOException $e) {
        $error = "Une erreur est survenue lors de la création du devoir.";
    }
}
?>

<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Créer un devoir</title>
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

        .nav-links a::after {
            content: '';
            position: absolute;
            width: 0;
            height: 2px;
            bottom: 0;
            left: 0;
            background-color: white;
            transition: width 0.3s ease;
        }

        .nav-links a:hover::after {
            width: 100%;
        }

        .nav-button {
            background-color: white;
            color: #152f20;
            border: none;
            padding: 0.7rem 1.5rem;
            border-radius: 12px;
            cursor: pointer;
            font-weight: 600;
            font-size: 0.95rem;
            transition: all 0.3s ease;
            animation: pulse 2s infinite;
        }

        .nav-button:hover {
            transform: translateY(-2px);
            box-shadow: 0 0 20px rgba(255, 255, 255, 0.4),
                       0 0 30px rgba(255, 255, 255, 0.2);
        }

        .content {
            padding: 2rem;
            max-width: 1200px;
            margin: 0 auto;
        }

        .form-container {
            background-color: white;
            padding: 2rem;
            border-radius: 15px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }

        .page-title {
            font-size: 1.8rem;
            font-weight: 700;
            color: #152f20;
            margin-bottom: 2rem;
        }

        .form-group {
            margin-bottom: 1.5rem;
        }

        .form-label {
            display: block;
            font-weight: 600;
            margin-bottom: 0.5rem;
            color: #333;
        }

        .form-input {
            width: 100%;
            padding: 0.8rem;
            border: 2px solid #e8f5e9;
            border-radius: 8px;
            font-size: 1rem;
            transition: all 0.3s ease;
        }

        .form-textarea {
            width: 100%;
            min-height: 200px;
            padding: 1rem;
            border: 2px solid #e8f5e9;
            border-radius: 12px;
            font-size: 1rem;
            resize: vertical;
            transition: all 0.3s ease;
        }

        .form-input:focus,
        .form-textarea:focus {
            border-color: #152f20;
            box-shadow: 0 0 15px rgba(21, 47, 32, 0.1);
            outline: none;
        }

        .submit-button {
            background-color: #152f20;
            color: white;
            border: none;
            padding: 1rem 2rem;
            border-radius: 12px;
            cursor: pointer;
            font-weight: 600;
            font-size: 1rem;
            transition: all 0.3s ease;
            width: 100%;
        }

        .submit-button:hover {
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(21, 47, 32, 0.3);
        }

        .error-message {
            background-color: #fee2e2;
            border: 1px solid #ef4444;
            color: #dc2626;
            padding: 1rem;
            border-radius: 8px;
            margin-bottom: 1.5rem;
        }

        @keyframes pulse {
            0% { box-shadow: 0 0 0 0 rgba(255, 255, 255, 0.4); }
            70% { box-shadow: 0 0 0 10px rgba(255, 255, 255, 0); }
            100% { box-shadow: 0 0 0 0 rgba(255, 255, 255, 0); }
        }
        /* Media Queries */
        @media (max-width: 768px) {
            .top-bar {
                margin: 0.5rem;
                padding: 0.8rem;
            }

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

            .nav-button {
                width: 100%;
                text-align: center;
                margin-top: 0.5rem;
            }

            .hamburger.active span:nth-child(1) {
                transform: rotate(45deg) translate(5px, 5px);
            }

            .hamburger.active span:nth-child(2) {
                opacity: 0;
            }

            .hamburger.active span:nth-child(3) {
                transform: rotate(-45deg) translate(7px, -7px);
            }

            .content {
                padding: 1rem;
            }

            .form-container {
                padding: 1rem;
            }
        }

        @media (min-width: 769px) {
            .top-bar {
                padding: 1.2rem 2.5rem;
                margin: 1.2rem;
            }

            .nav-links {
                display: flex !important;
                opacity: 1 !important;
                visibility: visible !important;
                transform: none !important;
                position: relative;
                background: none;
                padding: 0;
            }

            .logo {
                font-size: 1.5rem;
            }

            .nav-button {
                padding: 0.8rem 2rem;
            }
        }
    </style>
</head>
<body>
    <nav class="top-bar">
        <div class="nav-container">
            <div class="logo">Philosophix</div>
            <div class="hamburger">
                <span></span>
                <span></span>
                <span></span>
            </div>
            <div class="nav-links">
                <a href="creer-devoir.php">Créer</a>
                 
                <a href="voir-devoirs.php">Consulter</a>
                <button class="nav-button" onclick="window.location.href = 'corriger-copie.php';">Corriger une copie ✨</button>
            </div>
        </div>
    </nav>

    <div class="content">
        <div class="form-container">
            <h1 class="page-title">Créer un nouveau devoir</h1>
            
            <?php if (isset($error)): ?>
                <div class="error-message">
                    <?php echo htmlspecialchars($error); ?>
                </div>
            <?php endif; ?>

            <form method="POST" action="">
                <div class="form-group">
                    <label class="form-label" for="titre">Titre du devoir</label>
                    <input 
                        type="text" 
                        id="titre" 
                        name="titre" 
                        class="form-input" 
                        required 
                        maxlength="255"
                        value="<?php echo isset($_POST['titre']) ? htmlspecialchars($_POST['titre']) : ''; ?>"
                    >
                </div>

                <div class="form-group">
                    <label class="form-label" for="enonce">Énoncé du devoir</label>
                    <textarea 
                        id="enonce" 
                        name="enonce" 
                        class="form-textarea" 
                        required
                    ><?php echo isset($_POST['enonce']) ? htmlspecialchars($_POST['enonce']) : ''; ?></textarea>
                </div>

                <div class="form-group">
                    <label class="form-label" for="type">Type de devoir</label>
                    <select id="type" name="type" class="form-input" required>
                        <option value="dissertation" <?php echo (isset($_POST['type']) && $_POST['type'] == 'dissertation') ? 'selected' : ''; ?>>Dissertation</option>
                        <option value="explication" <?php echo (isset($_POST['type']) && $_POST['type'] == 'explication') ? 'selected' : ''; ?>>Explication de texte</option>
                    </select>
                </div>

                <button type="submit" class="submit-button">Créer le devoir</button>
            </form>
        </div>
    </div>

    <script>
        // Navigation mobile
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