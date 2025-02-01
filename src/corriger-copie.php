<?php require_once 'config.php'; ?>
<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Corriger une copie</title>
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

        /* Styles existants */
        .progress-bar { transition: width 0.5s ease-in-out; }
        .modal-overlay { backdrop-filter: blur(4px); }
        .progress-excellent { background-color: #22c55e; }
        .progress-good { background-color: #3b82f6; }
        .progress-average { background-color: #f59e0b; }
        .progress-poor { background-color: #ef4444; }
        
        .accordion-button { transition: all 0.3s ease; }
        .accordion-content { max-height: 0; overflow: hidden; transition: max-height 0.3s ease; }
        .accordion-content.open { max-height: 1000px; }

        .accordion-button {
            transition: all 0.3s ease;
            position: relative;
            padding-right: 3rem !important;
        }
        
        .accordion-button:after {
            content: '\f078';
            font-family: 'Font Awesome 6 Free';
            font-weight: 900;
            position: absolute;
            right: 1rem;
            transition: transform 0.3s ease;
            color: #6B7280;
        }
        
        .accordion-button.active:after {
            transform: rotate(180deg);
        }
        
        .accordion-content {
            max-height: 0;
            overflow: hidden;
            transition: all 0.3s ease-in-out;
            background-color: #f9fafb;
        }
        
        .accordion-content.open {
            max-height: 2000px;
            padding: 1rem;
        }

        @keyframes slideDown {
            from { transform: translateY(-20px); opacity: 0; }
            to { transform: translateY(0); opacity: 1; }
        }

        .result-animation {
            animation: slideDown 0.5s ease-out;
        }

        .note-container {
            background: linear-gradient(135deg, #4F46E5 0%, #7C3AED 100%);
            color: white;
            padding: 2rem;
            border-radius: 1rem;
            box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
        }

        /* Media Queries pour la navigation responsive */
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
    <!-- Navigation -->
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

    <!-- Modal de progression -->
    <div id="progressModal" class="fixed inset-0 bg-black bg-opacity-50 modal-overlay hidden flex items-center justify-center z-50">
        <!-- Le contenu du modal reste le même -->
        <div class="bg-white p-6 rounded-lg shadow-xl w-full max-w-2xl mx-4">
            <h2 class="text-xl font-bold mb-4">Correction en cours...</h2>
            
            <div class="mb-6">
                <div class="w-full bg-gray-200 rounded-full h-2">
                    <div id="globalProgress" class="bg-blue-600 h-2 rounded-full transition-all duration-500" style="width: 0%"></div>
                </div>
                <p id="globalProgressText" class="text-sm text-gray-600 mt-2 text-center">Initialisation de la correction...</p>
            </div>

            <div id="competencesProgress" class="space-y-4">
                <!-- Les compétences seront ajoutées ici dynamiquement -->
            </div>
        </div>
    </div>

    <div class="container mx-auto px-4 py-8">
        <!-- Le reste du contenu reste identique -->
        <div class="bg-white rounded-lg shadow-lg p-6">
            <h1 class="text-2xl font-bold mb-6">Corriger une copie</h1>

            <!-- Étape 1: Sélection du devoir -->
            <div id="step1" class="mb-8">
                <h2 class="text-xl font-bold mb-4">Étape 1: Sélectionner le devoir</h2>
                <select id="devoir_id" name="devoir_id" class="w-full p-2 border rounded">
                    <option value="">Sélectionnez un devoir...</option>
                    <?php
                    $stmt = $pdo->query("SELECT id, titre FROM devoirs ORDER BY date_creation DESC");
                    while ($row = $stmt->fetch()) {
                        echo "<option value='" . $row['id'] . "'>" . htmlspecialchars($row['titre']) . "</option>";
                    }
                    ?>
                </select>
            </div>

            <!-- Étape 2: Copie de l'élève -->
            <div id="step2" class="mb-8 hidden">
                <h2 class="text-xl font-bold mb-4">Étape 2: Copie de l'élève</h2>
                <div class="grid grid-cols-1 gap-4">
                <div>
                        <label class="block text-gray-700 text-sm font-bold mb-2">
                            Coller le texte
                        </label>
                        <textarea minlength="500" required id="copie_text" rows="5"
                            class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700"></textarea>
                    </div>

                    <input style="visibility: hidden" type="file" id="copie_file" accept=".txt,.doc,.docx,.pdf" class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700">
                    
                    
                </div>
            </div>

            <!-- Étape 3: Mot de passe -->
            <div id="step3" class="mb-8 hidden">
                <h2 class="text-xl font-bold mb-4">Étape 3: Mot de passe</h2>
                <div>
                    <label class="block text-gray-700 text-sm font-bold mb-2">
                        Mot de passe
                    </label>
                    <input type="password" id="password" class="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700" required>
                </div>
            </div>

            <!-- Bouton de correction -->
            <div class="flex justify-end">
                <button id="corriger" 
                    class="bg-green-500 hover:bg-green-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline hidden">
                    Lancer la correction
                </button>
            </div>

            <!-- Section des résultats -->
            <div id="resultats" class="mt-8 bg-white rounded-lg shadow-lg p-6 hidden">
                <!-- Le contenu des résultats reste le même -->
                <h2 class="text-2xl font-bold mb-6">Résultats de la correction</h2>
                
                <div class="note-container mb-8">
                    <h3 class="text-xl mb-2">Note finale</h3>
                    <div class="text-4xl font-bold"><span id="note">0</span>/20</div>
                </div>

                <div class="mb-8">
                    <h3 class="text-xl font-bold mb-4">Appréciation générale</h3>
                    <p id="appreciation" class="text-gray-700 leading-relaxed"></p>
                </div>

                <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
                    <div class="bg-green-50 p-4 rounded-lg">
                        <h4 class="font-bold text-green-700 mb-2">Points forts</h4>
                        <ul id="points_forts" class="list-disc pl-5 text-green-600 space-y-1"></ul>
                    </div>
                    <div class="bg-red-50 p-4 rounded-lg">
                        <h4 class="font-bold text-red-700 mb-2">Points à améliorer</h4>
                        <ul id="points_ameliorer" class="list-disc pl-5 text-red-600 space-y-1"></ul>
                    </div>
                </div>

                <div class="mb-8">
                    <h3 class="text-xl font-bold mb-4">Évaluation par compétence</h3>
                    <div id="competences" class="space-y-4"></div>
                </div>
            </div>

            <!-- Bouton de sauvegarde -->
            <div class="mt-8 flex justify-end">
                <button id="sauvegarder" 
                    class="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline hidden">
                    <i class="fas fa-save mr-2"></i>Sauvegarder la correction
                </button>
            </div>
        </div>
    </div>

    <script src="https://cdnjs.cloudflare.com/ajax/libs/crypto-js/4.0.0/crypto-js.min.js"></script>
    <script src="correction.js"></script>
    <script>
        // Script pour la navigation mobile
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

        // Script pour la gestion du formulaire
        document.addEventListener('DOMContentLoaded', () => {
            const devoirSelect = document.getElementById('devoir_id');
            const step2 = document.getElementById('step2');
            const step3 = document.getElementById('step3');
            const corrigerBtn = document.getElementById('corriger');

            devoirSelect.addEventListener('change', function() {
                console.log('Devoir sélectionné:', this.value);
                if (this.value) {
                    step2.classList.remove('hidden');
                    step3.classList.remove('hidden');
                    corrigerBtn.classList.remove('hidden');
                } else {
                    step2.classList.add('hidden');
                    step3.classList.add('hidden');
                    corrigerBtn.classList.add('hidden');
                }
            });
        });
    </script>
</body>
</html>