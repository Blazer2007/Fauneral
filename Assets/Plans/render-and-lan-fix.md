# Plano de Implementação: Suporte para Render e Modo LAN

Este plano detalha as atualizações nos scripts de rede para utilizar o servidor hospedado no Render e adicionar um "Plano B" (Modo LAN) para contornar bloqueios de rede em ambientes como escolas.

## Project Overview
- **Título**: Fauneral 2
- **Servidor Externo**: `https://node-server-4eg2.onrender.com`
- **Objetivo**: Garantir que o matchmaking funcione via internet e via rede local (LAN).

## Key Assets & Context
- `DiscoveryManager.cs`: Gerencia chamadas HTTP para o servidor Node.js.
- `MatchmakingController.cs`: Coordena o início de sessões Host/Client.
- `LobbyServerManager.cs`: Sincroniza dados da sala entre os jogadores.

## Implementation Steps

### Passo 1: Atualizar DiscoveryManager com URL do Render e Comentários
- **Descrição**: Alterar a URL padrão para o endereço do Render e adicionar explicações em português.
- **Arquivo**: `Assets/Scripts/Networking/DiscoveryManager.cs`
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Passo 2: Implementar Lógica LAN no MatchmakingController
- **Descrição**: Adicionar métodos `StartHostLAN` e `StartClientLAN` que utilizam o IP local em vez do Unity Relay.
- **Arquivo**: `Assets/Scripts/Networking/MatchmakingController.cs`
- **Assigned role**: developer
- **Dependencies**: Passo 1
- **Parallelizable**: Yes

### Passo 3: Adicionar Opção de LAN na Interface de Criação
- **Descrição**: Modificar o `CreateRoomUI` para permitir que o usuário escolha entre o modo "Online" (Relay) e "LAN" (IP Direto).
- **Arquivo**: `Assets/Scripts/GameScripts/CreateRoomUI.cs`
- **Assigned role**: developer
- **Dependencies**: Passo 2
- **Parallelizable**: No

### Passo 4: Sincronização e Comentários Finais
- **Descrição**: Revisar todos os scripts modificados, garantindo que as linhas criadas tenham comentários explicativos conforme solicitado.
- **Assigned role**: developer
- **Dependencies**: Todos os anteriores
- **Parallelizable**: No

## Verification & Testing
1. **Teste Online**: Criar uma sala no modo Online e verificar se o PIN aparece no catálogo global (`/public-rooms` do Render).
2. **Teste LAN**: Criar uma sala no modo LAN e verificar se outro PC na mesma rede consegue conectar usando o IP local registrado no servidor.
3. **Teste de Persistência**: Verificar se o `LobbyServerManager` não é destruído ao mudar de cena.
