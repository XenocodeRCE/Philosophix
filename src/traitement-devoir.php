<?php
require_once 'config.php';

header('Content-Type: application/json');

function sanitizeInput($input) {
    return htmlspecialchars(strip_tags(trim($input)));
}

try {
    // Vérification du titre
    if (empty($_POST['titre'])) {
        throw new Exception('Le titre est obligatoire');
    }
    $titre = sanitizeInput($_POST['titre']);

    // Traitement de l'énoncé
    $enonce = '';
    if (!empty($_FILES['enonce_file']['tmp_name'])) {
        $enonce = file_get_contents($_FILES['enonce_file']['tmp_name']);
    } elseif (!empty($_POST['enonce_text'])) {
        $enonce = sanitizeInput($_POST['enonce_text']);
    } else {
        throw new Exception('L\'énoncé est obligatoire');
    }

    // Traitement du contenu
    $contenu = '';
    if (!empty($_FILES['contenu_file']['tmp_name'])) {
        $contenu = file_get_contents($_FILES['contenu_file']['tmp_name']);
    } elseif (!empty($_POST['contenu_text'])) {
        $contenu = sanitizeInput($_POST['contenu_text']);
    } else {
        throw new Exception('Le contenu est obligatoire');
    }

    // Récupération du barème par défaut
    $stmt = $pdo->prepare("SELECT bareme FROM devoirs WHERE id = 1");
    $stmt->execute();
    $bareme = $stmt->fetchColumn();

    if (!$bareme) {
        throw new Exception('Barème par défaut non trouvé');
    }

    // Insertion dans la base de données
    $stmt = $pdo->prepare("INSERT INTO devoirs (titre, enonce, contenu, bareme) VALUES (?, ?, ?, ?)");
    $stmt->execute([$titre, $enonce, $contenu, $bareme]);

    echo json_encode([
        'success' => true,
        'message' => 'Devoir créé avec succès',
        'id' => $pdo->lastInsertId()
    ]);

} catch (Exception $e) {
    echo json_encode([
        'success' => false,
        'message' => $e->getMessage()
    ]);
}
?>