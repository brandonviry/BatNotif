<#
.SYNOPSIS
    Publie BatNotif en deux versions .exe autonomes (win-x64) dans publish/standard et publish/compact.

.DESCRIPTION
    Deux variantes du même programme, au choix de l'utilisateur (research R11) :
      - standard : exe unique NON compressé (~160 Mo sur disque, ~133 Mo de RAM) — recommandé,
                   l'application tourne en permanence, la mémoire compte plus que la place disque ;
      - compact  : exe unique compressé (~69 Mo sur disque, ~194 Mo de RAM) — le plus petit fichier possible.

    [1/5] Nettoie publish/
    [2/5] Lance les tests du moteur (la publication s'arrête s'ils échouent)
    [3/5] Publie la version standard
    [4/5] Publie la version compact
    [5/5] Affiche les fichiers produits et leur taille

    Chaque étape affiche sa sortie complète (CLAUDE.md §5). Aucun droit administrateur requis.

.EXAMPLE
    ./scripts/publish.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root 'publish'
$appProject = Join-Path $root 'src\BatNotif.App\BatNotif.App.csproj'
$solution = Join-Path $root 'BatNotif.sln'

$variants = @(
    @{ Name = 'standard'; Compress = 'false'; Description = 'non compressé : moins de RAM (recommandé)' },
    @{ Name = 'compact';  Compress = 'true';  Description = 'compressé : plus petit fichier, plus de RAM' }
)

function Fail([string]$message) {
    Write-Host ""
    Write-Host "ÉCHEC : $message" -ForegroundColor Red
    exit 1
}

if (Get-Process -Name BatNotif -ErrorAction SilentlyContinue) {
    Fail "BatNotif est en cours d'exécution. Quitte-le (clic droit sur l'icône → Quitter) puis relance ce script."
}

Write-Host "[1/5] Nettoyage de $publishDir ..." -ForegroundColor Cyan
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force -Verbose $publishDir
}
New-Item -ItemType Directory -Path $publishDir | ForEach-Object { "  Dossier créé : $($_.FullName)" }

Write-Host ""
Write-Host "[2/5] Tests du moteur (dotnet test) ..." -ForegroundColor Cyan
dotnet test $solution -c Release --logger "console;verbosity=normal"
if ($LASTEXITCODE -ne 0) { Fail "les tests ont échoué : publication annulée." }

$step = 3
foreach ($variant in $variants) {
    $output = Join-Path $publishDir $variant.Name
    Write-Host ""
    Write-Host "[$step/5] Publication de la version $($variant.Name) ($($variant.Description)) ..." -ForegroundColor Cyan
    dotnet publish $appProject -p:PublishProfile=win-x64 "-p:EnableCompressionInSingleFile=$($variant.Compress)" -o $output
    if ($LASTEXITCODE -ne 0) { Fail "dotnet publish a échoué pour la version $($variant.Name)." }
    $step++
}

Write-Host ""
Write-Host "[5/5] Résultat ..." -ForegroundColor Cyan
foreach ($variant in $variants) {
    $exe = Join-Path $publishDir "$($variant.Name)\BatNotif.exe"
    if (-not (Test-Path $exe)) { Fail "BatNotif.exe introuvable pour la version $($variant.Name)." }
    $extra = Get-ChildItem (Split-Path $exe) | Where-Object Name -ne 'BatNotif.exe'
    if ($extra) { Fail "fichiers inattendus dans $($variant.Name) : $($extra.Name -join ', ')" }
    "  {0,-9} {1,8:N1} Mo  {2}  — {3}" -f $variant.Name, ((Get-Item $exe).Length / 1MB), $exe, $variant.Description
}
Write-Host ""
Write-Host "OK : deux versions prêtes. Chacune est un seul fichier à copier où tu veux et à lancer." -ForegroundColor Green
