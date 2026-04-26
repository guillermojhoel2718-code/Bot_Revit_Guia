# ================================================================
# instalar_plugin.ps1
# Ejecutar en PowerShell como ADMINISTRADOR
# ================================================================
# USO:
#   1. Abrir PowerShell como Administrador
#   2. Set-ExecutionPolicy Bypass -Scope Process
#   3. cd F:\Pluginrevitbot
#   4. .\instalar_plugin.ps1
# ================================================================

$ErrorActionPreference = "Stop"
$ROOT      = "F:\Pluginrevitbot"
$CSPROJ    = "$ROOT\Source\BOT\BOT.csproj"
$ADDIN_SRC = "$ROOT\Source\BOT\RevitTutor.addin"
$OUTPUT    = "$ROOT\Addin"
$DLL_OUT   = "$OUTPUT\BOT.dll"
$REVIT_ADD = "C:\ProgramData\Autodesk\Revit\Addins\2025"
$GITHUB    = "https://github.com/guillermojhoel2718-code/Bot_Revit_Guia.git"

Write-Host "`n=====================================================" -ForegroundColor Cyan
Write-Host "  RevitTutorIA — Build + Instalar + Git Push" -ForegroundColor Cyan
Write-Host "=====================================================`n" -ForegroundColor Cyan

# ── PASO 1: Compilar ─────────────────────────────────────────
Write-Host "[1/5] Compilando plugin..." -ForegroundColor Yellow
dotnet build $CSPROJ -c Debug --nologo
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR en build" -ForegroundColor Red; exit 1 }
Write-Host "     Build OK" -ForegroundColor Green

# ── PASO 2: Actualizar .addin con ruta absoluta ───────────────
Write-Host "[2/5] Actualizando RevitTutor.addin con ruta absoluta..." -ForegroundColor Yellow

$addinContent = @"
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>RevitTutorIA</Name>
    <Assembly>$REVIT_ADD\BOT.dll</Assembly>
    <FullClassName>RevitTutor.App</FullClassName>
    <AddInId>a1b2c3d4-e5f6-7890-abcd-ef1234567890</AddInId>
    <VendorId>RevitTutorIA</VendorId>
    <VendorDescription>Tutor IA para estudiantes - Solo lectura</VendorDescription>
  </AddIn>
</RevitAddIns>
"@

Set-Content -Path $ADDIN_SRC -Value $addinContent -Encoding UTF8
Write-Host "     .addin actualizado" -ForegroundColor Green

# ── PASO 3: Instalar en Revit Addins ─────────────────────────
Write-Host "[3/5] Instalando en $REVIT_ADD ..." -ForegroundColor Yellow

if (-not (Test-Path $REVIT_ADD)) {
    New-Item -ItemType Directory -Path $REVIT_ADD | Out-Null
}

# Copiar DLL
Copy-Item $DLL_OUT "$REVIT_ADD\BOT.dll" -Force
Write-Host "     BOT.dll copiado" -ForegroundColor Green

# Copiar .addin
Copy-Item $ADDIN_SRC "$REVIT_ADD\RevitTutor.addin" -Force
Write-Host "     RevitTutor.addin copiado" -ForegroundColor Green

# Copiar sprites
$spriteDst = "$REVIT_ADD\sprites"
New-Item -ItemType Directory -Force -Path $spriteDst | Out-Null
Get-ChildItem "$OUTPUT\sprites\*.png" | Copy-Item -Destination $spriteDst -Force
$count = (Get-ChildItem "$spriteDst\*.png").Count
Write-Host "     $count sprites copiados" -ForegroundColor Green

# ── PASO 4: Desbloquear DLL (Windows Security) ───────────────
Write-Host "[4/5] Desbloqueando DLL..." -ForegroundColor Yellow
Unblock-File "$REVIT_ADD\BOT.dll"
Write-Host "     Unblock-File OK" -ForegroundColor Green

# ── PASO 5: Git — commit y push ───────────────────────────────
Write-Host "[5/5] Guardando en GitHub..." -ForegroundColor Yellow

Set-Location $ROOT

# Inicializar git si no existe
if (-not (Test-Path "$ROOT\.git")) {
    git init
    git remote add origin $GITHUB
    Write-Host "     Git inicializado" -ForegroundColor Green
} else {
    Write-Host "     Repo git existente detectado" -ForegroundColor Green
}

# Crear / actualizar .gitignore
@"
obj/
bin/
Addin/
.vs/
*.user
*.suo
*.pdb
node_modules/
.next/
next/
"@ | Set-Content "$ROOT\.gitignore" -Encoding UTF8

# Stage todo
git add -A

# Commit
$date = Get-Date -Format "yyyy-MM-dd HH:mm"
git commit -m "chore: commit completo desde disco externo F:\ [$date]"

# Push
git branch -M main
git push -u origin main --force

Write-Host "`n=====================================================" -ForegroundColor Green
Write-Host "  LISTO. Plugin instalado y repo actualizado." -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  DLL      : $REVIT_ADD\BOT.dll" -ForegroundColor White
Write-Host "  ADDIN    : $REVIT_ADD\RevitTutor.addin" -ForegroundColor White
Write-Host "  SPRITES  : $spriteDst ($count archivos)" -ForegroundColor White
Write-Host "  GITHUB   : $GITHUB" -ForegroundColor White
Write-Host ""
Write-Host "  -> Abre Revit 2025 para probar el plugin." -ForegroundColor Cyan
Write-Host "  -> Ve a: Vista > Paneles de usuario > Tutor IA" -ForegroundColor Cyan
