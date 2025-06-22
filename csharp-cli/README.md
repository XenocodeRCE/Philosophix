# Philosophix CLI v2.0

## Description

Version en ligne de commande (CLI) de Philosophix - un système de correction automatisée de copies de philosophie utilisant l'intelligence artificielle (GPT-4o-mini).

## Fonctionnalités

### ✅ Gestion des Devoirs
- **Créer un devoir** : Définir le titre, énoncé, type (dissertation/explication)
- **Voir les devoirs** : Lister tous les devoirs créés avec leurs détails
- **Barèmes automatiques** : Attribution des grilles de compétences selon le type de devoir

### ✅ Correction Automatisée
- **Correction par compétences** : Évaluation détaillée selon 7 compétences spécifiques
- **Gestion du PAP** : Support des Plans d'Accompagnement Personnalisé
- **Analyse détaillée** : Utilisation des mêmes prompts que la version web
- **Échelles de notation** : Respect des critères académiques officiels
- **Saisie flexible** : Copie directe ou chargement depuis un fichier

### 🆕 Support du PAP (Plan d'Accompagnement Personnalisé)
- **Détection automatique** : Question posée lors de la correction
- **Exclusion ciblée** : Les compétences d'expression ne sont pas évaluées
- **Prompts adaptés** : Messages spécifiques pour l'IA indiquant de ne pas tenir compte de l'orthographe/grammaire
- **Affichage informatif** : Indication claire quand le mode PAP est activé

### ✅ Résultats Complets
- **Note globale** : Moyenne pondérée des compétences
- **Appréciation générale** : Évaluation synthétique personnalisée
- **Points forts** : Identification des réussites de l'élève
- **Points à améliorer** : Suggestions concrètes de progression
- **Détail par compétence** : Note et analyse spécifique pour chaque critère

### ✅ Persistance des Données
- **Sauvegarde JSON** : Stockage local des devoirs et corrections
- **Historique** : Consultation des corrections précédentes
- **Export** : Données facilement exploitables

## Installation

### Prérequis
- .NET 9.0 SDK
- Clé API OpenAI

### Configuration
1. Cloner le repository
2. Naviguer vers le dossier `csharp-cli`
3. Modifier `OpenAiService.cs` ligne 16 pour ajouter votre clé API OpenAI :
   ```csharp
   _apiKey = "sk-votre-clé-api-ici";
   ```

### Compilation
```bash
cd csharp-cli
dotnet build
```

### Exécution
```bash
dotnet run
```

## Utilisation

### Menu Principal
```
╔════════════════════════════════════════════════╗
║              Philosophix CLI v2.0              ║
║        Correction automatisée de copies       ║
╚════════════════════════════════════════════════╝

1. Créer un devoir
2. Voir les devoirs
3. Corriger une copie
4. Voir les corrections
5. Quitter
```

### Workflow de Correction

1. **Créer un devoir** (si ce n'est pas déjà fait)
   - Saisir titre, énoncé et type
   - Le barème est automatiquement attribué

2. **Corriger une copie**
   - Sélectionner le devoir
   - Saisir ou charger la copie (minimum 500 caractères)
   - Attendre l'analyse automatique

3. **Consulter les résultats**
   - Note finale sur 20
   - Appréciation détaillée
   - Points forts et améliorations
   - Analyse par compétence

## Types de Devoirs Supportés

### Dissertation
**Compétences évaluées :**
- Compréhension et analyse du sujet
- Élaboration d'un plan structuré
- Rédaction de l'introduction et de la conclusion
- Développement de l'argumentation
- Maîtrise de la langue française
- Cohérence et cohésion textuelle
- Esprit critique et réflexion personnelle

### Explication de Texte
**Compétences évaluées :**
- Lecture analytique et compréhension globale
- Analyse de la structure argumentative
- Analyse conceptuelle
- Analyse argumentative
- Contextualisation philosophique
- Expression et rédaction
- Appropriation critique

## Exemples de Sortie

### Note Finale
```
🎯 NOTE FINALE : 14.2/20
📅 Date de correction : 22/06/2025 14:30
```

### Appréciation
```
💬 APPRÉCIATION GÉNÉRALE
Votre copie témoigne d'un effort de réflexion et d'une bonne 
compréhension du sujet. L'argumentation est globalement cohérente...
```

### Détail par Compétence
```
📊 DÉTAIL PAR COMPÉTENCE

1. Compréhension et analyse du sujet - 15.0/20
   Vous avez correctement identifié les enjeux du sujet...
   ✅ Points forts :
      • Bonne reformulation du sujet
      • Problématique pertinente
   📈 À améliorer :
      • Approfondir l'analyse des termes clés
```

## Architecture Technique

### Structure des Fichiers
- `Program.cs` : Interface utilisateur et navigation principale
- `CorrectionService.cs` : **[NOUVEAU]** Logique métier de correction
- `OpenAiService.cs` : Communication avec l'API OpenAI
- `JsonDatabaseService.cs` : Persistance des données
- `Devoir.cs` : Modèles pour les devoirs et barèmes
- `Correction.cs` : Modèles pour les corrections

### Séparation des Responsabilités
- **Program.cs** : Interface utilisateur, navigation, saisie/affichage
- **CorrectionService.cs** : Orchestration de la correction, logique métier
- **OpenAiService.cs** : Communication API et parsing des réponses
- **JsonDatabaseService.cs** : Accès aux données JSON

### Données
- `devoirs.json` : Stockage des devoirs créés
- `devoirs_corrections.json` : Stockage des corrections

### API
- **Modèle** : GPT-4o-mini
- **Température** : 1 (créativité modérée)
- **Tokens max** : 8000
- **Prompts** : Identiques à la version web Philosophix

## Avantages de la Version CLI

- **Performance** : Exécution locale, pas de dépendance web
- **Portabilité** : Fonctionne sur Windows, macOS, Linux
- **Simplicité** : Interface épurée, focus sur l'essentiel
- **Sécurité** : Données stockées localement
- **Extensibilité** : Code C# facilement modifiable

## Support

Pour toute question ou problème :
- Vérifier la clé API OpenAI
- S'assurer que .NET 9.0 est installé
- Contrôler la connectivité internet pour l'API
- Minimum 500 caractères pour les copies à corriger

## Licence

Ce projet est une extension de Philosophix. Respecter les conditions d'utilisation de l'API OpenAI.
