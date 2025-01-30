<?php

// Mettez ici votre clef API OpenAI
$apiKey = "sk-";

// Fonction pour envoyer une requête à l'API d'OpenAI
function askGPT($system, $prompt) {
    global $apiKey;

    $url = 'https://api.openai.com/v1/chat/completions';

    // Préparer les données JSON
    $data = json_encode([
        'messages' => [
            [
                'role' => 'system',
                'content' => $system
            ],
            [
                'role' => 'user',
                'content' => $prompt
            ]
        ],
        'model' => 'gpt-4o-mini',
        'temperature' => 1,
        'max_tokens' => 8000,
        'top_p' => 1,
        'stream' => false,
        'stop' => null
    ]);

    // Initialiser cURL
    $ch = curl_init($url);

    // Configurer les options cURL
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_HTTPHEADER, [
        'Content-Type: application/json',
        'Authorization: Bearer ' . $apiKey,
    ]);
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_POSTFIELDS, $data);

    // Exécuter la requête cURL et récupérer la réponse
    $response = curl_exec($ch);
    $error = curl_error($ch);

    // Fermer cURL
    curl_close($ch);

    // Gérer les erreurs
    if ($error) {
        return 'Erreur cURL : ' . $error;
    } else {
        return $response;
    }
}


// Vérifier si les données ont été envoyées via POST
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    // Récupérer les données du formulaire via POST
    $system = isset($_POST['system']) ? $_POST['system'] : '';
    $prompt = isset($_POST['prompt']) ? $_POST['prompt'] : '';

    // Appeler la fonction askGPT avec les données récupérées
    $response = askGPT($system, $prompt);

    // Retourner la réponse au format JSON
    header('Content-Type: application/json');
    echo json_encode(['response' => ($response)]);
}