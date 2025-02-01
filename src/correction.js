console.log('Script de correction chargé'); // Debug

// Inclure la bibliothèque md5
function md5(string) {
    return CryptoJS.MD5(string).toString();
}

document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM chargé'); // Debug
    
    const devoirSelect = document.getElementById('devoir_id');
    const step2 = document.getElementById('step2');
    const step3 = document.getElementById('step3');
    const corrigerBtn = document.getElementById('corriger');
    
    if (!devoirSelect || !step2 || !step3 || !corrigerBtn) {
        console.error('Éléments manquants:', {
            devoirSelect: !!devoirSelect,
            step2: !!step2,
            step3: !!step3,
            corrigerBtn: !!corrigerBtn
        });
        return;
    }

    // Montrer/cacher les éléments en fonction de la sélection
    devoirSelect.addEventListener('change', async function() {
        if (this.value) {
            step2.classList.remove('hidden');
            step3.classList.remove('hidden');
            corrigerBtn.classList.remove('hidden');
        } else {
            step2.classList.add('hidden');
            step3.classList.add('hidden');
            corrigerBtn.classList.add('hidden');
            document.getElementById('resultats').classList.add('hidden');
        }
    });

    // Gestionnaire du bouton de correction
    corrigerBtn.addEventListener('click', async function() {
        const devoir_id = devoirSelect.value;
        const fileInput = document.getElementById('copie_file');
        const textInput = document.getElementById('copie_text');
        const passwordInput = document.getElementById('password');
        const passwordHash = md5(passwordInput.value);

        if (passwordHash !== 'eb1ec90d51150748189a5c844d9faa45') {
            alert('Mot de passe invalide');
            return;
        }

        if (fileInput.files.length > 0) {
            try {
                copie = await fileInput.files[0].text();
            } catch (error) {
                console.error('Erreur lecture fichier:', error);
                alert('Erreur lors de la lecture du fichier');
                return;
            }
        } else if (textInput.value.trim()) {
            if (textInput.value.trim().length < 500) {
                alert('Le texte de la copie doit contenir au moins 500 caractères.');
                return;
            }
            copie = textInput.value.trim();
        } else {
            alert('Veuillez fournir une copie à corriger');
            return;
        }

        if (!devoir_id) {
            alert('Veuillez sélectionner un devoir');
            return;
        }

        try {
            await corrigerCopie(devoir_id, copie);
        } catch (error) {
            console.error('Erreur:', error);
            alert('Une erreur est survenue lors de la correction');
        }
    });
});

// Fonction pour extraire le contenu JSON de la réponse de l'API
function extractJsonFromApiResponse(apiResponse) {
    try {
        const parsed = JSON.parse(apiResponse.response);
        if (parsed.choices && parsed.choices[0].message.content) {
            const content = parsed.choices[0].message.content;
            // Nettoyer la réponse des balises Markdown
            const cleanJson = content.replace(/```json\n?|\n?```/g, '').trim();
            return JSON.parse(cleanJson);
        }
        throw new Error('Format de réponse invalide');
    } catch (error) {
        console.error('Réponse brute:', apiResponse.response);
        console.error('Erreur lors du parsing de la réponse:', error);
        throw error;
    }
}

// Gestionnaire de progression
class ProgressManager {
    constructor() {
        this.modal = document.getElementById('progressModal');
        this.globalProgress = document.getElementById('globalProgress');
        this.globalProgressText = document.getElementById('globalProgressText');
        this.competencesProgress = document.getElementById('competencesProgress');
        this.evaluations = new Map();
    }

    show() {
        this.modal.classList.remove('hidden');
        this.modal.classList.add('flex');
    }

    hide() {
        this.modal.classList.add('hidden');
        this.modal.classList.remove('flex');
    }

    initializeCompetences(competences) {
        this.competencesProgress.innerHTML = '';
        this.totalCompetences = competences.length;
        this.completedCompetences = 0;

        competences.forEach(competence => {
            const div = document.createElement('div');
            div.id = `progress-${competence.id}`;
            div.innerHTML = `
                <div class="flex justify-between items-center mb-1">
                    <span class="text-sm font-medium">${competence.nom}</span>
                    <span class="text-sm text-gray-500" id="status-${competence.id}">En attente...</span>
                </div>
                <div class="w-full bg-gray-200 rounded-full h-2">
                    <div id="bar-${competence.id}" 
                        class="bg-gray-600 h-2 rounded-full transition-all duration-500" 
                        style="width: 0%">
                    </div>
                </div>
            `;
            this.competencesProgress.appendChild(div);
        });
    }

    updateCompetenceProgress(competenceId, status, progress) {
        const bar = document.getElementById(`bar-${competenceId}`);
        const statusEl = document.getElementById(`status-${competenceId}`);
        
        if (bar && statusEl) {
            bar.style.width = `${progress}%`;
            statusEl.textContent = status;
            
            if (progress === 100) {
                bar.classList.remove('bg-gray-600');
                bar.classList.add('bg-green-600');
                this.completedCompetences++;
                this.updateGlobalProgress();
            }
        }
    }

    updateGlobalProgress() {
        const progress = (this.completedCompetences / this.totalCompetences) * 100;
        this.globalProgress.style.width = `${progress}%`;
        this.globalProgressText.textContent = `Progression globale: ${Math.round(progress)}%`;
    }

    addEvaluation(competenceId, evaluation) {
        this.evaluations.set(competenceId, evaluation);
    }

    getAllEvaluations() {
        return Array.from(this.evaluations.values());
    }
}

async function evaluerCompetence(competence, copie, enonce, typeDevoir) {
    const prompt = `
    Évaluez la compétence "${competence.nom}" dans cette composition philosophique.
    
    Critères d'évaluation :
    ${competence.criteres.join('\n')}
    
    Sujet du travail :
    ${enonce}

    Niveau scolaire de l'élève : Terminale au lycée.

    Style d'appréciation : Formel, vouvoie l'apprenant.

    Type de devoir : ${typeDevoir}

    Copie de l'élève :
    ${copie}

    Répondez UNIQUEMENT au format JSON suivant :
    {
        "note": <note sur 20>,
        "analyse": "<analyse détaillée qui cite des éléments de la copie>",
        "points_forts": ["point fort 1", "point fort 2", ...],
        "points_ameliorer": ["point à améliorer 1", "point à améliorer 2", ...]
    }

    Évaluez UNIQUEMENT cette compétence, rien d'autre.
    Pour l'analyse, cites des éléments de la copie pour justifier ta note, et addresses-toi à l'élève directement.

    Degré de sévérité : 3 / 5
    `;

    try {
        const response = await fetch('openai.php', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: new URLSearchParams({
                'system': 'Vous êtes un professeur de philosophie expérimenté qui corrige des rédactions.',
                'prompt': prompt
            })
        });

        return await response.json();

    } catch (error) {
        console.error('Erreur API:', error);
        throw error;
    }
}

async function corrigerCopie(devoir_id, copie) {
    const progress = new ProgressManager();
    try {
        // Récupérer le devoir et son barème
        const response = await fetch(`get-devoir.php?id=${devoir_id}`);
        const devoir = await response.json();

        if (!devoir.bareme || !devoir.bareme.competences) {
            throw new Error('Le barème est mal formaté');
        }

        // Cacher les résultats précédents
        document.getElementById('resultats').classList.add('hidden');
        
        // Initialiser le modal de progression
        progress.show();
        progress.initializeCompetences(devoir.bareme.competences);

        // Lancer toutes les évaluations en parallèle
        const evaluationPromises = devoir.bareme.competences.map(async (competence) => {
            progress.updateCompetenceProgress(competence.id, "Évaluation en cours...", 50);
            
            try {
                const evaluation = await evaluerCompetence(competence, copie, devoir.enonce, devoir.type);
                const parsedEvaluation = extractJsonFromApiResponse(evaluation);
                
                progress.updateCompetenceProgress(competence.id, "Évaluation terminée", 100);
                progress.addEvaluation(competence.id, parsedEvaluation);
                
                return { competence, evaluation: parsedEvaluation };
            } catch (error) {
                progress.updateCompetenceProgress(competence.id, "Erreur", 100);
                console.error(`Erreur pour la compétence ${competence.nom}:`, error);
                throw error;
            }
        });

        // Attendre que toutes les évaluations soient terminées
        const results = await Promise.all(evaluationPromises);

        // Évaluation finale
        const evaluationFinale = await evaluerFinal(results, copie, devoir.type);
        const parsedEvaluationFinale = extractJsonFromApiResponse(evaluationFinale);

        // Calculer la note moyenne
        const noteMoyenne = results.reduce((sum, { evaluation }) => sum + evaluation.note, 0) / results.length;

        // Sauvegarder automatiquement la correction
        const correction = {
            devoir_id: document.getElementById('devoir_id').value,
            note: noteMoyenne,
            appreciation: parsedEvaluationFinale.appreciation,
            points_forts: parsedEvaluationFinale.points_forts,
            points_ameliorer: parsedEvaluationFinale.points_ameliorer,
            competences: results.map(r => ({
                nom: r.competence.nom,
                note: r.evaluation.note,
                analyse: r.evaluation.analyse,
                points_forts: r.evaluation.points_forts,
                points_ameliorer: r.evaluation.points_ameliorer
            })),
            copie: document.getElementById('copie_text').value || 'Copie non disponible',
            date_correction: new Date().toISOString()
        };

        const saveResponse = await fetch('sauvegarder-correction.php', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(correction)
        });

        const data = await saveResponse.json();
        if (data.success) {
            window.location.href = `voir-correction.php?id=${data.correction_id}`;
        } else {
            throw new Error(data.message);
        }

    } catch (error) {
        console.error('Erreur lors de la correction:', error);
        progress.hide();
        alert('Une erreur est survenue lors de la correction');
    }
}

async function evaluerFinal(results, copie, typeDevoir) {

    let echelleNotation = '';

    if (typeDevoir === 'dissertation') {
        echelleNotation = `Échelle d’évaluation pour guider la notation des copies :
    """
    - Ce qui est valorisé : une problématisation du sujet, une argumentation cohérente et progressive, l’analyse de concepts (notions, distinctions) et d’exemples précisément étudiés, la mobilisation d’éléments de culture philosophique au service du traitement du sujet, la capacité de la réflexion à entrer en dialogue avec elle-même. 
    - Ce qui est sanctionné : la paraphrase du texte, la récitation de cours sans lien avec le sujet, l’accumulation de lieux communs, la juxtaposition d’exemples sans réflexion, l’absence de problématisation, l’absence de rigueur dans le raisonnement, l’absence de culture philosophique mobilisée pour traiter le sujet.
    
    # Échelle de notation :
    - Entre 0 et 5 → copie très insuffisante : inintelligible ; non structurée ; excessivement brève ; marquant un refus manifeste de faire l’exercice.
    - De 06 à 09 → Copie intelligible mais qui ne répond pas aux critères attestés de l’épreuve : propos excessivement général ou restant sans rapport avec la question posée ; juxtaposition d’exemples sommaires ou anecdotiques ; accumulation de lieux communs ; paraphrase ou répétition du texte ; récitation de cours sans traitement du sujet ;- copie qui aurait pu être rédigée au début de l’année, sans aucun cours de philosophie ou connaissances acquises.
    - Pas moins de 10 → Copie témoignant d’un réel effort de réflexion, et, même si le résultat n’est pas abouti, de traitement du sujet : effort de problématisation ; effort de définition des notions ; examen de réponses possibles ; cohérence globale du propos.
    - Pas moins de 12 → Si, en plus, il y a mobilisation de références et d’exemples pertinents pour le sujet.
    - Pas moins de 14 → Si, en plus, le raisonnement est construit, progressif, et que les affirmations posées sont rigoureusement justifiées.
    - Pas moins de 16 → Si, en plus, la copie témoigne de la maîtrise de concepts philosophiques utiles pour le sujet (notions, repères), d’une démarche de recherche et du souci des enjeux de la question, d’une précision dans l’utilisation d’une culture au service du traitement du sujet. 
    """`;
    }else{
        echelleNotation = `Échelle d’évaluation pour guider la notation des copies :
    """
    - Ce qui est valorisé : une détermination du problème du texte, une explication de ses éléments signifiants, une explicitation des articulations du texte, une caractérisation  de la position philosophique élaborée par  l’auteur dans le texte, et, plus généralement,  du questionnement auquel elle s’articule.
    - Ce qui est sanctionné : la paraphrase du texte, la récitation de cours sans lien avec le texte de l'auteur, l’accumulation de lieux communs, la juxtaposition d’exemples sans réflexion, l’absence de problématisation du texte, l’absence de rigueur dans le raisonnement, l’absence de culture philosophique mobilisée pour traiter le sujet.

    # Échelle de notation :
    - Entre 0 et 5 → copie très insuffisante : inintelligible ; non structurée ; excessivement brève ; marquant un refus manifeste de faire l’exercice.
    - De 06 à 09 → Copie intelligible mais qui ne répond pas aux critères attestés de l’épreuve : propos excessivement général ou restant sans rapport avec la question posée ; juxtaposition d’exemples sommaires ou anecdotiques ; accumulation de lieux communs ; paraphrase ou répétition du texte ; récitation de cours sans traitement du sujet ;- copie qui aurait pu être rédigée au début de l’année, sans aucun cours de philosophie ou connaissances acquises.
    - Pas moins de 10 → Copie faisant l’effort de réaliser l’exercice, même si l’explication demeure maladroite et inaboutie : explication commençante ; pas de contresens majeur sur le propos et la démarche de l’auteur.
    - Pas moins de 12 → Si, en plus, le texte est interrogé avec un effort d’attention au détail du propos, ainsi qu’à sa structure logique.
    - Pas moins de 14 → Si, en plus, les éléments du texte sont mis en perspective, avec des éléments de connaissance permettant de déterminer et d’examiner le problème.
    - Pas moins de 16 → Si, en plus, l’explication est développée avec amplitude et justesse : l’ensemble du texte est examiné et bien situé dans une problématique  et un questionnement pertinents.
    """
    `;
    }
    const prompt = `
    Type de devoir : ${typeDevoir}
    En tant que professeur de philosophie, faites une évaluation globale de cette copie.
    Voici les évaluations par compétence :
    ${results.map(r => `${r.competence.nom}: ${r.evaluation.note}/20 - ${r.evaluation.analyse}`).join('\n')}

    Copie de l'élève :
    ${copie}

    Niveau scolaire de l'élève : Terminale au lycée.

    Style d'appréciation : Formel, vouvoie l'apprenant.

    ${echelleNotation}

    Répondez UNIQUEMENT au format JSON suivant :
    {
        "appreciation": "<appréciation générale détaillée>",
        "points_forts": ["point fort 1", "point fort 2", "point fort 3"],
        "points_ameliorer": ["point 1", "point 2", "point 3"]
    }

    Pour l'appreciation addresses-toi à l'élève directement.
    Degré de sévérité : 3 / 5
    `;

    try {
        const response = await fetch('openai.php', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: new URLSearchParams({
                'system': 'Vous êtes un professeur de philosophie expérimenté qui corrige des rédactions.',
                'prompt': prompt
            })
        });

        return await response.json();

    } catch (error) {
        console.error('Erreur API:', error);
        throw error;
    }
}

function afficherResultats(results, evaluationFinale) {
    // Vérifier que tous les éléments nécessaires existent
    const elements = {
        competences: document.getElementById('competences'),
        note: document.getElementById('note'),
        appreciation: document.getElementById('appreciation'),
        pointsForts: document.getElementById('points_forts'),
        pointsAmeliorer: document.getElementById('points_ameliorer')
        // Retirer la référence à apercu_copie qui n'existe plus
    };

    // Vérifier si tous les éléments sont présents
    for (const [key, element] of Object.entries(elements)) {
        if (!element) {
            console.error(`Élément manquant: ${key}`);
            throw new Error(`Élément DOM manquant: ${key}`);
        }
    }

    // Afficher les résultats pour chaque compétence
    elements.competences.innerHTML = '';

    results.forEach(({ competence, evaluation }) => {
        const note = evaluation.note;
        let progressClass = 'progress-poor';
        if (note >= 16) progressClass = 'progress-excellent';
        else if (note >= 12) progressClass = 'progress-good';
        else if (note >= 8) progressClass = 'progress-average';

        const div = document.createElement('div');
        div.className = 'bg-white rounded-lg shadow-lg overflow-hidden result-animation';
        div.innerHTML = `
            <button class="accordion-button w-full p-4 text-left font-bold flex justify-between items-center bg-white hover:bg-gray-50">
                <span class="flex items-center">
                    <span class="mr-3">${competence.nom}</span>
                    <span class="text-sm font-normal text-gray-500">(cliquez pour voir les détails)</span>
                </span>
                <span class="text-xl font-bold ${note >= 10 ? 'text-green-600' : 'text-red-600'}">${note}/20</span>
            </button>
            <div class="accordion-content">
                <div class="w-full bg-gray-200 rounded-full h-4 mb-4">
                    <div class="${progressClass} h-4 rounded-full progress-bar" 
                        style="width: ${note * 5}%">
                    </div>
                </div>
                <p class="text-gray-600 mb-4 leading-relaxed">${evaluation.analyse}</p>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div class="bg-green-50 p-4 rounded-lg">
                        <h4 class="font-semibold text-green-700 mb-2">
                            <i class="fas fa-check-circle mr-2"></i>Points forts
                        </h4>
                        <ul class="list-disc pl-5 text-sm text-green-600 space-y-1">
                            ${evaluation.points_forts.map(point => `<li>${point}</li>`).join('')}
                        </ul>
                    </div>
                    <div class="bg-red-50 p-4 rounded-lg">
                        <h4 class="font-semibold text-red-700 mb-2">
                            <i class="fas fa-exclamation-circle mr-2"></i>Points à améliorer
                        </h4>
                        <ul class="list-disc pl-5 text-sm text-red-600 space-y-1">
                            ${evaluation.points_ameliorer.map(point => `<li>${point}</li>`).join('')}
                        </ul>
                    </div>
                </div>
            </div>
        `;
        elements.competences.appendChild(div);

        const button = div.querySelector('.accordion-button');
        const content = div.querySelector('.accordion-content');
        button.addEventListener('click', () => {
            content.classList.toggle('open');
            button.classList.toggle('active');
            button.classList.toggle('bg-gray-50');
        });
    });

    // Calculer et afficher la note finale
    const noteMoyenne = results.reduce((sum, { evaluation }) => sum + evaluation.note, 0) / results.length;
    elements.note.textContent = noteMoyenne.toFixed(1);

    // Afficher l'évaluation finale
    elements.appreciation.textContent = evaluationFinale.appreciation;

    // Afficher les points forts et à améliorer
    elements.pointsForts.innerHTML = evaluationFinale.points_forts
        .map(point => `<li>${point}</li>`).join('');
    elements.pointsAmeliorer.innerHTML = evaluationFinale.points_ameliorer
        .map(point => `<li>${point}</li>`).join('');

    // Rendre visible la section des résultats et le bouton de sauvegarde
    document.getElementById('resultats').classList.remove('hidden');
    const btnSauvegarder = document.getElementById('sauvegarder');
    btnSauvegarder.classList.remove('hidden');
    
    // Ajouter l'événement de sauvegarde
    btnSauvegarder.addEventListener('click', async () => {
        const correction = {
            devoir_id: document.getElementById('devoir_id').value,
            note: noteMoyenne,
            appreciation: evaluationFinale.appreciation,
            points_forts: evaluationFinale.points_forts,
            points_ameliorer: evaluationFinale.points_ameliorer,
            competences: results.map(r => ({
                nom: r.competence.nom,
                note: r.evaluation.note,
                analyse: r.evaluation.analyse,
                points_forts: r.evaluation.points_forts,
                points_ameliorer: r.evaluation.points_ameliorer
            })),
            // Récupérer le contenu de la copie depuis le textarea ou le fichier
            copie: document.getElementById('copie_text').value || 
                   'Copie non disponible',
            date_correction: new Date().toISOString()
        };

        try {
            const response = await fetch('sauvegarder-correction.php', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(correction)
            });

            const data = await response.json();
            if (data.success) {
                alert('Correction sauvegardée avec succès !');
                window.location.href = `voir-correction.php?id=${data.correction_id}`;
            } else {
                throw new Error(data.message);
            }
        } catch (error) {
            console.error('Erreur lors de la sauvegarde:', error);
            alert('Une erreur est survenue lors de la sauvegarde de la correction.');
        }
    });
}