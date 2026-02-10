#!/bin/bash

# Script para ejecutar ambos proyectos (API y Cliente)
# Uso: ./ejecutar.sh

echo "🚀 Iniciando ImpulsaDBA - Blazor WebAssembly con PWA"
echo ""

# Colores para output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Función para verificar si un puerto está en uso
check_port() {
    if lsof -Pi :$1 -sTCP:LISTEN -t >/dev/null 2>&1 ; then
        echo -e "${YELLOW}⚠️  Puerto $1 ya está en uso${NC}"
        return 1
    fi
    return 0
}

# Verificar puertos (HTTPS para PWA: API 7001, Cliente 7023)
echo "🔍 Verificando puertos..."
check_port 7001 && echo -e "${GREEN}✓${NC} Puerto 7001 (API HTTPS) disponible" || echo -e "${YELLOW}⚠️${NC} Puerto 7001 en uso"
check_port 7023 && echo -e "${GREEN}✓${NC} Puerto 7023 (Cliente HTTPS / PWA) disponible" || echo -e "${YELLOW}⚠️${NC} Puerto 7023 en uso"
echo ""

# Verificar que los proyectos existan
if [ ! -d "ImpulsaDBA.API" ]; then
    echo "❌ Error: No se encuentra el directorio ImpulsaDBA.API"
    exit 1
fi

if [ ! -d "ImpulsaDBA.Client" ]; then
    echo "❌ Error: No se encuentra el directorio ImpulsaDBA.Client"
    exit 1
fi

# Función para ejecutar el API (HTTPS para CORS y PWA)
run_api() {
    echo -e "${BLUE}📡 Iniciando API Backend (HTTPS)...${NC}"
    cd ImpulsaDBA.API
    dotnet run --launch-profile https &
    API_PID=$!
    cd ..
    echo -e "${GREEN}✓ API iniciado (PID: $API_PID)${NC}"
    echo -e "   URL: https://localhost:7001"
    echo -e "   Swagger: https://localhost:7001/swagger"
    echo ""
}

# Función para ejecutar el Cliente (HTTPS para PWA instalable)
run_client() {
    echo -e "${BLUE}🌐 Iniciando Cliente Blazor WebAssembly (PWA en HTTPS)...${NC}"
    sleep 3  # Esperar un poco para que el API inicie
    cd ImpulsaDBA.Client
    dotnet run --launch-profile https &
    CLIENT_PID=$!
    cd ..
    echo -e "${GREEN}✓ Cliente iniciado (PID: $CLIENT_PID)${NC}"
    echo -e "   URL: https://localhost:7023"
    echo ""
}

# Función para limpiar procesos al salir
cleanup() {
    echo ""
    echo -e "${YELLOW}🛑 Deteniendo procesos...${NC}"
    if [ ! -z "$API_PID" ]; then
        # Matar el proceso y sus hijos
        pkill -P $API_PID 2>/dev/null
        kill $API_PID 2>/dev/null
        echo -e "${GREEN}✓ API detenido${NC}"
    fi
    if [ ! -z "$CLIENT_PID" ]; then
        # Matar el proceso y sus hijos
        pkill -P $CLIENT_PID 2>/dev/null
        kill $CLIENT_PID 2>/dev/null
        echo -e "${GREEN}✓ Cliente detenido${NC}"
    fi
    # Limpiar procesos dotnet huérfanos en los puertos (HTTPS: 7001 API, 7023 Cliente)
    lsof -ti:7001 | xargs kill -9 2>/dev/null
    lsof -ti:7023 | xargs kill -9 2>/dev/null
    lsof -ti:5001 | xargs kill -9 2>/dev/null
    lsof -ti:5079 | xargs kill -9 2>/dev/null
    exit 0
}

# Capturar Ctrl+C
trap cleanup SIGINT SIGTERM

# Ejecutar proyectos
run_api
run_client

echo -e "${GREEN}✅ Ambos proyectos están ejecutándose${NC}"
echo ""
echo "📝 Para detener, presiona Ctrl+C"
echo ""
echo "🌐 Abre tu navegador en: https://localhost:7023 (PWA instalable)"
echo ""

# Esperar indefinidamente
wait
