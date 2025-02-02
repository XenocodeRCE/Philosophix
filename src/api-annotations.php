<?php
require_once 'config.php';

// Headers pour les réponses JSON et CORS
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, Accept');

// Fonction de logging
function debugLog($message, $data = null) {
    error_log("DEBUG - " . $message . ($data ? ": " . print_r($data, true) : ""));
}

// Si c'est une requête OPTIONS (pre-flight), on arrête ici
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

// Vérifier que c'est une requête POST
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['error' => 'Méthode non autorisée']);
    exit;
}

try {
    // Récupérer le contenu brut de la requête
    $rawData = file_get_contents('php://input');
    debugLog("Données reçues", $rawData);
    
    // Décoder les données JSON
    $data = json_decode($rawData, true);

    if (json_last_error() !== JSON_ERROR_NONE) {
        throw new Exception('JSON invalide: ' . json_last_error_msg());
    }

    // Valider les données requises
    $requiredFields = ['id', 'copie', 'note', 'appreciation', 'points_forts', 'points_ameliorer'];
    foreach ($requiredFields as $field) {
        if (!isset($data[$field])) {
            throw new Exception("Le champ '$field' est manquant");
        }
    }

    // Préparer le prompt pour l'API OpenAI
    $prompt = "
    Voici une copie d'un élève :
    {$data['copie']}

    Voici les données de correction :
    Note: {$data['note']}
    Appréciation: {$data['appreciation']}
    Points forts: " . implode(', ', $data['points_forts']) . "
    Points à améliorer: " . implode(', ', $data['points_ameliorer']) . "

    Annoter le texte de la copie de l'élève en surlignant les passages et en les commentant. Garder exactement le même texte.

    Répondez UNIQUEMENT au format JSON suivant :
    {
        \"texte\": \"<texte de la copie>\",
        \"annotations\": [
            {
                \"passage\": \"<texte surligné>\",
                \"commentaire\": \"<commentaire sur le passage>\"
            }
        ]
    }";

    debugLog("Prompt envoyé à OpenAI", $prompt);

    // Appel à l'API OpenAI
    $openaiResponse = file_get_contents('openai.php', false, stream_context_create([
        'http' => [
            'method' => 'POST',
            'header' => "Content-Type: application/x-www-form-urlencoded\r\n",
            'content' => http_build_query([
                'system' => 'Vous êtes un professeur de philosophie expérimenté qui corrige des rédactions.',
                'prompt' => $prompt
            ]),
            'timeout' => 30
        ]
    ]));

    if ($openaiResponse === false) {
        throw new Exception('Erreur lors de l\'appel à l\'API OpenAI');
    }

    debugLog("Réponse brute de OpenAI", $openaiResponse);

    $result = json_decode($openaiResponse, true);
    debugLog("Premier décodage", $result);

    if (!$result || !isset($result['response'])) {
        throw new Exception('Réponse OpenAI invalide');
    }

    // On récupère d'abord le contenu du message
    $contentJson = json_decode($result['response'], true);
    if (!$contentJson || !isset($contentJson['choices'][0]['message']['content'])) {
        throw new Exception('Format de réponse OpenAI invalide');
    }

    // Puis on décode le contenu du message qui contient notre JSON
    $messageContent = $contentJson['choices'][0]['message']['content'];
    $annotationsData = json_decode($messageContent, true);
    debugLog("Annotations décodées", $annotationsData);

    if (!$annotationsData || !isset($annotationsData['texte']) || !isset($annotationsData['annotations'])) {
        throw new Exception('Format JSON invalide - Structure attendue non trouvée');
    }

    // Formatage de la réponse finale
    $annotationsContent = json_encode([
        'texte' => $annotationsData['texte'],
        'annotations' => $annotationsData['annotations']
    ]);

    // Sauvegarder en base de données
    $stmt = $pdo->prepare("UPDATE corrections SET annotations = ? WHERE id = ?");
    if (!$stmt->execute([$messageContent, $data['id']])) {
        throw new Exception('Erreur lors de la sauvegarde en base de données');
    }

    // Retourner la réponse formatée
    echo json_encode([
        'texte' => $annotationsData['texte'],
        'annotations' => $annotationsData['annotations']
    ]);

} catch (Exception $e) {
    debugLog("ERREUR", $e->getMessage());
    http_response_code(500);
    echo json_encode([
        'error' => $e->getMessage()
    ]);
}