# Configuration Ollama pour Philosophix CLI

## Qu'est-ce qu'Ollama ?

Ollama est une solution open-source qui permet d'exécuter des modèles de langage (LLM) localement sur votre machine. Contrairement à OpenAI qui est payant et nécessite une connexion internet, Ollama est :

- **Gratuit** : Aucun coût d'utilisation
- **Privé** : Les données restent sur votre machine
- **Hors ligne** : Fonctionne sans connexion internet une fois installé
- **Performant** : Optimisé pour l'exécution locale

## Installation d'Ollama

### Windows
```bash
# Télécharger depuis https://ollama.com/download
# Ou avec winget :
winget install Ollama.Ollama
```

### macOS
```bash
# Télécharger depuis https://ollama.com/download
# Ou avec Homebrew :
brew install ollama
```

### Linux
```bash
curl -fsSL https://ollama.com/install.sh | sh
```

## Configuration des Modèles

### Modèles Recommandés pour la Philosophie

1. **Llama 3.1 8B** (Recommandé) - Équilibre performance/qualité
```bash
ollama pull llama3.1:8b
```

2. **Qwen2.5 14B** - Excellente qualité mais plus lourd
```bash
ollama pull qwen2.5:14b
```

3. **Phi-3 Mini** - Très léger, bon pour les tests
```bash
ollama pull phi3:mini
```

4. **Mixtral 8x7B** - Très bonne qualité mais nécessite plus de RAM
```bash
ollama pull mixtral:8x7b
```

### Démarrage du Service

```bash
# Démarrer Ollama en arrière-plan
ollama serve
```

Le service sera accessible sur `http://localhost:11434`

## Configuration de Philosophix CLI

Modifiez votre `appsettings.json` :

```json
{
  "LLM": {
    "Provider": "Ollama"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.1:8b",
    "Temperature": 1.0
  }
}
```

## Comparaison OpenAI vs Ollama

| Critère | OpenAI | Ollama |
|---------|--------|--------|
| **Coût** | ~0.02-0.04€/correction | Gratuit |
| **Qualité** | Très élevée | Bonne à très bonne |
| **Vitesse** | Rapide (2-5s) | Variable (5-30s) |
| **Privacité** | Données envoyées à OpenAI | 100% local |
| **Installation** | Simple (clé API) | Installation + téléchargement modèles |
| **Ressources** | Aucune | RAM : 8GB min, 16GB recommandé |

## Exigences Système pour Ollama

### Configuration Minimale
- **RAM** : 8 GB (pour modèles 7-8B)
- **Stockage** : 5-10 GB par modèle
- **CPU** : Processeur moderne (derniers 5 ans)

### Configuration Recommandée
- **RAM** : 16 GB ou plus
- **GPU** : Carte graphique NVIDIA (optionnel, accélère significativement)
- **Stockage** : SSD pour de meilleures performances

## Résolution de Problèmes

### Ollama ne démarre pas
```bash
# Vérifier l'installation
ollama --version

# Redémarrer le service
ollama serve
```

### Modèle non trouvé
```bash
# Lister les modèles installés
ollama list

# Télécharger le modèle manquant
ollama pull llama3.1:8b
```

### Performances lentes
1. Vérifiez votre RAM disponible : `htop` (Linux/macOS) ou Gestionnaire des tâches (Windows)
2. Utilisez un modèle plus léger : `phi3:mini`
3. Fermez les autres applications gourmandes

### Erreur de connexion dans Philosophix
1. Vérifiez qu'Ollama est démarré : `ollama serve`
2. Testez l'accès : `curl http://localhost:11434/api/tags`
3. Vérifiez la configuration dans `appsettings.json`

## Test de Fonctionnement

```bash
# Tester Ollama directement
ollama run llama3.1:8b "Écris une phrase sur la philosophie"

# Si ça fonctionne, Philosophix CLI devrait aussi fonctionner
```

## Migration OpenAI → Ollama

Pour passer d'OpenAI à Ollama :

1. Installez Ollama et téléchargez un modèle
2. Modifiez `"Provider": "Ollama"` dans `appsettings.json`
3. Relancez Philosophix CLI
4. Vos données et corrections existantes sont conservées

## Avantages Pédagogiques d'Ollama

- **Économies** : Pas de coûts récurrents pour l'établissement
- **RGPD** : Conformité totale, données jamais transmises
- **Autonomie** : Fonctionne même sans internet
- **Transparence** : Modèles open-source auditables
- **Pérennité** : Pas de dépendance à un service externe

---

*Pour plus d'informations : https://ollama.com/docs*
