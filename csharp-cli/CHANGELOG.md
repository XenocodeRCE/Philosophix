# Changelog - Philosophix CLI

## Version 2.1 - Refactoring Architecture (22 juin 2025)

### 🔄 Refactoring Majeur
- **Séparation des responsabilités** : Extraction de la logique de correction depuis `Program.cs`
- **Nouvelle classe `CorrectionService.cs`** : Centralisation de toute la logique métier de correction
- **Code plus maintenable** : Séparation claire entre interface utilisateur et logique métier

### ✨ Améliorations
- **Architecture SOLID** : Respect des principes de responsabilité unique
- **Testabilité** : Services isolés plus facilement testables
- **Lisibilité** : `Program.cs` réduit de ~300 lignes, plus focus sur l'interface
- **Réutilisabilité** : `CorrectionService` peut être utilisé dans d'autres interfaces

### 📁 Structure Modifiée
```
Avant (v2.0):
- Program.cs (533 lignes) : Interface + Logique métier
- OpenAiService.cs : API + Parsing
- JsonDatabaseService.cs : Persistance
- Devoir.cs / Correction.cs : Modèles

Après (v2.1):
- Program.cs (292 lignes) : Interface utilisateur uniquement
- CorrectionService.cs (245 lignes) : Logique métier de correction
- OpenAiService.cs : API + Parsing
- JsonDatabaseService.cs : Persistance  
- Devoir.cs / Correction.cs : Modèles
```

### 🔧 Détails Techniques

#### Méthodes Déplacées vers CorrectionService
- `CorrigerCopieAsync()` : Orchestration complète de la correction
- `EvaluerCompetenceAsync()` : Évaluation d'une compétence spécifique
- `EvaluerFinalAsync()` : Génération de l'évaluation finale
- `GetEchelleNotation()` : Récupération des échelles de notation
- `ValiderCopie()` : Validation des critères de copie
- `AfficherResultatsCorrection()` : Affichage formaté des résultats

#### Interface Program.cs Simplifiée
- Conservation uniquement des méthodes d'interface utilisateur
- Navigation, saisie, affichage des listes
- Appels délégués vers les services métier

#### Injection de Dépendances
```csharp
var correctionService = new CorrectionService(openAiService, dbService);
```

### 🚀 Bénéfices Immédiats
1. **Maintenance** : Modifications de la logique de correction isolées
2. **Tests** : Possibilité de tester `CorrectionService` indépendamment
3. **Évolution** : Ajout de nouvelles interfaces (Web, API) facilité
4. **Lisibilité** : Code plus clair et organisé
5. **Debugging** : Isolation des erreurs par responsabilité

### 🔄 Migration
- **Aucun changement utilisateur** : Interface identique
- **Compatibilité** : Tous les fichiers de données conservés
- **Performance** : Aucun impact sur les performances

---

## Version 2.0 - Version Initiale (22 juin 2025)

### 🎉 Fonctionnalités Initiales
- Interface CLI complète
- Correction automatisée par compétences
- Gestion des devoirs (création, consultation)
- Historique des corrections
- Support dissertation et explication de texte
- Intégration API OpenAI (GPT-4o-mini)
- Persistance JSON locale

### 📋 Barèmes Philosophie
- **Dissertation** : 7 compétences spécialisées
- **Explication** : 7 compétences d'analyse de texte
- **Échelles officielles** : Conformes aux critères académiques

### 🛠️ Stack Technique
- **.NET 9.0** : Framework moderne
- **JSON** : Stockage local simple
- **OpenAI API** : IA de correction
- **Console App** : Interface utilisateur efficace
