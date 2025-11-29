# Script d'installation de winget (Windows Package Manager)
# Ce script télécharge et installe la dernière version stable de winget

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Installation de winget (Windows Package Manager)" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# URL de la page des releases
$releasesUrl = "https://api.github.com/repos/microsoft/winget-cli/releases/latest"

Write-Host "Récupération des informations de la dernière version..." -ForegroundColor Yellow

try {
    # Récupérer les informations de la dernière release
    $release = Invoke-RestMethod -Uri $releasesUrl -UseBasicParsing
    
    # Trouver le fichier .msixbundle dans les assets
    $msixbundle = $release.assets | Where-Object { $_.name -like "*.msixbundle" } | Select-Object -First 1
    
    if (-not $msixbundle) {
        Write-Host "Erreur: Impossible de trouver le fichier .msixbundle" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Version trouvée: $($release.tag_name)" -ForegroundColor Green
    Write-Host "Fichier à télécharger: $($msixbundle.name)" -ForegroundColor Green
    Write-Host ""
    
    # Créer le dossier temporaire si nécessaire
    $tempFolder = "C:\Temp"
    if (-not (Test-Path $tempFolder)) {
        New-Item -ItemType Directory -Path $tempFolder -Force | Out-Null
        Write-Host "Dossier temporaire créé: $tempFolder" -ForegroundColor Green
    }
    
    # Chemin complet du fichier à télécharger
    $downloadPath = Join-Path $tempFolder $msixbundle.name
    
    # Télécharger le fichier
    Write-Host "Téléchargement en cours..." -ForegroundColor Yellow
    Write-Host "URL: $($msixbundle.browser_download_url)" -ForegroundColor Gray
    Write-Host "Destination: $downloadPath" -ForegroundColor Gray
    
    Invoke-WebRequest -Uri $msixbundle.browser_download_url -OutFile $downloadPath -UseBasicParsing
    
    Write-Host "Téléchargement terminé!" -ForegroundColor Green
    Write-Host ""
    
    # Installer le package
    Write-Host "Installation de winget..." -ForegroundColor Yellow
    Add-AppxPackage -Path $downloadPath
    
    Write-Host ""
    Write-Host "======================================" -ForegroundColor Green
    Write-Host "Installation terminée avec succès!" -ForegroundColor Green
    Write-Host "======================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Vous pouvez maintenant utiliser la commande 'winget' dans PowerShell ou CMD" -ForegroundColor Cyan
    Write-Host ""
    
    # Vérifier l'installation
    Write-Host "Vérification de l'installation..." -ForegroundColor Yellow
    try {
        $version = winget --version
        Write-Host "Version installée: $version" -ForegroundColor Green
    } catch {
        Write-Host "Note: Vous devrez peut-être redémarrer PowerShell pour utiliser winget" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "Erreur lors de l'installation: $_" -ForegroundColor Red
    exit 1
}
