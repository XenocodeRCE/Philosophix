<?php
require_once 'config.php';

try {
    // Récupération des devoirs
    $stmt = $pdo->prepare("
        SELECT c.id, c.note, c.date_correction, d.titre as devoir_titre
        FROM corrections c
        JOIN devoirs d ON c.devoir_id = d.id
        ORDER BY c.date_correction DESC
    ");
    $stmt->execute();
    $corrections = $stmt->fetchAll(PDO::FETCH_ASSOC);

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
    <title>Voir les copies corrigées</title>
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

/* Top Bar et Navigation */
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

/* Content Styles */
.content {
    max-width: 1200px;
    margin: 2rem auto;
    padding: 0 1rem;
}

.main-section {
    display: flex;
    gap: 2rem;
    margin-bottom: 2rem;
}

/* Appreciation Styles */
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

/* Feedback Box Styles */
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

/* Grade Box Styles */
.grade-box {
    background: linear-gradient(135deg, #4F46E5 0%, #7C3AED 100%);
    padding: 2rem;
    border-radius: 12px;
    text-align: center;
    position: relative;
    margin-bottom: 1rem;
    color: white;
    animation: gradeBorder 4s linear infinite;
}

.grade {
    font-size: 3rem;
    font-weight: 700;
}

/* Skill Item Styles */
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
    max-height: 500px;
}

/* Modal Styles */
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

/* Card Animations */
.correction-card {
    transition: all 0.3s ease;
}

.correction-card:hover {
    transform: translateY(-5px);
    box-shadow: 0 10px 20px rgba(0, 0, 0, 0.1);
}

/* Animations */
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

    .main-section {
        flex-direction: column;
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
            <div class="logo">AutoCorrect</div>
            <div class="hamburger">
                <span></span>
                <span></span>
                <span></span>
            </div>
            <div class="nav-links">
                <a href="creer-devoir.php">Créer</a>
                <a href="corriger-copie.php">Corriger</a>
                <a href="voir-devoirs.php">Consulter</a>
                <button class="nav-button">Se connecter</button>
            </div>
        </div>
    </nav>

    <div class="container mx-auto px-4 py-8">
        <div class="bg-white rounded-lg shadow-lg p-6">
            <h1 class="text-2xl font-bold mb-6">Copies corrigées</h1>
            <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                <?php foreach ($corrections as $correction): ?>
                    <div class="correction-card bg-gray-50 p-6 rounded-lg shadow-md hover:shadow-lg transition-all duration-300">
                        <h2 class="text-xl font-bold mb-3 text-gray-800"><?php echo htmlspecialchars($correction['devoir_titre']); ?></h2>
                        <div class="flex items-center mb-3">
                            <span class="text-lg font-semibold <?php echo $correction['note'] >= 10 ? 'text-green-600' : 'text-red-600'; ?>">
                                <?php echo htmlspecialchars($correction['note']); ?>/20
                            </span>
                        </div>
                        <p class="text-gray-600 text-sm mb-4">
                            <span class="font-medium">Corrigé le :</span> 
                            <?php echo date('d/m/Y', strtotime($correction['date_correction'])); ?>
                        </p>
                        <a href="voir-correction.php?id=<?php echo $correction['id']; ?>" 
                           class="inline-block bg-blue-500 text-white px-4 py-2 rounded-lg hover:bg-blue-600 transition-colors duration-300">
                            Voir les détails
                        </a>
                    </div>
                <?php endforeach; ?>
            </div>
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