# Plano de Implementação: Modo LAN e Hospedagem Externa (Render)

Este plano descreve as alterações necessárias para permitir que o jogo funcione em redes restritas (como escolas) através de um Modo LAN (IP Direto) e prepara o servidor Node.js para hospedagem externa.

## Project Overview
- **Título**: Fauneral 2
- **Conceito**: Jogo multiplayer com sistema de matchmaking via PIN e Relay.
- **Objetivo**: Adicionar suporte a conexões locais (LAN) e hospedar o servidor de descoberta.

## Key Asset & Context
- `MatchmakingController.cs`: Gerencia os modos de conexão.
- `DiscoveryManager.cs`: Comunicação com o servidor Node.js.
- `CreateRoomUI.cs` / `JoinRoomUI.cs`: Interface de usuário para os modos de conexão.
- `app.js` / `request-handlers.js`: Servidor Node.js.

## Implementation Steps

### Passo 1: Preparação do Servidor Node.js para Hospedagem
- **Ação**: Atualizar o servidor para usar a porta do ambiente (`process.env.PORT`) e adicionar comentários.
- **Arquivo**: `Assets/unityWebRequest/app.js` e `request-handlers.js`
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Passo 2: Adicionar Métodos LAN no MatchmakingController
- **Ação**: Implementar `StartHostLAN` e `StartClientLAN` que ignoram o Unity Relay e conectam diretamente via IP.
- **Arquivo**: `Assets/Scripts/Networking/MatchmakingController.cs`
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Passo 3: Atualizar DiscoveryManager para URL Configurável
- **Ação**: Garantir que a URL do servidor Node.js possa ser trocada facilmente no Inspector (Local vs Render).
- **Arquivo**: `Assets/Scripts/Networking/DiscoveryManager.cs`
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### Passo 4: Atualizar UI para Suporte ao Modo LAN
- **Ação**: Adicionar botões ou campos para ativar o modo LAN nas telas de criar e entrar na sala.
- **Arquivo**: `Assets/Scripts/GameScripts/CreateRoomUI.cs`, `JoinRoomUI.cs` e prefabs de cena.
- **Assigned role**: developer
- **Dependencies**: Passo 2
- **Parallelizable**: No

## Verification & Testing
1. **Teste Local**: Verificar se o jogo ainda funciona com `127.0.0.1`.
2. **Teste LAN**: Abrir duas instâncias no mesmo PC e conectar via `127.0.0.1` usando o novo Modo LAN.
3. **Teste Render**: Após o deploy, mudar a URL no `DiscoveryManager` e testar a busca de salas.
