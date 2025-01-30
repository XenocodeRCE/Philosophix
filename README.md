<p align="center">
  <img width="256" height="256" src="https://i.imgur.com/RIbF90F.png">
</p>

# Philosophix : évaluer les copies automatiquement
⚠️ Ceci n'a pas pour but de « remplacer » un professeur, rien ne peut se substituer à l'appréciation authentique d'un professeur.

## Installation
### Prérequis 
- Avoir un compte chez [OpenAI et créer une clef API](https://platform.openai.com/api-keys)
- Mettre des sous sur son compte OpenAI.
  - (Une correction d'une copie coûte en moyenne $0.0018 et le prix peut baisser [en fonction des prix d'usage](https://openai.com/api/pricing/))
- Avoir un serveur où héberger le code, il faut impérativement un accès MySQL. Un hébergeur "classique" suffit, il faut chercher sur internet ce qu'on appelle des 'Shared web-hosting'
  - J'utilise personellement Namecheap, ça me coûte 5€ chaque mois. J'utilise ces 5€ pour louer une place sur un serveur partagé, je fais d'autres choses que ce projet avec, ça représente un coût lacunaire.      
### Guide
- Importez le fichier .sql dans MySQL
- Modifier le [contenu de connexion MySQL ici](https://github.com/XenocodeRCE/Philosophix/blob/main/src/config.php)
- [Insérer votre clef API OpenAI ici](https://github.com/XenocodeRCE/Philosophix/blob/bff8d4768b5265e904cf3406b468102d42401f33/src/openai.php#L4)
- Mettez le contenu de ce repo Gitub sur votre server web
- Profitez

## Présentation
### Structure
```md
- config.php //la page pour MySQL
- correction.js //le script de gestion des requettes d'évaluation
- corriger-copie.php //la page de correction de copie
- creer-devoir.php //la page de création de devoir
- get-devoir.php //un script pour récupérer les devoirs
- index.php //un brouillon de page d'accueil
- openai.php //la page qui communique avec OpenAI
- sauvegarder-correction.php //un script pour sauvegarder la correction
- traitement-devoir.php //un script pour ajouter le devoir à la base de données
- voir-correction.php //une page pour voir le détail d'une correction
- voir-devoirs.php //une page pour voir toutes vos copies corrigées
```
### Explication
- Il faut créer un devoir, pour l'instant le système n'accepte que des ***dissertations***
  - Un titre
  - Un énoncé    
- Il faut ensuite lancer une correction
  - On sélectionne le devoir, le sujet du devoir si vous préférez
  - On entre le travail de l'élève
  - On est redirigé, en 15-20 secondes, vers la page pour voir le détail de la correction
### Correction
- Il y a 7 appels à OpenAI, un par compétence, puis un dernier pour l'évaluation globale.
- Voici le détail des compétences, issues de la méthode que j'enseigne à mes élèves :


<details>
  <summary>Détails :</summary>
  
  ```json
{
    "competences": [
      {
        "id": 1,
        "nom": "Compréhension et analyse du sujet",
        "criteres": [
          "Identification des termes clés",
          "Reformulation du sujet",
          "Formulation de la problématique"
        ]
      },
      {
        "id": 2,
        "nom": "Élaboration d'un plan structuré",
        "criteres": [
          "Organisation logique des idées",
          "Maîtrise des types de plans",
          "Annonce claire du plan"
        ]
      },
      {
        "id": 3,
        "nom": "Rédaction de l'introduction et de la conclusion",
        "criteres": [
          "Rédaction de l'accroche",
          "Maîtrise des étapes de l'introduction",
          "Synthèse et ouverture"
        ]
      },
      {
        "id": 4,
        "nom": "Développement de l'argumentation",
        "criteres": [
          "Construction des paragraphes",
          "Pertinence des exemples",
          "Utilisation des références"
        ]
      },
      {
        "id": 5,
        "nom": "Maîtrise de la langue française",
        "criteres": [
          "Orthographe et grammaire",
          "Richesse du vocabulaire",
          "Qualité de la syntaxe"
        ]
      },
      {
        "id": 6,
        "nom": "Cohérence et cohésion textuelle",
        "criteres": [
          "Utilisation des connecteurs",
          "Cohérence globale",
          "Progression logique"
        ]
      },
      {
        "id": 7,
        "nom": "Esprit critique et réflexion personnelle",
        "criteres": [
          "Position argumentée",
          "Évaluation des arguments",
          "Réflexion personnelle"
        ]
      }
    ]
  }
```
  
</details>

### Fiabilité ? 
- ❌ Ca ne remplace pas l'avis d'un professeur. 
- ✅ Au mieux, ça permet d'avoir un second avis.
- 🧐 En évaluant une même copie plusieurs fois, je trouve un écart de 3 points pour les plus complexes.

## Discussion / F.A.Q.
<details>
  <summary>Je n'ai pas de sous 😥</summary>
  
  Aucun soucis ! Vous pouvez utiliser [GroQCloud](https://console.groq.com/keys), qui propose, comme OpenAI mais gratuitement pour l'instant, une clef API et des modèles. Il faut s'y connaître un tout petit peu pour modifier le code du projet. Il y a même le modèle DeepSeek qui fait tant parler de lui en ce moment.
  
</details>
<details>
  <summary>Comment modifier les critères d'évaluation ? 🎓</summary>
  
  C'est très simple ! On parle à l'I.A. comme à un humain, en fait. [Il faut aller ici](https://github.com/XenocodeRCE/Philosophix/blob/bff8d4768b5265e904cf3406b468102d42401f33/src/correction.js#L164) et [là aussi](https://github.com/XenocodeRCE/Philosophix/blob/bff8d4768b5265e904cf3406b468102d42401f33/src/correction.js#L302), et changer les informations écrites. Attention, je ne garantis pas que vos modifications feront marcher le script.
  
</details>

## Photos
![](https://i.imgur.com/wO0bmE4.png)
![](https://i.imgur.com/xGnnRvU.png)
![](https://i.imgur.com/KlOeY3M.png)
![](https://i.imgur.com/VsCmsod.png)
