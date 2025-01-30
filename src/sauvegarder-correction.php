<?php
require_once 'config.php';

header('Content-Type: application/json');

function sanitizeInput($input) {
    return htmlspecialchars(strip_tags($input));
}

try {
    $data = json_decode(file_get_contents('php://input'), true);
    if (!$data) {
        throw new Exception('Données invalides');
    }

    // Début de la transaction
    $pdo->beginTransaction();

    // Insertion de la correction principale
    $stmt = $pdo->prepare("
        INSERT INTO corrections (
            id, devoir_id, note, appreciation, 
            points_forts, points_ameliorer, competences, copie, date_correction
        ) VALUES (NULL, ?, ?, ?, ?, ?, ?, ?, ?)
    ");

    $stmt->execute([
        $data['devoir_id'],
        $data['note'],
        sanitizeInput($data['appreciation']),
        json_encode($data['points_forts']),
        json_encode($data['points_ameliorer']),
        json_encode($data['competences']),
        sanitizeInput($data['copie']),
        $data['date_correction']
    ]);

    $correction_id = $pdo->lastInsertId();

    // Insertion des évaluations par compétence
    $stmt = $pdo->prepare("
        INSERT INTO evaluations_competences (
            correction_id, nom_competence, note, 
            analyse, points_forts, points_ameliorer
        ) VALUES (?, ?, ?, ?, ?, ?)
    ");

    foreach ($data['competences'] as $comp) {
        $stmt->execute([
            $correction_id,
            sanitizeInput($comp['nom']),
            $comp['note'],
            sanitizeInput($comp['analyse']),
            json_encode($comp['points_forts']),
            json_encode($comp['points_ameliorer'])
        ]);
    }

    $pdo->commit();

    echo json_encode([
        'success' => true,
        'message' => 'Correction sauvegardée avec succès',
        'correction_id' => $correction_id
    ]);

} catch (Exception $e) {
    if ($pdo->inTransaction()) {
        $pdo->rollBack();
    }
    echo json_encode([
        'success' => false,
        'message' => $e->getMessage()
    ]);
}
?>
