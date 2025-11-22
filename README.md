# Savior

**Savior** est un utilitaire complet pour Windows conçu pour automatiser les tâches de post-installation, l'optimisation du système et la surveillance du matériel. Développé en C# (Windows Forms), il simplifie la configuration d'un nouveau PC ou la maintenance d'un système existant.

## 🚀 Fonctionnalités

### 🛠️ Installation & Configuration
*   **Installation de Logiciels Automatisée** : Installez rapidement une liste d'applications essentielles (VLC, Chrome, Steam, Discord, etc.) via **Winget**.
*   **Installation de Pilotes GPU** : Détection automatique et installation des pilotes pour cartes graphiques **NVIDIA** et **AMD**.
*   **Optimisation Windows** :
    *   Désactivation des services inutiles/télémétrie.
    *   Gestion des mises à jour Windows.
    *   Configuration des icônes et raccourcis du bureau.
*   **Activation** : Intégration de scripts d'activation (MAS).

### 📊 Surveillance & Infos Système
*   **Monitoring Matériel** : Surveillance en temps réel des températures, charges et fréquences (CPU, GPU, RAM) grâce à `LibreHardwareMonitor`.
*   **Informations Système** : Affichage détaillé des spécifications du PC (OS, Processeur, Mémoire, Stockage, etc.).

## 📋 Prérequis

*   Windows 10 ou Windows 11 (64-bit)
*   [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (pour exécuter l'application)
*   Droits administrateur (nécessaire pour les installations et modifications système)

## ⚙️ Configuration

Le fichier `Savior/Config/InstallCatalog.json` permet de personnaliser la liste des applications à installer via Winget.
Format : `"ID.Winget": "Nom Affiché"`

Exemple :
```json
{
  "VideoLAN.VLC": "VLC",
  "Google.Chrome": "Chrome",
  "Valve.Steam": "Steam"
}
```

## 🔧 Développement

### Environnement
*   Visual Studio 2022 ou JetBrains Rider
*   .NET 8.0 SDK

### Structure du Projet
*   `Savior/UI` : Interfaces utilisateur (Windows Forms).
*   `Savior/Services` : Logique métier (Installateurs, Monitoring, Info Système).
*   `Savior/Scripts` : Scripts PowerShell et Batch pour les tâches système.
*   `Savior/Config` : Fichiers de configuration JSON.
*   `Savior/Data` : Ressources statiques (Icônes, Installateurs).

## ⚠️ Avertissement

Ce logiciel effectue des modifications sur votre système (installation de pilotes, désactivation de services, modification du registre). Utilisez-le en connaissance de cause. L'auteur n'est pas responsable des éventuels dysfonctionnements.

## 📄 Licence

[À définir par l'utilisateur]
