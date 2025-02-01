<?php
require_once 'config.php';

ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

header('Content-Type: application/json');

if (!isset($_GET['id'])) {
    echo json_encode(['error' => 'ID du devoir non fourni']);
    exit;
}

try {
    $stmt = $pdo->prepare("SELECT * FROM devoirs WHERE id = ?");
    $stmt->execute([$_GET['id']]);
    $devoir = $stmt->fetch(PDO::FETCH_ASSOC);

    if (!$devoir) {
        // Si le devoir n'existe pas, récupérer le barème par défaut
        $stmt = $pdo->prepare("SELECT * FROM devoirs WHERE id = 1");
        $stmt->execute();
        $devoir = $stmt->fetch(PDO::FETCH_ASSOC);
        
        if (!$devoir) {
            echo json_encode(['error' => 'Aucun barème trouvé']);
            exit;
        }
    }

    // Parse le JSON du barème
    $bareme = json_decode($devoir['bareme'], true);
    if (json_last_error() !== JSON_ERROR_NONE) {
        echo json_encode([
            'error' => 'Erreur de parsing JSON',
            'details' => json_last_error_msg()
        ]);
        exit;
    }

    // Vérifier la structure du barème
    if (!isset($bareme['competences']) || !is_array($bareme['competences'])) {
        echo json_encode([
            'error' => 'Structure du barème invalide',
            'bareme' => $bareme
        ]);
        exit;
    }

    $devoir['bareme'] = $bareme;
    echo json_encode($devoir);
    
} catch (Exception $e) {
    echo json_encode([
        'error' => $e->getMessage(),
        'trace' => $e->getTraceAsString()
    ]);
}