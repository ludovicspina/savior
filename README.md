# Savior

**Savior** est un utilitaire complet pour Windows conçu pour automatiser les tâches de post-installation, l'optimisation du système et la surveillance du matériel. Développé en C# (Windows Forms), il simplifie la configuration d'un nouveau PC ou la maintenance d'un système existant.

## 🚀 Fonctionnalités

### 🛠️ Installation & Configuration
*   **Installation de Logiciels Automatisée** : Installez rapidement une liste d'applications essentielles (VLC, Chrome, Steam, Discord, etc.) via **Winget**.
*   **Workflow d'Installation Unifié** : Suppression des bloatwares, désactivation des services et installation d'applications dans une seule fenêtre de progression moderne.
*   **Installation de Pilotes GPU** : Détection automatique et installation des pilotes pour cartes graphiques **NVIDIA** et **AMD**.
*   **Optimisation Windows** :
    *   Suppression des bloatwares préinstallés (applications UWP inutiles).
    *   Désactivation des services inutiles et de la télémétrie.
    *   Gestion des mises à jour Windows.
    *   Configuration des icônes et raccourcis du bureau.
*   **Activation** : Intégration de scripts d'activation (MAS).

### 🎨 Interface Utilisateur
*   **Thème Sombre Complet** : Interface moderne avec palette de couleurs cohérente.
*   **Barre de Titre Personnalisée** : Design sans bordure avec titre draggable.
*   **Onglets Stylisés** : `DarkTabControl` avec rendu personnalisé pour une intégration parfaite.
*   **Logs Colorés** : Affichage différencié des erreurs, avertissements et informations dans la fenêtre de progression.

### 📊 Surveillance & Infos Système
*   **Monitoring Matériel** : Surveillance en temps réel des températures, charges et fréquences (CPU, GPU, RAM) grâce à `LibreHardwareMonitor`.
*   **Informations Système** : Affichage détaillé des spécifications du PC (OS, Processeur, Mémoire, Stockage, Fabricant).
*   **Statut d'Activation Windows** : Vérification automatique de l'état d'activation de Windows.

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

Les profils d'applications (Multimédia, Gaming) sont définis dans `Savior/Constants/AppProfiles.cs`.

## 🔧 Développement

### Environnement
*   Visual Studio 2022 ou JetBrains Rider
*   .NET 8.0 SDK

### Structure du Projet
*   `Savior/UI` : Interfaces utilisateur (Windows Forms).
    *   `MainForm.cs` : Interface principale avec thème sombre.
    *   `InstallProgressForm.cs` : Fenêtre de progression unifiée.
    *   `DarkTabControl.cs` : Contrôle personnalisé pour les onglets sombres.
*   `Savior/Services` : Logique métier (Installateurs, Monitoring, Info Système).
    *   `WingetInstaller.cs` : Gestion des installations Winget avec filtrage de logs.
    *   `ProcessRunner.cs` : Exécution silencieuse des processus avec encodage UTF-8.
    *   `HardwareMonitorService.cs` : Surveillance du matériel.
    *   `SystemInfoService.cs` : Récupération des informations système.
*   `Savior/Constants` : Constantes et profils d'applications prédéfinis.
*   `Savior/Scripts` : Scripts PowerShell et Batch pour les tâches système.
*   `Savior/Config` : Fichiers de configuration JSON.
*   `Savior/Data` : Ressources statiques (Icônes, Installateurs).

### Fonctionnalités Récentes
*   **v1.1.0** (2025-11-22)
    *   Implémentation du thème sombre complet avec composants personnalisés
    *   Unification du workflow d'installation (Bloatware → Services → Winget)
    *   Amélioration de l'encodage UTF-8 pour un affichage correct des caractères
    *   Filtrage avancé des logs Winget (suppression des barres de progression, messages redondants)
    *   Relocalisation des contrôles de température et d'activation vers l'onglet Général
    *   Création de profils d'applications centralisés (`AppProfiles`)

## ⚠️ Avertissement

Ce logiciel effectue des modifications sur votre système (installation de pilotes, désactivation de services, modification du registre, suppression d'applications). Utilisez-le en connaissance de cause. L'auteur n'est pas responsable des éventuels dysfonctionnements.

## 📄 Licence

MIT License

Copyright (c) 2025 Ludovic Spina

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

**Attribution requise** : Si vous utilisez ce logiciel ou des portions de son code, veuillez mentionner la source et/ou le nom de l'auteur (Ludovic Spina).
