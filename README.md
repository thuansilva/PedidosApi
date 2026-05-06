# PedidosApi

API de pedidos desenvolvida com **.NET 10 Minimal API** para demonstrar na prática os conceitos de **Trunk-Based Development (TBD)**, **Feature Flags** e **Rollout Gradual** usando o pacote `Microsoft.FeatureManagement`.
   
---

## Índice

- [Visão geral](#visão-geral)
- [Pré-requisitos](#pré-requisitos)
- [Instalação e execução](#instalação-e-execução)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Endpoints](#endpoints)
- [Feature Flags](#feature-flags)
- [Rollout gradual](#rollout-gradual)
- [Configuração por ambiente](#configuração-por-ambiente)
- [Adaptando para JWT](#adaptando-para-jwt)
- [Conceitos aplicados](#conceitos-aplicados)
- [CHANGELOG](#changelog)


---

## Visão geral

O projeto simula uma API de pedidos com uma feature de **desconto progressivo** protegida por feature flag. A feature pode ser:

- **Desligada** — comportamento padrão, sem desconto (seguro para produção)
- **Ligada para todos** — todos os usuários recebem desconto
- **Liberada gradualmente** — apenas uma porcentagem dos usuários recebe desconto, controlada por `userId` de forma determinística

Esse padrão permite que o time faça commits na branch `main` com código incompleto ou experimental sem impacto em produção — **a flag protege o usuário final**.

---

## Pré-requisitos

| Ferramenta | Versão mínima | Link |
|---|---|---|
| .NET SDK | 10.0 | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| VS Code | Qualquer | [code.visualstudio.com](https://code.visualstudio.com) |
| C# Dev Kit | Qualquer | Extensão do VS Code |

---

## Instalação e execução

```bash
# 1. Clonar ou extrair o projeto
cd PedidosApi

# 2. Restaurar dependências
dotnet restore

# 3. Rodar a API
dotnet run
```

A API sobe em `http://localhost:5000/swagger`.

---

## Estrutura do projeto

```
PedidosApi/
├── Program.cs                        # Registro de serviços, filtros e pipeline HTTP 
├── PedidosApi.csproj                 # Definição do projeto e dependências NuGet
├── appsettings.json                  # Configuração de produção — flags desligadas por padrão
├── appsettings.Development.json      # Configuração de desenvolvimento — flags ligadas para devs
├── CHANGELOG.md                      # Histórico de versões do projeto
├── Features/
│   ├── FeatureFlags.cs               # Constantes dos nomes das flags
│   ├── DescontoService.cs            # Lógica de negócio protegida pela feature flag
│   └── UserIdPercentageFilter.cs     # Filtro customizado de rollout gradual por userId
└── Endpoints/
    └── PedidoEndpoints.cs            # Definição dos endpoints da Minimal API
```

### Responsabilidade de cada arquivo

**`FeatureFlags.cs`** — centraliza os nomes das flags como constantes tipadas. Evita typos e garante que o compilador aponte todos os usos caso o nome mude.

**`DescontoService.cs`** — contém a lógica de negócio do desconto. Consulta o `IFeatureManager` para decidir qual caminho executar. Não sabe nada sobre porcentagem ou rollout — essa responsabilidade é do filtro.

**`UserIdPercentageFilter.cs`** — implementa o rollout gradual determinístico. Calcula um bucket de 0 a 99 a partir do hash do `userId` e compara com a porcentagem configurada no `appsettings.json`. O mesmo `userId` sempre cai no mesmo bucket.

**`PedidoEndpoints.cs`** — mapeia os três endpoints da API usando o padrão Minimal API do .NET 8.

---

## Endpoints

### `GET /pedidos`

Retorna a lista de pedidos de exemplo.

```bash
curl http://localhost:5000/pedidos
```

**Resposta:**
```json
[
  { "id": 1, "valor": 100.0, "clientePremium": false },
  { "id": 2, "valor": 250.0, "clientePremium": true  },
  { "id": 3, "valor": 80.0,  "clientePremium": false }
]
```

---

### `POST /pedidos/calcular-desconto`

Calcula o desconto de um pedido. O comportamento varia conforme o estado da feature flag e o `userId` informado no header.

**Headers:**

| Header | Obrigatório | Descrição |
|---|---|---|
| `Content-Type` | Sim | `application/json` |
| `X-User-Id` | Não | Identificador do usuário para o rollout gradual. Se ausente, usa `anonymous`. |

**Body:**
```json
{
  "valor": 200.00,
  "clientePremium": true
}
```

**Exemplo — cliente premium:**
```bash
curl -X POST http://localhost:5000/pedidos/calcular-desconto \
  -H "Content-Type: application/json" \
  -H "X-User-Id: user-042" \
  -d '{"valor": 200, "clientePremium": true}'
```

**Resposta com flag desligada:**
```json
{
  "valor": 200.00,
  "clientePremium": true,
  "desconto": 0.00,
  "total": 200.00,
  "featureAtiva": false
}
```

**Resposta com flag ligada — cliente premium (15% de desconto):**
```json
{
  "valor": 200.00,
  "clientePremium": true,
  "desconto": 30.00,
  "total": 170.00,
  "featureAtiva": true
}
```

**Resposta com flag ligada — cliente padrão (5% de desconto):**
```json
{
  "valor": 200.00,
  "clientePremium": false,
  "desconto": 10.00,
  "total": 190.00,
  "featureAtiva": true
}
```

**Regras de desconto:**

| Tipo de cliente | Flag ativa | Desconto |
|---|---|---|
| Premium | Sim | 15% sobre o valor |
| Padrão | Sim | 5% sobre o valor |
| Qualquer | Não | 0% — comportamento atual |

---

### `GET /feature-flags`

Retorna o status atual de todas as feature flags. Útil para debug e validação durante o rollout.

```bash
curl http://localhost:5000/feature-flags
```

**Resposta:**
```json
{
  "descontoPremium": false,
  "novoCalculoFrete": false
}
```

---

## Feature Flags

As flags são configuradas na seção `FeatureManagement` do `appsettings.json`. O projeto usa o pacote `Microsoft.FeatureManagement.AspNetCore`.

### Flags disponíveis

| Flag | Descrição | Status padrão |
|---|---|---|
| `DescontoPremium` | Habilita o cálculo de desconto progressivo por tipo de cliente | `false` |
| `NovoCalculoFrete` | Reservada para futura feature de cálculo de frete | `false` |

### Liga/desliga simples

```json
"FeatureManagement": {
  "DescontoPremium": true,
  "NovoCalculoFrete": false
}
```

### Usando o `PercentageFilter` nativo

Avalia por **requisição** — não determinístico. A mesma chamada pode retornar resultados diferentes para o mesmo usuário.

```json
"FeatureManagement": {
  "DescontoPremium": {
    "EnabledFor": [
      {
        "Name": "Percentage",
        "Parameters": { "Value": 20 }
      }
    ]
  }
}
```

> ⚠️ Use o `PercentageFilter` nativo apenas para testes de carga ou smoke tests. Para rollout de produto com experiência consistente por usuário, use o `UserIdPercentageFilter` descrito abaixo.

---

## Rollout gradual

O `UserIdPercentageFilter` é um filtro customizado que garante que o mesmo usuário sempre veja (ou não veja) a feature, independente de quantas requisições ele fizer.

### Como funciona

```
userId "user-042"
    → hash: Math.Abs("user-042".GetHashCode())
    → bucket: hash % 100  →  resultado: 7
    → Percentage configurado: 20
    → 7 < 20  →  true  →  usuário VÊ a feature

userId "user-001"
    → bucket: 23
    → 23 < 20  →  false  →  usuário NÃO vê a feature
```

O bucket de cada `userId` é fixo — não muda entre requisições nem entre deploys.

### Configuração no appsettings.json

```json
"FeatureManagement": {
  "DescontoPremium": {
    "EnabledFor": [
      {
        "Name": "UserIdPercentage",
        "Parameters": { "Percentage": 20 }
      }
    ]
  }
}
```

### Fases sugeridas de rollout

| Fase | Percentage | Duração | Objetivo | Critério para avançar |
|---|---|---|---|---|
| Validação interna | `5` | 1-2 dias | QA e beta testers | Zero erros 5xx novos |
| Canary release | `20` | 2-3 dias | Primeiros usuários reais | Métricas estáveis |
| Rollout ampliado | `50` | 1-2 dias | Metade da base | Latência p95 estável |
| Completo | `100` | — | Todos os usuários | Alguns dias estável |
| Cleanup | — | 1 dia | Remover código da flag | Feature 100% OK |

### Testando diferentes usuários

```bash
# user-042 — bucket 7 — VÊ a feature com Percentage: 20
curl -X POST http://localhost:5000/pedidos/calcular-desconto \
  -H "Content-Type: application/json" \
  -H "X-User-Id: user-042" \
  -d '{"valor": 300, "clientePremium": true}'

# user-001 — bucket 23 — NÃO vê a feature com Percentage: 20
curl -X POST http://localhost:5000/pedidos/calcular-desconto \
  -H "Content-Type: application/json" \
  -H "X-User-Id: user-001" \
  -d '{"valor": 300, "clientePremium": true}'

# Repetir com user-042 — sempre o mesmo resultado (determinístico)
```

### Simulando rollback

Para reverter instantaneamente sem novo deploy, basta alterar o `Percentage` para `0` e reiniciar:

```json
"Parameters": { "Percentage": 0 }
```

---

## Configuração por ambiente

O .NET carrega automaticamente o arquivo correto com base na variável `ASPNETCORE_ENVIRONMENT`.

| Arquivo | Ambiente | Comportamento |
|---|---|---|
| `appsettings.json` | Produção | Todas as flags `false` por padrão |
| `appsettings.Development.json` | Development | `DescontoPremium: true` para facilitar o desenvolvimento |

Para forçar um ambiente manualmente:

```bash
# Linux / Mac
ASPNETCORE_ENVIRONMENT=Production dotnet run

# Windows PowerShell
$env:ASPNETCORE_ENVIRONMENT="Production"; dotnet run
```

---

## Adaptando para JWT

O `UserIdPercentageFilter` lê o `userId` do header `X-User-Id` por simplicidade. Em produção, substitua pela leitura do claim do token JWT:

```csharp
// Em Features/UserIdPercentageFilter.cs
// Substituir:
var userId = http.HttpContext?
    .Request.Headers["X-User-Id"]
    .FirstOrDefault()
    ?? "anonymous";

// Por:
var userId = http.HttpContext?
    .User.FindFirst("sub")?.Value   // claim "sub" padrão OAuth2/OIDC
    ?? "anonymous";
```

---

## Conceitos aplicados

| Conceito | Descrição | Onde está no código |
|---|---|---|
| **Trunk-Based Development** | Integração contínua na branch `main` sem branches de vida longa | Estratégia de branching do projeto |
| **Feature Flag simples** | Liga/desliga uma funcionalidade sem novo deploy | `appsettings.json` + `DescontoService.cs` |
| **Deploy ≠ Release** | O código existe em produção mas está inativo enquanto a flag está `false` | `DescontoService.cs` — caminho `return 0` |
| **Rollout gradual** | Liberar para porcentagem crescente de usuários de forma controlada | `UserIdPercentageFilter.cs` |
| **Determinismo por userId** | Mesmo usuário sempre cai no mesmo grupo — experiência consistente | Hash `% 100` em `UserIdPercentageFilter.cs` |
| **Rollback instantâneo** | Reverter a feature sem reverter commit ou fazer deploy | Alterar `Percentage: 0` no `appsettings.json` |

---

## CHANGELOG

Consulte o arquivo [CHANGELOG.md](./CHANGELOG.md) para o histórico completo de versões.
