# 🚀 Installation Rapide - Philosophix CLI

## Étapes d'installation

### 1. Prérequis
- ✅ .NET 9.0 SDK installé
- ✅ Clé API OpenAI active

### 2. Configuration
1. Ouvrir le fichier `OpenAiService.cs`
2. Ligne 16, remplacer `"sk-"` par votre clé API OpenAI :
   ```csharp
   _apiKey = "sk-votre-clé-api-ici";
   ```

### 3. Compilation
```bash
dotnet build
```

### 4. Lancement
**Option A** - Ligne de commande :
```bash
dotnet run
```

**Option B** - Script Windows :
Double-cliquez sur `start.bat`

## 🧪 Test Rapide

### Tester avec l'exemple fourni
1. Choisir "1. Créer un devoir" ou copier `devoirs_exemple.json` vers `devoirs.json`
2. Choisir "3. Corriger une copie"
3. Sélectionner le devoir ID 1
4. Choisir "2. Charger depuis un fichier"
5. Entrer le chemin : `copie_exemple.txt`
6. Attendre la correction automatique

### Créer votre propre devoir
1. Menu "1. Créer un devoir"
2. Saisir titre, énoncé, type (dissertation/explication)
3. Le barème sera automatiquement assigné

## 📁 Structure des Fichiers

```
csharp-cli/
├── Program.cs              # Interface utilisateur
├── CorrectionService.cs    # Logique de correction
├── OpenAiService.cs        # Service API OpenAI
├── JsonDatabaseService.cs  # Persistance données
├── Devoir.cs              # Modèles devoirs
├── Correction.cs          # Modèles corrections
├── README.md              # Documentation complète
├── INSTALLATION.md        # Ce fichier
├── start.bat              # Script de démarrage Windows
├── appsettings.json       # Configuration
├── devoirs_exemple.json   # Exemple de devoir
└── copie_exemple.txt      # Exemple de copie
```

## ⚠️ Points d'Attention

- **Clé API** : Obligatoire pour le fonctionnement
- **Connectivité** : Internet requis pour l'API OpenAI
- **Longueur copie** : Minimum 500 caractères
- **Coût** : Usage de l'API OpenAI facturé selon tarifs OpenAI

## 🆘 Dépannage

### Erreur de compilation
- Vérifier .NET 9.0 installé : `dotnet --version`
- Nettoyer et recompiler : `dotnet clean && dotnet build`

### Erreur API OpenAI
- Vérifier la clé API dans `OpenAiService.cs`
- Contrôler les crédits disponibles sur votre compte OpenAI
- Tester la connectivité internet

### Fichiers manquants
- Les fichiers JSON sont créés automatiquement au premier usage
- Copier les exemples fournis si nécessaire

## 🎯 Utilisation Efficace

1. **Préparer vos devoirs** : Créez d'abord tous vos sujets
2. **Tester avec exemples** : Utilisez les fichiers fournis pour valider
3. **Copier-coller** : Plus rapide que le chargement de fichiers pour les courtes copies
4. **Sauvegarder** : Toutes les corrections sont automatiquement sauvegardées

Vous êtes maintenant prêt à utiliser Philosophix CLI ! 🎉
