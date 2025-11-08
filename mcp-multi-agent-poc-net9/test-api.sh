#!/bin/bash
# Test script for MCP Multi-Agent POC Web API

echo "=== MCP Multi-Agent POC - API Test Script ==="
echo ""

# Check if API is running
if ! curl -s http://localhost:5555/api/health > /dev/null 2>&1; then
    echo "Starting web API..."
    cd "$(dirname "$0")"
    dotnet run --project src/McpMultiAgent.WebApi/McpMultiAgent.WebApi.csproj --urls "http://localhost:5555" &
    API_PID=$!
    echo "Waiting for API to start..."
    sleep 10
else
    echo "API already running"
fi

echo ""
echo "Test 1: Health Check"
echo "===================="
curl -s http://localhost:5555/api/health | jq .

echo ""
echo "Test 2: Search Agent - DuckDuckGo Search"
echo "========================================="
curl -s -X POST http://localhost:5555/api/search \
  -H "Content-Type: application/json" \
  -d '{"query":"What is Microsoft Agent Framework?"}' | jq .

echo ""
echo "Test 3: Database Agent (will fail without SQL Server)"
echo "======================================================"
curl -s -X POST http://localhost:5555/api/database \
  -H "Content-Type: application/json" \
  -d '{"request":"List all databases"}' | jq .

echo ""
echo "Test 4: Multi-Agent Coordinator"
echo "================================"
curl -s -X POST http://localhost:5555/api/agent \
  -H "Content-Type: application/json" \
  -d '{"request":"Find information about .NET 9"}' | jq .

echo ""
echo "=== All Tests Complete ==="

# Cleanup if we started the API
if [ ! -z "$API_PID" ]; then
    echo "Stopping web API..."
    kill $API_PID 2>/dev/null || true
fi
