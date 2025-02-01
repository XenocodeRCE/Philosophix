<?php
require_once 'config.php';

if (!isset($_GET['id'])) {
    header('Location: index.php');
    exit;
}

try {
    // Récupération de la correction
    $stmt = $pdo->prepare("
        SELECT c.*, d.titre as devoir_titre, d.enonce as devoir_enonce
        FROM corrections c 
        JOIN devoirs d ON c.devoir_id = d.id 
        WHERE c.id = ?
    ");
    $stmt->execute([$_GET['id']]);
    $correction = $stmt->fetch(PDO::FETCH_ASSOC);

    if (!$correction) {
        throw new Exception('Correction non trouvée');
    }

    // Récupération des évaluations par compétence
    $stmt = $pdo->prepare("
        SELECT * FROM evaluations_competences 
        WHERE correction_id = ?
        ORDER BY created_at ASC
    ");
    $stmt->execute([$_GET['id']]);
    $competences = $stmt->fetchAll(PDO::FETCH_ASSOC);

    // Conversion des champs JSON
    $correction['points_forts'] = json_decode($correction['points_forts'], true);
    $correction['points_ameliorer'] = json_decode($correction['points_ameliorer'], true);
    
    foreach ($competences as &$comp) {
        $comp['points_forts'] = json_decode($comp['points_forts'], true);
        $comp['points_ameliorer'] = json_decode($comp['points_ameliorer'], true);
    }

} catch (Exception $e) {
    echo 'Erreur : ' . $e->getMessage();
    exit;
}
?>
<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Détails de la correction</title>
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/tailwindcss@2.2.19/dist/tailwind.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css">
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

@media (min-width: 769px) {
    .top-bar {
        padding: 1.2rem 2.5rem;
        margin: 1.2rem;
    }
}

@media (max-width: 768px) {
    .top-bar {
        margin: 0.5rem;
        padding: 0.8rem;
    }
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
            max-width: 1200px;
            margin: 2rem auto;
            padding: 0 1rem;
        }

        .appreciation {
            font-style: italic;
            color: #666;
            font-size: 1.1rem;
            margin-bottom: 2rem;
            padding: 1rem;
            background-color: #fff;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }

        .main-section {
            display: flex;
            gap: 2rem;
            margin-bottom: 2rem;
        }

        .feedback-box {
            padding: 1.5rem;
            border-radius: 12px;
            margin-bottom: 1rem;
            transition: all 0.3s ease;
        }

        .feedback-box:hover {
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
        }

        .feedback-box.positive {
            background-color: #e8f5e9;
            border: 1px solid #4caf50;
        }

        .feedback-box.negative {
            background-color: #ffebee;
            border: 1px solid #ef5350;
        }

        .grade-box {
            background: #152f20; /* Changer la couleur de fond */
            padding: 2rem;
            border-radius: 12px;
            text-align: center;
            position: relative;
            margin-bottom: 1rem;
            color: white;
            animation: lightShadow 4s linear infinite; /* Ajouter l'animation */
        }

        @keyframes lightShadow {
            0% { box-shadow: 0 0 20px rgba(21, 47, 32, 0.4); }
            50% { box-shadow: 0 0 30px rgba(21, 47, 32, 0.6); }
            100% { box-shadow: 0 0 20px rgba(21, 47, 32, 0.4); }
        }

        .grade {
            font-size: 3rem;
            font-weight: 700;
        }

        .skill-item {
            background-color: white;
            border-radius: 12px;
            margin-bottom: 1rem;
            overflow: hidden;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            transition: all 0.3s ease;
        }

        .skill-header {
            padding: 1rem;
            background-color: #f8f9fa;
            cursor: pointer;
            display: flex;
            justify-content: space-between;
            align-items: center;
            transition: all 0.3s ease;
        }

        .skill-header:hover {
            background-color: #e8f5e9;
        }

        .progress-container {
            height: 8px;
            background-color: #e0e0e0;
            border-radius: 4px;
            overflow: hidden;
            flex: 1;
            margin: 0 1rem;
        }

        .skill-content {
            max-height: 0;
            overflow: hidden;
            transition: max-height 0.3s ease;
        }

        .skill-content.active {
            max-height: 1500px;
        }

        /* Modal styles */
        .modal {
            display: none;
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background-color: rgba(0,0,0,0.5);
            z-index: 1000;
            align-items: center;
            justify-content: center;
        }

        .modal.active {
            display: flex;
        }

        .modal-content {
            background-color: white;
            padding: 2rem;
            border-radius: 15px;
            max-width: 800px;
            width: 90%;
            max-height: 90vh;
            overflow-y: auto;
            position: relative;
        }

        @media (max-width: 768px) {
            .main-section {
                flex-direction: column;
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
        }

        @keyframes gradeBorder {
            0% { box-shadow: 0 0 20px rgba(79, 70, 229, 0.4); }
            50% { box-shadow: 0 0 30px rgba(124, 58, 237, 0.4); }
            100% { box-shadow: 0 0 20px rgba(79, 70, 229, 0.4); }
        }

        @keyframes pulse {
            0% { box-shadow: 0 0 0 0 rgba(255, 255, 255, 0.4); }
            70% { box-shadow: 0 0 0 10px rgba(255, 255, 255, 0); }
            100% { box-shadow: 0 0 0 0 rgba(255, 255, 255, 0); }
        }

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
}

        @media (max-width: 768px) {
            .grid-cols-2 {
                grid-template-columns: repeat(1, minmax(0, 1fr));
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
        <h1 class="text-3xl font-bold text-gray-800 mb-6">Détails de la correction</h1>
        
        <!-- Section principale -->
        <div class="main-section">
            <div class="left-column flex-grow">
                <!-- Appréciation générale -->
                <div class="appreciation mb-8">
                    <h2 class="text-xl font-semibold mb-3">Appréciation générale</h2>
                    <p style="text-align: justify;" class="text-gray-700"><?php echo nl2br(htmlspecialchars($correction['appreciation'])); ?></p>
                </div>

                <!-- Points forts et à améliorer -->
                <div class="feedback-box positive">
                    <h2 class="feedback-title text-green-800 font-bold mb-3">Points forts</h2>
                    <ul class="space-y-2">
                        <?php foreach ($correction['points_forts'] as $point): ?>
                            <li class="flex items-start">
                                <i class="fas fa-check-circle text-green-600 mt-1 mr-2"></i>
                                <span><?php echo htmlspecialchars($point); ?></span>
                            </li>
                        <?php endforeach; ?>
                    </ul>
                </div>

                <div class="feedback-box negative mt-4">
                    <h2 class="feedback-title text-red-800 font-bold mb-3">Points à améliorer</h2>
                    <ul class="space-y-2">
                        <?php foreach ($correction['points_ameliorer'] as $point): ?>
                            <li class="flex items-start">
                                <i class="fas fa-exclamation-circle text-red-600 mt-1 mr-2"></i>
                                <span><?php echo htmlspecialchars($point); ?></span>
                            </li>
                        <?php endforeach; ?>
                    </ul>
                </div>
            </div>

            <div class="right-column">
                <!-- Note -->
                <div class="grade-box">
                    <h3 class="text-lg mb-2 opacity-90">Note finale</h3>
                    <div class="grade"><?php echo htmlspecialchars($correction['note']); ?>/20</div>
                </div>

                <!-- Aperçu de la copie -->
                <div id="previewBox" class="bg-white rounded-lg p-4 cursor-pointer shadow-md hover:shadow-lg transition-all duration-300">
                    <h3 class="text-lg font-semibold mb-3">Copie de l'apprenant</h3>
                    <div class="text-gray-600 text-sm mb-3">
                        <?php echo substr(htmlspecialchars($correction['copie']), 0, 150) . '...'; ?>
                    </div>
                    <div class="flex justify-center">
                        <button class="text-blue-600 hover:text-blue-800 transition-colors">
                            <i class="fas fa-search mr-2"></i>Voir la copie complète
                        </button>
                    </div>
                </div>

                <!-- Boîte de partage -->
                <div class="bg-white rounded-lg p-4 mt-4 shadow-md hover:shadow-lg transition-all duration-300">
                    <h3 class="text-lg font-semibold mb-3">Partager</h3>
                    <div class="flex justify-center space-x-4">
                        <button id="copyUrlButton" class="text-blue-600 hover:text-blue-800 transition-colors">
                            <i class="fas fa-copy mr-2"></i>Copier l'URL
                        </button>
                        <button id="qrCodeButton" class="text-blue-600 hover:text-blue-800 transition-colors">
                            <i class="fas fa-qrcode mr-2"></i>Voir le QR Code
                        </button>
                    </div>
                    <div id="qrCodeContainer" class="flex justify-center mt-4 hidden">
                        <img id="qrCodeImage" src="" alt="QR Code">
                    </div>
                </div>

                <!-- Boîte d'amélioration -->
                <div class="bg-white rounded-lg p-4 mt-4 shadow-md hover:shadow-lg transition-all duration-300">
                    <h3 class="text-lg font-semibold mb-3">Amélioration</h3>
                    <div class="flex justify-center">
                        <button onclick="window.location.href='ameliorations.php?id=<?php echo $_GET['id']; ?>'" 
                                class="text-green-600 hover:text-green-800 transition-colors">
                            <i class="fas fa-arrow-up mr-2"></i>Comment m'améliorer ?
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Évaluations par compétence -->
        <div class="mt-8">
            <h2 class="text-2xl font-bold mb-6">Évaluations par compétence</h2>
            <div class="space-y-4">
                <?php foreach ($competences as $competence): ?>
                    <?php
                    $note = $competence['note'];
                    $progressClass = $note >= 16 ? 'bg-green-500' : 
                                   ($note >= 12 ? 'bg-blue-500' : 
                                   ($note >= 8 ? 'bg-yellow-500' : 'bg-red-500'));
                    ?>
                    <div class="skill-item">
                        <div class="skill-header">
                            <div class="flex items-center flex-grow">
                                <span class="font-semibold"><?php echo htmlspecialchars($competence['nom_competence']); ?></span>
                                <div class="progress-container mx-4">
                                    <div class="<?php echo $progressClass; ?> h-full transition-all duration-500" 
                                         style="width: <?php echo $note * 5; ?>%"></div>
                                </div>
                                <span class="font-bold <?php echo $note >= 10 ? 'text-green-600' : 'text-red-600'; ?>">
                                    <?php echo $note; ?>/20
                                </span>
                            </div>
                            <i class="fas fa-chevron-down ml-2 transition-transform duration-300"></i>
                        </div>
                        <div class="skill-content">
                            <div class="p-4" style="text-align: justify;">
                                <p class="text-gray-700 mb-4"><?php echo nl2br(htmlspecialchars($competence['analyse'])); ?></p>
                                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div class="bg-green-50 p-4 rounded-lg">
                                        <h4 class="font-semibold text-green-800 mb-2">Points forts</h4>
                                        <ul class="space-y-2">
                                            <?php foreach ($competence['points_forts'] as $point): ?>
                                                <li class="flex items-start">
                                                    <i class="fas fa-check-circle text-green-600 mt-1 mr-2"></i>
                                                    <span><?php echo htmlspecialchars($point); ?></span>
                                                </li>
                                            <?php endforeach; ?>
                                        </ul>
                                    </div>
                                    <div class="bg-red-50 p-4 rounded-lg">
                                        <h4 class="font-semibold text-red-800 mb-2">Points à améliorer</h4>
                                        <ul class="space-y-2">
                                            <?php foreach ($competence['points_ameliorer'] as $point): ?>
                                                <li class="flex items-start">
                                                    <i class="fas fa-exclamation-circle text-red-600 mt-1 mr-2"></i>
                                                    <span><?php echo htmlspecialchars($point); ?></span>
                                                </li>
                                            <?php endforeach; ?>
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                <?php endforeach; ?>
            </div>
        </div>
    </div>

    <!-- Modal pour la copie complète -->
    <div class="modal" id="copyModal">
        <div class="modal-content">
            <button class="absolute top-4 right-4 text-gray-600 hover:text-gray-800 text-2xl" id="closeModal">
                <i class="fas fa-times"></i>
            </button>
            <h2 class="text-2xl font-bold mb-4">Copie complète</h2>
            <div class="prose max-w-none" style="text-align: justify;">
                <?php echo nl2br(htmlspecialchars($correction['copie'])); ?>
            </div>
        </div>
    </div>
    <script>
        // Gestion du menu hamburger
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

        // Gestion des compétences dépliables
        document.querySelectorAll('.skill-header').forEach(header => {
            header.addEventListener('click', () => {
                const content = header.nextElementSibling;
                const icon = header.querySelector('i');
                
                // Ferme tous les autres contenus
                document.querySelectorAll('.skill-content').forEach(item => {
                    if (item !== content && item.classList.contains('active')) {
                        item.classList.remove('active');
                        item.previousElementSibling.querySelector('i')
                            .style.transform = 'rotate(0deg)';
                    }
                });

                // Toggle le contenu actuel
                content.classList.toggle('active');
                icon.style.transform = content.classList.contains('active') 
                    ? 'rotate(180deg)' 
                    : 'rotate(0deg)';
            });
        });

        // Gestion de la modal
        const modal = document.getElementById('copyModal');
        const previewBox = document.getElementById('previewBox');
        const closeModal = document.getElementById('closeModal');

        previewBox.addEventListener('click', () => {
            modal.classList.add('active');
            document.body.style.overflow = 'hidden'; // Empêche le défilement du body
        });

        const closeModalFunction = () => {
            modal.classList.remove('active');
            document.body.style.overflow = ''; // Réactive le défilement du body
        };

        closeModal.addEventListener('click', closeModalFunction);

        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                closeModalFunction();
            }
        });

        // Fermeture de la modal avec la touche Echap
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && modal.classList.contains('active')) {
                closeModalFunction();
            }
        });

        // Animation des barres de progression au chargement
        document.addEventListener('DOMContentLoaded', () => {
            const progressBars = document.querySelectorAll('.progress-container > div');
            progressBars.forEach(bar => {
                const width = bar.style.width;
                bar.style.width = '0';
                setTimeout(() => {
                    bar.style.width = width;
                }, 100);
            });
        });

        // Gestion du défilement fluide
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                e.preventDefault();
                const target = document.querySelector(this.getAttribute('href'));
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });

        // Gestion du bouton de copie de l'URL
        const copyUrlButton = document.getElementById('copyUrlButton');
        copyUrlButton.addEventListener('click', () => {
            const url = window.location.href;
            navigator.clipboard.writeText(url).then(() => {
                alert('URL copiée dans le presse-papiers');
            });
        });

        // Gestion du bouton de QR Code
        const qrCodeButton = document.getElementById('qrCodeButton');
        const qrCodeContainer = document.getElementById('qrCodeContainer');
        const qrCodeImage = document.getElementById('qrCodeImage');
        qrCodeButton.addEventListener('click', () => {
            const url = window.location.href;
            qrCodeImage.src = `https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${encodeURIComponent(url)}`;
            qrCodeContainer.classList.toggle('hidden');
        });
    </script>
</body>
</html>