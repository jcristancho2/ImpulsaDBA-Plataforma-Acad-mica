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

# Verificar puertos
echo "🔍 Verificando puertos..."
check_port 7001 && echo -e "${GREEN}✓${NC} Puerto 7001 (API HTTPS) disponible" || echo -e "${YELLOW}⚠️${NC} Puerto 7001 en uso"
check_port 5001 && echo -e "${GREEN}✓${NC} Puerto 5001 (API HTTP) disponible" || echo -e "${YELLOW}⚠️${NC} Puerto 5001 en uso"
check_port 7023 && echo -e "${GREEN}✓${NC} Puerto 7023 (Cliente HTTPS) disponible" || echo -e "${YELLOW}⚠️${NC} Puerto 7023 en uso"
check_port 5079 && echo -e "${GREEN}✓${NC} Puerto 5079 (Cliente HTTP) disponible" || echo -e "${YELLOW}⚠️${NC} Puerto 5079 en uso"
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

# Función para ejecutar el API
run_api() {
    echo -e "${BLUE}📡 Iniciando API Backend...${NC}"
    cd ImpulsaDBA.API
    dotnet run &
    API_PID=$!
    cd ..
    echo -e "${GREEN}✓ API iniciado (PID: $API_PID)${NC}"
    echo -e "   URL: https://localhost:7001"
    echo -e "   Swagger: https://localhost:7001/swagger"
    echo ""
}

# Función para ejecutar el Cliente
run_client() {
    echo -e "${BLUE}🌐 Iniciando Cliente Blazor WebAssembly...${NC}"
    sleep 3  # Esperar un poco para que el API inicie
    cd ImpulsaDBA.Client
    dotnet run &
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
        kill $API_PID 2>/dev/null
        echo -e "${GREEN}✓ API detenido${NC}"
    fi
    if [ ! -z "$CLIENT_PID" ]; then
        kill $CLIENT_PID 2>/dev/null
        echo -e "${GREEN}✓ Cliente detenido${NC}"
    fi
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
echo "🌐 Abre tu navegador en: https://localhost:7023"
echo ""

# Esperar indefinidamente
wait
