<?php
require_once 'config.php';

// Vérification de l'ID de la correction
if (!isset($_GET['id'])) {
    echo json_encode(['error' => 'ID de la correction non fourni']);
    exit;
}

try {
    // Récupération des données de la correction
    $stmt = $pdo->prepare("SELECT * FROM corrections WHERE id = ?");
    $stmt->execute([$_GET['id']]);
    $correction = $stmt->fetch(PDO::FETCH_ASSOC);

    if (!$correction) {
        echo json_encode(['error' => 'Correction non trouvée']);
        exit;
    }

    $hasAnnotations = !empty($correction['annotations']);

    if ($hasAnnotations) {
        // Décodage du JSON des annotations existantes
        $annotationsData = json_decode($correction['annotations'], true);
        
        if (!$annotationsData || !isset($annotationsData['texte']) || !isset($annotationsData['annotations'])) {
            $texte = '';
            $annotations = [];
        } else {
            $texte = $annotationsData['texte'];
            $annotations = $annotationsData['annotations'];
        }
    }

} catch (Exception $e) {
    echo json_encode(['error' => $e->getMessage()]);
    exit;
}

// Préparation des données pour JavaScript
$jsData = [
    'hasAnnotations' => $hasAnnotations,
    'correctionId' => $_GET['id'],
    'correctionData' => [
        'copie' => $correction['copie'],
        'note' => $correction['note'],
        'appreciation' => $correction['appreciation'],
        'points_forts' => json_decode($correction['points_forts'], true),
        'points_ameliorer' => json_decode($correction['points_ameliorer'], true)
    ]
];

if ($hasAnnotations) {
    $jsData['texte'] = $texte;
    $jsData['annotations'] = $annotations;
}
?>
<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Annotations de la copie</title>
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
            position: relative;
        }

        .modal-overlay {
            display: none;
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background-color: rgba(0, 0, 0, 0.5);
            z-index: 1000;
            align-items: center;
            justify-content: center;
        }

        .modal {
            background-color: white;
            padding: 2rem;
            border-radius: 15px;
            width: 90%;
            max-width: 500px;
            text-align: center;
        }

        .progress-container {
            margin: 1.5rem 0;
            background-color: #f0f0f0;
            border-radius: 10px;
            overflow: hidden;
        }

        .progress-bar {
            width: 0%;
            height: 10px;
            background: linear-gradient(90deg, #152f20 0%, #2d5a40 100%);
            transition: width 0.5s ease;
            border-radius: 10px;
        }

        .modal-text {
            color: #152f20;
            margin-bottom: 1rem;
            font-size: 1.1rem;
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

        @keyframes pulse {
            0% { box-shadow: 0 0 0 0 rgba(255, 255, 255, 0.4); }
            70% { box-shadow: 0 0 0 10px rgba(255, 255, 255, 0); }
            100% { box-shadow: 0 0 0 0 rgba(255, 255, 255, 0); }
        }

        .container {
            max-width: 1200px;
            margin: 2rem auto;
            padding: 0 1rem;
        }

        .header {
            margin-bottom: 2rem;
        }

        .title {
            color: #152f20;
            font-size: 1.5rem;
            font-weight: 600;
            margin-bottom: 0.5rem;
        }

        .date {
            color: #666;
            font-style: italic;
            font-size: 0.9rem;
        }

        .content-container {
            display: flex;
            gap: 2rem;
            background-color: white;
            border-radius: 15px;
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }

        .text-container {
            flex: 3;
            padding: 2rem;
            overflow-y: auto;
            max-height: calc(100vh - 250px);
        }

        .text-wrapper {
            white-space: pre-wrap;
            line-height: 1.6;
        }

        .annotations-container {
            flex: 1;
            background-color: #f8fafc;
            padding: 1.5rem;
            border-left: 1px solid #e2e8f0;
            overflow-y: auto;
            max-height: calc(100vh - 250px);
        }

        .highlighted {
            background-color: #FFEB3B;
            cursor: pointer;
            transition: background-color 0.3s ease;
        }

        .highlighted:hover {
            background-color: #FFEB3B;
        }

        .annotation-card {
            background-color: white;
            border-radius: 8px;
            padding: 1rem;
            margin-bottom: 1rem;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
            transition: background-color 0.3s ease;
        }

        .annotation-passage {
            font-size: 0.9rem;
            color: #1e293b;
            margin-bottom: 0.5rem;
            background-color: #FFEB3B;
            padding: 0.5rem;
            border-radius: 4px;
        }

        .annotation-comment {
            font-size: 0.85rem;
            color: #64748b;
            line-height: 1.4;
        }

        @media (max-width: 768px) {
            .content-container {
                flex-direction: column;
            }

            .text-container,
            .annotations-container {
                max-height: none;
            }

            .annotations-container {
                border-left: none;
                border-top: 1px solid #e2e8f0;
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

            .hamburger.active span:nth-child(1) {
                transform: rotate(45deg) translate(5px, 5px);
            }

            .hamburger.active span:nth-child(2) {
                opacity: 0;
            }

            .hamburger.active span:nth-child(3) {
                transform: rotate(-45deg) translate(7px, -7px);
            }
        }

        @media (min-width: 769px) {
            .nav-links {
                display: flex !important;
                opacity: 1 !important;
                visibility: visible !important;
                transform: none !important;
                position: relative;
                background: none;
                padding: 0;
            }
        }
    </style>
</head>
<body>
    <div class="modal-overlay" id="loadingModal">
        <div class="modal">
            <h2 class="modal-text">Génération des annotations en cours...</h2>
            <div class="progress-container">
                <div class="progress-bar" id="progressBar"></div>
            </div>
            <p class="modal-text" id="progressText">0%</p>
        </div>
    </div>

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
                <button class="nav-button" onclick="window.location.href='corriger-copie.php';">Corriger une copie ✨</button>
            </div>
        </div>
    </nav>

    <div class="container">
        <div class="header">
            <h1 class="title">Analyse du texte</h1>
            <p class="date"><?php echo date('d/m/Y', strtotime($correction['date_correction'])); ?></p>
        </div>

        <div class="content-container">
            <div class="text-container" id="text-content"></div>
            <div class="annotations-container" id="annotations-content"></div>
        </div>
    </div>

    <script>
        // Configuration initiale
        const initialData = <?php echo json_encode($jsData); ?>;
        let data = null;

        // Fonctions d'utilitaires
        function updateProgressBar(progress) {
            const progressBar = document.getElementById('progressBar');
            const progressText = document.getElementById('progressText');
            if (progressBar && progressText) {
                progressBar.style.width = `${progress}%`;
                progressText.textContent = `${Math.round(progress)}%`;
            }
        }

        function showLoadingModal() {
            document.getElementById('loadingModal').style.display = 'flex';
        }

        function hideLoadingModal() {
            document.getElementById('loadingModal').style.display = 'none';
        }

        function simulateProgress(duration) {
            const steps = 50;
            const interval = duration / steps;
            let progress = 0;
            let step = 0;

            const progressInterval = setInterval(() => {
                step++;
                progress = (1 / (1 + Math.exp(-0.2 * (step - steps/2)))) * 100;
                updateProgressBar(progress);

                if (step >= steps) clearInterval(progressInterval);
            }, interval);
        }

        // Construit un mapping entre le texte original et sa version normalisée
        function createNormalizationMapping(text) {
            let normalized = "";
            let mapping = [];
            for (let i = 0; i < text.length; i++) {
                let ch = text[i];
                // Normalisation de la lettre (décomposition + suppression des diacritiques)
                let normCh = ch.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
                // Remplacer toutes les suites d'espaces par un espace unique
                if (/\s/.test(ch)) {
                    if (normalized.slice(-1) !== ' ') {
                        normalized += ' ';
                        mapping.push(i);
                    }
                } else {
                    normalized += normCh;
                    mapping.push(i);
                }
            }
            return { normalized, mapping };
        }

        function normalizeText(text) {
            if (!text || typeof text !== 'string') return '';
            return text
                .normalize('NFD')
                .replace(/[\u0300-\u036f]/g, '')
                .replace(/\s+/g, ' ')
                .replace(/[\u00A0\u2007\u202F\u205F]/g, ' ')
                .replace(/[''′`]/g, "'")
                .replace(/[""]/g, '"')
                .replace(/[\u2010-\u2015]/g, '-')
                .replace(/\s*-\s*/g, '-')
                .replace(/\f/g, '')
                .trim();
        }

        function findExactMatch(text, passage) {
            if (!text || !passage) return null;
            const mappingData = createNormalizationMapping(text);
            // Utilisation de la version normalisée en minuscules
            const normText = mappingData.normalized.toLowerCase();
            const mapping = mappingData.mapping;
            const normPassage = normalizeText(passage).toLowerCase();
            const indexFound = normText.indexOf(normPassage);
            if (indexFound === -1) return null;
            const originalStart = mapping[indexFound];
            return {
                start: originalStart,
                length: passage.length
            };
        }

        function highlightText(text, annotations) {
            if (!text || !annotations) return text || '';
            let positions = [];
            
            // Itérer sur les annotations dans l'ordre original
            annotations.forEach((annotation, originalIndex) => {
                if (!annotation.passage) return;
                const match = findExactMatch(text, annotation.passage);
                if (match) {
                    positions.push({
                        start: match.start,
                        end: match.start + annotation.passage.length,
                        originalIndex: originalIndex
                    });
                }
            });
            
            // Trier les positions par position décroissante pour éviter les chevauchements
            positions.sort((a, b) => b.start - a.start);
            
            // Appliquer le surlignage en utilisant l'index original pour le data-annotation-id
            let result = text;
            positions.forEach(pos => {
                const before = result.substring(0, pos.start);
                const highlighted = result.substring(pos.start, pos.end);
                const after = result.substring(pos.end);
                result = `${before}<span class="highlighted" data-annotation-id="${pos.originalIndex}">${highlighted}</span>${after}`;
            });
            return result;
        }

        async function generateAnnotations() {
            showLoadingModal();
            simulateProgress(20000);

            try {
                const response = await fetch('api-annotations.php', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    },
                    body: JSON.stringify({
                        id: initialData.correctionId,
                        copie: initialData.correctionData.copie,
                        note: initialData.correctionData.note,
                        appreciation: initialData.correctionData.appreciation,
                        points_forts: initialData.correctionData.points_forts,
                        points_ameliorer: initialData.correctionData.points_ameliorer
                    })
                });

                if (!response.ok) {
                    throw new Error(`Erreur HTTP: ${response.status}`);
                }

                const result = await response.json();
                if (result.error) {
                    throw new Error(result.error);
                }

                data = {
                    texte: result.texte,
                    annotations: result.annotations
                };

                hideLoadingModal();
                updateDisplay();

            } catch (error) {
                console.error('Erreur:', error);
                hideLoadingModal();
                alert('Une erreur est survenue lors de la génération des annotations. Veuillez réessayer.');
            }
        }

        function updateDisplay() {
            if (!data || !data.texte || !data.annotations) {
                console.error('Données invalides:', data);
                return;
            }

            const textContainer = document.getElementById('text-content');
            const annotationsContainer = document.getElementById('annotations-content');

            if (!textContainer || !annotationsContainer) {
                console.error('Conteneurs non trouvés');
                return;
            }

            textContainer.innerHTML = `<div class="text-wrapper">${highlightText(data.texte, data.annotations)}</div>`;
            annotationsContainer.innerHTML = data.annotations
                .map((annotation, index) => createAnnotationCard(annotation, index))
                .join('');

            setTimeout(setupInteractions, 0);
        }

        function createAnnotationCard(annotation, index) {
            return `
                <div class="annotation-card" data-annotation-id="${index}">
                    <div class="annotation-passage">${annotation.passage}</div>
                    <div class="annotation-comment">${annotation.commentaire}</div>
                </div>
            `;
        }

        function setupInteractions() {
            const highlights = document.querySelectorAll('.highlighted');
            const cards = document.querySelectorAll('.annotation-card');

            highlights.forEach(highlight => {
                highlight.addEventListener('mouseenter', function() {
                    const annotationId = this.getAttribute('data-annotation-id');
                    const card = document.querySelector(`.annotation-card[data-annotation-id="${annotationId}"]`);
                    if (card) {
                        card.style.backgroundColor = '#f1f5f9';
                        card.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                    }
                });

                highlight.addEventListener('mouseleave', function() {
                    const annotationId = this.getAttribute('data-annotation-id');
                    const card = document.querySelector(`.annotation-card[data-annotation-id="${annotationId}"]`);
                    if (card) {
                        card.style.backgroundColor = 'white';
                    }
                });
            });

            cards.forEach(card => {
                card.addEventListener('mouseenter', function() {
                    const annotationId = this.getAttribute('data-annotation-id');
                    const highlight = document.querySelector(`.highlighted[data-annotation-id="${annotationId}"]`);
                    if (highlight) {
                        highlight.style.backgroundColor = '#FFD53BC8';
                        highlight.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                    }
                });

                card.addEventListener('mouseleave', function() {
                    const annotationId = this.getAttribute('data-annotation-id');
                    const highlight = document.querySelector(`.highlighted[data-annotation-id="${annotationId}"]`);
                    if (highlight) {
                        highlight.style.backgroundColor = '#FFEB3B';
                    }
                });
            });
        }

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

        // Initialisation
        window.onload = function() {
            if (initialData.hasAnnotations) {
                data = {
                    texte: initialData.texte,
                    annotations: initialData.annotations
                };
                updateDisplay();
            } else {
                generateAnnotations();
            }
        };
    </script>
</body>
</html>