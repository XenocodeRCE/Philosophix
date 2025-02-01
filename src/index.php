<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Système de Correction Automatique</title>
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/tailwindcss@2.2.19/dist/tailwind.min.css" rel="stylesheet">
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

        /* Mobile First Design */
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

        /* Desktop Optimizations */
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

    <div class="container mx-auto px-4 py-8">
        <h1 class="text-3xl font-bold mb-8 text-center">Système de Correction Automatique</h1>
        
        <div class="grid grid-cols-1 md:grid-cols-2 gap-8">
            <a href="creer-devoir.php" class="bg-blue-500 text-white p-6 rounded-lg shadow-lg hover:bg-blue-600 transition">
                <h2 class="text-xl font-bold mb-2">Créer un devoir</h2>
                <p>Importez ou rédigez l'énoncé d'un nouveau devoir</p>
            </a>
            
            <a href="corriger-copie.php" class="bg-green-500 text-white p-6 rounded-lg shadow-lg hover:bg-green-600 transition">
                <h2 class="text-xl font-bold mb-2">Corriger une copie</h2>
                <p>Évaluez une copie d'élève avec l'aide de l'IA</p>
            </a>

            <a href="voir-devoirs.php" class="bg-purple-500 text-white p-6 rounded-lg shadow-lg hover:bg-purple-600 transition">
                <h2 class="text-xl font-bold mb-2">Voir les copies</h2>
                <p>Consultez les copies corrigées</p>
            </a>
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