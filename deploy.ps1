# Script de despliegue automático para Gasbel B2B en DigitalOcean Droplet
# Ejecución: .\deploy.ps1

$ErrorActionPreference = "Stop"

$DropletIp = "68.183.126.230"
$SshKeyPath = Join-Path $env:USERPROFILE ".ssh\id_foyke_do"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Iniciando despliegue de Gasbel B2B en $DropletIp" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# 1. Asegurar que los cambios locales estén pusheados
Write-Host "Verificando cambios locales en Git..." -ForegroundColor Yellow
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Host "Hay cambios sin confirmar en Git. Haciendo commit..." -ForegroundColor Yellow
    git add .
    git commit -m "chore(auto-deploy): sync changes before deployment"
}
Write-Host "Pusheando a GitHub main..." -ForegroundColor Yellow
git push origin main

# 2. Conexión SSH y actualización del contenedor en el Droplet
Write-Host "Conectando al Droplet y actualizando contenedor..." -ForegroundColor Yellow
$remoteCommands = 'cd /root/Gasbel-B2B && git pull origin main && docker compose up -d --build && docker restart foyke-web'

ssh -i "$SshKeyPath" -o StrictHostKeyChecking=no root@$DropletIp $remoteCommands

Write-Host "=============================================" -ForegroundColor Green
Write-Host "Despliegue exitoso!" -ForegroundColor Green
Write-Host "Sitio en vivo: https://gasbel.com.ar" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
