# Plano: hospedar Formarca na Hostinger (back, front, DB) + CI/CD Git

Atualizado em 2026-09-15.

Este documento é o runbook de deploy. A decisão de produto é **VPS Hostinger com Docker + GitHub Actions**. Hosting compartilhado, Cloud e o Git/Node.js do hPanel **não** servem o stack atual.

---

## 1. Objetivo

Subir em produção, na Hostinger:

| Peça | Projeto | Runtime hoje |
|---|---|---|
| API | `src/Formarca.Web` | ASP.NET Core **.NET 10**, porta local `5000` |
| Front | `frontend/formarca-web` | React + Vite + Ant Design |
| Banco | `docker-compose.yml` → `mcr.microsoft.com/mssql/server:2022-latest` | **SQL Server** |
| Jobs | `src/Formarca.BackgroundRunner` | worker .NET (limpa refresh tokens) |
| Schema | `src/Formarca.DbMigrator` | `Database.Migrate()` + seed |

Tudo no **mesmo VPS**, atrás de um reverse proxy, com deploy automático no merge em `main`.

---

## 2. Por que não usar Websites / Node.js / Git do hPanel

A Hostinger tem **três** integrações Git. Só a do VPS cobre o Formarca.

| Integração | O que faz | Serve? |
|---|---|---|
| [Git genérico](https://docs.hostinger.com/websites/git) (Advanced → Git) | Push copia arquivos. **Sem build.** | Não. Front precisa de `npm run build`; API precisa de `dotnet`. |
| [Node.js + GitHub](https://docs.hostinger.com/node.js/github) | `npm install` → build → sobe processo Node. Vite/React está na [lista de frameworks](https://docs.hostinger.com/node.js/creating-an-app). | Só o **SPA**. Sem .NET, sem SQL Server. Processo Node **dorme sem tráfego** — quebra webhook Autentique e jobs. Banco do hPanel é só [MySQL](https://docs.hostinger.com/websites/databases). |
| [VPS Docker Manager](https://docs.hostinger.com/api-reference/endpoints/vps) + [Action](https://github.com/hostinger/deploy-on-vps) | Compose no VPS, pull/rebuild, restart | **Sim.** |

Migrar EF de SQL Server → MySQL (hosting compartilhado) está **fora de escopo**. Seria outro projeto.

Docs oficiais usadas neste plano:

- [Hostinger Docs](https://docs.hostinger.com/)
- [Node.js overview](https://docs.hostinger.com/node.js/overview) (para saber o que **não** usar)
- [Git genérico](https://docs.hostinger.com/websites/git)
- [GitHub Node.js](https://docs.hostinger.com/node.js/github)
- [Databases MySQL](https://docs.hostinger.com/websites/databases)
- [VPS API / Docker Manager](https://docs.hostinger.com/api-reference/endpoints/vps)
- [Deploy VPS via GitHub Actions](https://www.hostinger.com/support/deploy-to-hostinger-vps-using-github-actions/)
- [Action `hostinger/deploy-on-vps`](https://github.com/hostinger/deploy-on-vps)
- [Repo privado no Docker Manager](https://www.hostinger.com/support/how-to-deploy-from-private-github-repository-on-hostinger-docker-manager/)

---

## 3. Arquitetura alvo

```
                    Internet (443)
                         │
              ┌──────────▼──────────┐
              │  Caddy (ou nginx)   │  TLS Let's Encrypt
              │  painel.dominio.com │  (site atual fica no apex)
              └───┬────────────┬────┘
                  │            │
             /    │            │  /api  /api/webhooks
                  │            │
          ┌───────▼───┐   ┌────▼─────────┐
          │ nginx     │   │ Formarca.Web │
          │ (SPA dist)│   │ ASP.NET 10   │
          └───────────┘   └──────┬───────┘
                                 │
                    ┌────────────┼────────────┐
                    │            │            │
              ┌─────▼─────┐ ┌────▼─────┐ ┌────▼──────────────┐
              │ SQL Server│ │ DbMigrator│ │ BackgroundRunner │
              │ Express   │ │ one-shot  │ │ hosted service   │
              │ volume    │ │ migrate+  │ │ refresh cleanup  │
              │ persistente│ │ seed      │ └──────────────────┘
              └───────────┘ └──────────┘
```

**Um domínio, same-origin.** O front chama `/api/...` (ver §7). CORS de produção deixa de ser necessário se o proxy unificar host.

Portas internas (não publicar na internet):

| Serviço | Porta interna | Publicar? |
|---|---|---|
| Caddy | 80, 443 | sim |
| API | 8080 | não |
| Front nginx | 80 (rede docker) | não |
| SQL Server | 1433 | **não** (só rede docker) |
| BackgroundRunner | — | não |

Health check da API já existe: `GET /api/health` e `GET /api/version` (`HealthController`).

Webhook Autentique (público, anônimo): `POST /api/webhooks/autentique` com header `X-Autentique-Signature`.

---

## 4. Hardware e plano Hostinger

SQL Server Linux + .NET + Node build (se buildar no VPS) comem RAM.

| Plano VPS (referência) | RAM | Uso |
|---|---|---|
| Evitar | 2 GB | SQL Server nem sobe direito |
| Mínimo apertado | 4 GB | só com **SQL Express** e sem build no VPS |
| **Recomendado** | **8 GB** | Express + API + front + worker + headroom |
| Folga | 16 GB | se o volume de PDF/contratos crescer |

Outros requisitos:

- Template **Docker** (ou Ubuntu + Docker Compose instalado).
- Datacenter perto do público (Brasil se o painel for BR).
- Snapshots/backups do VPS ligados no hPanel.
- Firewall Hostinger: **22** (SSH, idealmente só IP do escritório), **80**, **443**. Nada de 1433, 8080, 5173.

SQL Server:

- A imagem local `2022-latest` é **Developer** — não licenciada para produção.
- Produção: `MSSQL_PID=Express` (grátis, teto de memória ~1,4 GB) **ou** licença paga.
- Senha SA forte, diferente da do `docker-compose.yml` local (`Your_password123`).
- Volume nomeado (`formarca-sql-data`) para sobreviver a redeploy.

---

## 5. DNS e TLS — mesmo domínio do site atual

O domínio **já na Hostinger** (site institucional no hPanel) é o que vamos usar. Não precisa comprar outro.

Um hostname (`formarca.com` ou `www`) só aponta para **um IP**. O site atual vive no IP do hosting compartilhado; o painel vai viver no IP do **VPS**. Por isso o apex continua no site, e o Formarca entra num **subdomínio**.

| Host | Para onde aponta | O que serve |
|---|---|---|
| `dominio.com` e `www` | **não mexer** (A/CNAME atuais do site) | site que já está no ar |
| `painel.dominio.com` | registro **A** → IP do VPS | SPA + API (`/api`) + webhook |
| MX / e-mail | **não mexer** | caixas Hostinger, se existirem |

Nameservers do domínio **continuam** na Hostinger. Só adicionar um A record em DNS ([Domain pointing](https://docs.hostinger.com/domains/domain-pointing) / [DNS Management](https://docs.hostinger.com/domains/dns-management)):

```
painel    A    <IP-do-VPS>
```

Opcional: `www.painel` não precisa. SSL do site atual segue no hPanel. No VPS o Caddy emite Let's Encrypt **só** para `painel.dominio.com` (porta 80/443 abertas + A já propagado).

Same-origin continua valendo: o browser fala com `https://painel.dominio.com` e `https://painel.dominio.com/api/...`. Não criar `api.dominio.com` no começo (CORS, segundo certificado, segundo deploy).

Autentique: webhook `https://painel.dominio.com/api/webhooks/autentique`.

**Não fazer:** apontar o apex/`www` para o VPS. O site atual some desse endereço. Só faria sentido se o institucional for desligado ou migrado para o VPS também.

Nome do subdomínio é livre (`painel`, `app`, `cliente`). Preferência deste plano: **`painel`**.

---

## 6. O que o repositório ainda não tem

Hoje só existe `docker-compose.yml` com o SQL de desenvolvimento. Falta:

| Artefato | Função |
|---|---|
| `src/Formarca.Web/Dockerfile` | API multi-stage (`sdk` → `aspnet`) |
| `src/Formarca.BackgroundRunner/Dockerfile` | worker |
| `src/Formarca.DbMigrator/Dockerfile` | one-shot migrate + seed |
| `frontend/formarca-web/Dockerfile` | `npm run build` → nginx |
| `frontend/formarca-web/nginx.conf` | SPA `try_files` + proxy `/api` **ou** proxy só no Caddy |
| `docker/Caddyfile` | TLS + rotas `/` e `/api` |
| `docker-compose.prod.yml` | stack de produção (imagens, volumes, healthchecks, env) |
| `.dockerignore` / `frontend/formarca-web/.dockerignore` | não mandar `bin/`, `node_modules/`, `.vs/` |
| `.github/workflows/ci.yml` | PR: `dotnet test` + `npm ci` + `npm run build` |
| `.github/workflows/deploy.yml` | `main`: CI + `hostinger/deploy-on-vps@v2` |
| `.github/dependabot.yml` (opcional) | patches de Actions e NuGet/npm |

Ajustes de **código** necessários antes do primeiro deploy (não são Docker, são comportamento de produção):

1. **CORS** — `Program.cs` só libera `http://localhost:5173` e **só em Development**. Com same-origin via Caddy, produção não precisa de CORS. Se o front ficar em outro host, ler `Cors:Origins` da config.
2. **HTTPS atrás de proxy** — habilitar `UseForwardedHeaders` (`X-Forwarded-For`, `X-Forwarded-Proto`) senão cookies/redirects quebram atrás do Caddy. `UseHttpsRedirection` no Kestrel interno (HTTP 8080) deve respeitar o proto encaminhado, ou ser desligado internamente.
3. **Front** — `VITE_MOCK_AUTH=false`. `VITE_API_URL` vazio (same-origin: `fetch('/api/...')`). `env.ts` já faz `${apiUrl}/api/...`; string vazia vira `/api/...`.
4. **JWT** — `Jwt:Key` com pelo menos 32 bytes aleatórios. Não reutilizar o de Development.
5. **Seed** — `DatabaseSeeder` cria `admin@formarca.com` / `Formarca@Admin123!`. Trocar a senha **no primeiro login** (ou tornar a senha inicial configurável por env e obrigar rotação).
6. **`ASPNETCORE_ENVIRONMENT=Production`**.
7. **Arquivos locais** — `appsettings*.json` já estão no `.gitignore`. Produção = só variáveis de ambiente (ver §8).
8. **`Documents:FormarcaSignaturePath`** — se a assinatura da Formarca for um arquivo no disco, montar volume ou embutir no image. PDFs de contrato já vão para o **banco** (`ContractFile`), então não precisam de disco extra.

---

## 7. Roteamento front ↔ API

O cliente (`frontend/formarca-web/src/shared/api/client.ts`) chama `${env.apiUrl}/api/...`.

Produção recomendada:

```
VITE_API_URL=          # vazio
VITE_MOCK_AUTH=false
```

Caddy (exemplo):

```
painel.dominio.com {
  encode gzip
  reverse_proxy /api* api:8080
  reverse_proxy /* front:80
}
```

nginx do front precisa de fallback SPA (React Router: `/login`, `/register`, `/marcas/:id`, etc.):

```
location / {
  try_files $uri $uri/ /index.html;
}
```

`VITE_*` é bake **no build**. Mudou a URL da API? Rebuild da imagem do front. Por isso same-origin (URL vazia) é o caminho mais estável.

---

## 8. Variáveis de ambiente (produção)

ASP.NET Core mapeia `__` → seção JSON. Ex.: `Jwt__Key` = `Jwt:Key`.

### Obrigatórias — API, Migrator, BackgroundRunner

| Variável | Origem | Notas |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | SQL no compose | Host interno `sqlserver,1433`. `TrustServerCertificate=True` (TLS interno). User `sa` ou login dedicado. |
| `Jwt__Issuer` | `appsettings.json.example` | `Formarca` |
| `Jwt__Audience` | idem | `Formarca` |
| `Jwt__Key` | **secret** | 32+ bytes, aleatório |
| `Jwt__AccessTokenMinutes` | 30 | |
| `Jwt__RefreshTokenDays` | 14 | |
| `Autentique__ApiKey` | **secret** | |
| `Autentique__OrganizationId` | opcional | |
| `Autentique__Sandbox` | `false` em prod | |
| `Autentique__WebhookSecret` | **secret** | HMAC do `X-Autentique-Signature` |
| `Autentique__FormarcaSignerEmail` | e-mail da Formarca no Autentique | API falha emitir contrato se vazio |
| `Documents__FormarcaSignaturePath` | opcional | caminho do PNG/JPG da assinatura |
| `ASPNETCORE_ENVIRONMENT` | `Production` | |
| `ASPNETCORE_URLS` | `http://+:8080` | só a API |

### SQL

| Variável | Notas |
|---|---|
| `ACCEPT_EULA` | `Y` |
| `MSSQL_SA_PASSWORD` | **secret**, complexidade MS |
| `MSSQL_PID` | `Express` |

### Front (build-args)

| Variável | Valor prod |
|---|---|
| `VITE_API_URL` | vazio |
| `VITE_MOCK_AUTH` | `false` |

### GitHub (Actions)

| Nome | Tipo | Uso |
|---|---|---|
| `HOSTINGER_API_KEY` | secret | hPanel → API |
| `HOSTINGER_VM_ID` | variable | número do hostname `srvNNNNNN.hstgr.cloud` |
| `MSSQL_SA_PASSWORD` | secret | injetado no compose |
| `JWT_KEY` | secret | |
| `AUTENTIQUE_API_KEY` | secret | |
| `AUTENTIQUE_WEBHOOK_SECRET` | secret | |
| `AUTENTIQUE_SIGNER_EMAIL` | secret/var | |
| `GHCR_TOKEN` | (se Fase 2) | push de imagens |

Nunca commitar `.env` de produção, `appsettings.json` preenchido, nem a API key da Hostinger.

---

## 9. Compose de produção (esboço)

Arquivo alvo: `docker-compose.prod.yml`. O Action da Hostinger manda esse arquivo (no SHA do commit) para o Docker Manager do VPS.

```yaml
# Esboço — implementar na fase de Dockerfiles.
name: formarca

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD}
      MSSQL_PID: Express
    volumes:
      - formarca-sql-data:/var/opt/mssql
    # sem ports: para o mundo
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 5s
      retries: 12
      start_period: 40s
    restart: unless-stopped

  migrator:
    build:
      context: .
      dockerfile: src/Formarca.DbMigrator/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: ${DB_CONNECTION}
      ASPNETCORE_ENVIRONMENT: Production
    depends_on:
      sqlserver:
        condition: service_healthy
    restart: "no"

  api:
    build:
      context: .
      dockerfile: src/Formarca.Web/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: ${DB_CONNECTION}
      Jwt__Key: ${JWT_KEY}
      Jwt__Issuer: Formarca
      Jwt__Audience: Formarca
      Autentique__ApiKey: ${AUTENTIQUE_API_KEY}
      Autentique__WebhookSecret: ${AUTENTIQUE_WEBHOOK_SECRET}
      Autentique__Sandbox: "false"
      Autentique__FormarcaSignerEmail: ${AUTENTIQUE_SIGNER_EMAIL}
    depends_on:
      sqlserver:
        condition: service_healthy
      migrator:
        condition: service_completed_successfully
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/api/health"]
      interval: 15s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  worker:
    build:
      context: .
      dockerfile: src/Formarca.BackgroundRunner/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: ${DB_CONNECTION}
      Jwt__Key: ${JWT_KEY}
    depends_on:
      sqlserver:
        condition: service_healthy
      migrator:
        condition: service_completed_successfully
    restart: unless-stopped

  front:
    build:
      context: ./frontend/formarca-web
      dockerfile: Dockerfile
      args:
        VITE_API_URL: ""
        VITE_MOCK_AUTH: "false"
    restart: unless-stopped

  caddy:
    image: caddy:2
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./docker/Caddyfile:/etc/caddy/Caddyfile:ro
      - caddy-data:/data
      - caddy-config:/config
    depends_on:
      - api
      - front
    restart: unless-stopped

volumes:
  formarca-sql-data:
  caddy-data:
  caddy-config:
```

`docker-compose.yml` atual **permanece** só para SQL local. Não misturar senhas/PID de prod nele.

Dependência `migrator` → `service_completed_successfully`: cada deploy reaplica migrations (idempotente) e re-roda o seeder (hoje o seeder é seguro para reexecução — permissões/admin só se não existirem). Confirmar isso antes de ir ao ar se o seeder ganhar dados destrutivos.

---

## 10. CI/CD no GitHub

### Estratégia de branch

| Branch | Papel |
|---|---|
| feature/* | trabalho |
| `main` | protegida; PR obrigatório; **produção** |

Não ligar auto-deploy em todo push de feature. Preview = segundo compose/VPS (caro) ou simplesmente não ter staging no começo.

Proteções sugeridas em `main`: require PR, require `ci` verde, sem push direto.

### Job CI (todo PR e todo push)

```
dotnet test Formarca.sln
cd frontend/formarca-web && npm ci && npm run lint && npm run build
```

SDK: .NET **10.x**, Node **22** (LTS, alinhado ao que a Hostinger Node.js ofereceria; no VPS só importa a imagem de build).

### Job deploy (só `main` depois do CI)

Duas fases. Começar na 1.

**Fase 1 — build no VPS (simples)**

1. Action `hostinger/deploy-on-vps@v2` envia o `docker-compose.prod.yml` do commit.
2. Docker Manager clona o repo (repo privado: [SSH deploy key](https://www.hostinger.com/support/how-to-deploy-from-private-github-repository-on-hostinger-docker-manager/)).
3. `docker compose build && up -d`.
4. Volume do SQL permanece.

Contras: primeiro build no VPS é pesado (SDK .NET 10 + npm). 8 GB ajuda.

**Fase 2 — imagens no GHCR (melhor)**

1. CI faz `docker build` + `docker push ghcr.io/<org>/formarca-{api,front,worker,migrator}:$SHA` e `:latest`.
2. Compose de prod usa só `image:`, sem `build:`.
3. Action dispara o Docker Manager, que dá pull e recria.

O Action oficial (`v2`) **não** usa mais Personal Access Token; repo privado = deploy key SSH.

Esboço:

```yaml
# .github/workflows/deploy.yml
name: CI and deploy

on:
  pull_request:
  push:
    branches: [main]

jobs:
  ci:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - run: dotnet test Formarca.sln --configuration Release
      - uses: actions/setup-node@v4
        with:
          node-version: "22"
          cache: npm
          cache-dependency-path: frontend/formarca-web/package-lock.json
      - run: npm ci
        working-directory: frontend/formarca-web
      - run: npm run lint
        working-directory: frontend/formarca-web
      - run: npm run build
        working-directory: frontend/formarca-web
        env:
          VITE_API_URL: ""
          VITE_MOCK_AUTH: "false"

  deploy:
    needs: ci
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - name: Deploy to Hostinger VPS
        uses: hostinger/deploy-on-vps@v2
        with:
          api-key: ${{ secrets.HOSTINGER_API_KEY }}
          virtual-machine: ${{ vars.HOSTINGER_VM_ID }}
          project-name: formarca
          docker-compose-path: docker-compose.prod.yml
          environment-variables: |
            MSSQL_SA_PASSWORD=${{ secrets.MSSQL_SA_PASSWORD }}
            DB_CONNECTION=${{ secrets.DB_CONNECTION }}
            JWT_KEY=${{ secrets.JWT_KEY }}
            AUTENTIQUE_API_KEY=${{ secrets.AUTENTIQUE_API_KEY }}
            AUTENTIQUE_WEBHOOK_SECRET=${{ secrets.AUTENTIQUE_WEBHOOK_SECRET }}
            AUTENTIQUE_SIGNER_EMAIL=${{ secrets.AUTENTIQUE_SIGNER_EMAIL }}
```

A action `v1` (`hostinger/deploy-action`) está **obsoleta**. Usar `@v2`.

### O que o Git da Hostinger **não** faz neste plano

- Não conectar o repo em Websites → Git.
- Não criar Web App Node.js para o Vite (a não ser um plano B do front, §14).
- Não usar MySQL do hPanel.

---

## 11. Segurança

- SSH por chave; desligar senha se possível. Firewall 22 restrito.
- Não mapear `1433:1433` em produção.
- JWT e senha SA só em secrets.
- Trocar senha do seed no primeiro acesso.
- Autentique em `Sandbox=false` só com chave de produção e webhook HTTPS.
- `AllowedHosts` pode ficar `*` atrás do Caddy, ou o hostname real.
- Headers do Caddy: `X-Forwarded-Proto`, `X-Forwarded-For`.
- Atualizar imagens base (`aspnet:10.0`, `nginx:alpine`, `caddy:2`, SQL) via CI periódico.
- Logs: stdout (Serilog já escreve console). No VPS: `docker compose logs -f api`. Não commitar logs.
- Rate limit / WAF: opcional no Caddy depois; não bloquear o primeiro go-live.

---

## 12. Backup e rollback

| Camada | Como |
|---|---|
| Disco do VPS | Snapshot Hostinger antes do 1º deploy e antes de migration arriscada |
| Banco | Job periódico `BACKUP DATABASE` para volume ou `*.bak` baixado; volume Docker `formarca-sql-data` **não** apagar no `compose down` sem `-v` |
| App | Redeploy do SHA anterior (re-run do workflow num commit antigo, ou `docker compose` com tag anterior na Fase 2) |
| Front | imutável na imagem; rollback = imagem anterior |

`docker compose down -v` **apaga o banco**. Documentar isso no runbook operacional. Preferir `up -d --build` / recreate de serviços **exceto** o volume.

Rollback de migration EF: não é automático. Ter os `.sql` em `src/Formarca.DbMigrator/scripts/migrations/` como referência humana. Em emergência, restore do `.bak`.

---

## 13. Observabilidade mínima

Depois do deploy:

```bash
curl -I https://painel.dominio.com/
curl https://painel.dominio.com/api/health
curl https://painel.dominio.com/api/version
# site institucional continua no apex:
curl -I https://dominio.com/
```

- Caddy access log + API Serilog.
- Docker Manager no hPanel: status, logs agregados ([Get project logs](https://docs.hostinger.com/api-reference/endpoints/vps)).
- Alerta manual no começo: se `/api/health` cair, o Caddy ainda pode servir o SPA “morto” (tela de login que falha no POST). Monitorar os dois.

Timezone: containers UTC. O front já usa `dayjs`/locale; datas da API em UTC.

---

## 14. Plano B (não recomendado): front no Node.js Hosting

Só se o VPS ficar só para API+SQL e quiser o SPA no [Node.js Web App](https://docs.hostinger.com/node.js/creating-an-app):

- Root directory: `frontend/formarca-web`
- Framework: Vite / React
- Node 22, `npm run build`, output `dist`
- Env: `VITE_API_URL=https://painel.dominio.com`, `VITE_MOCK_AUTH=false` (só neste plano B; no plano principal a URL é vazia / same-origin)
- CORS na API para a origem do SPA
- Dois deploys, dois certificados, webhook continua no VPS

Útil como fallback barato do **front**. Não resolve API nem SQL Server. **Não é o plano principal.**

---

## 15. Hostinger Connector (Cursor)

A Hostinger tem [Connector MCP](https://docs.hostinger.com/) para o editor. Pode ajudar a ver VPS/DNS depois que a conta existir. **Não substitui** Dockerfiles, compose nem o workflow. CI oficial continua sendo GitHub Actions + `deploy-on-vps@v2`.

---

## 16. Ordem de execução

Checklist. Não pular.

### A. Conta e DNS

- [ ] VPS Docker, **8 GB**, snapshot ligado
- [ ] SSH por chave
- [ ] Firewall 22/80/443
- [ ] **Não** alterar A/CNAME/MX do site atual (`dominio.com` / `www`)
- [ ] A record `painel` → IP do VPS
- [ ] Propagação: `dig painel.dominio.com` resolve o VPS
- [ ] Caddyfile com `painel.dominio.com` (Let's Encrypt nesse host)
- [ ] Token API Hostinger gerado (guardar no GitHub, não no repo)
- [ ] Anotar `HOSTINGER_VM_ID`

### B. Repo

- [ ] Dockerfiles (API, worker, migrator, front)
- [ ] `nginx.conf` SPA + `Caddyfile`
- [ ] `docker-compose.prod.yml` + `.dockerignore`
- [ ] Forwarded headers + CORS same-origin
- [ ] Workflows `ci.yml` / `deploy.yml`
- [ ] Secrets e variables no GitHub
- [ ] Deploy key SSH se o repo for privado
- [ ] Branch `main` protegida

### C. Primeiro deploy

- [ ] Push/merge em `main` (ou disparo manual uma vez)
- [ ] Acompanhar logs do Docker Manager / Action
- [ ] `GET /api/health` 200
- [ ] Front carrega, `VITE_MOCK_AUTH` efetivamente false (login real)
- [ ] Login `admin@formarca.com` → **trocar senha**
- [ ] Configurar webhook Autentique para `https://<dominio>/api/webhooks/autentique`
- [ ] Testar emissão de contrato + PDF
- [ ] Confirmar que `docker compose down` **sem** `-v` não some o volume
- [ ] Snapshot do VPS “golden”

### D. Operação

- [ ] Backup SQL agendado
- [ ] Atualizar senha SA / JWT só via secrets + redeploy
- [ ] PRs nunca deployam
- [ ] Fase 2 (GHCR) quando o build no VPS ficar lento

---

## 17. Critérios de pronto

O plano está implementado quando:

1. `https://<dominio>/` serve o SPA e rotas client-side não dão 404 no F5.
2. Login contra a API real funciona (`VITE_MOCK_AUTH=false`).
3. `GET /api/health` responde 200 no mesmo host.
4. Migrations aplicam no SQL Express do VPS; dados sobrevivem a um segundo deploy.
5. BackgroundRunner está `Up` (não Restarting).
6. Push em `main` verde no GitHub dispara o Docker Manager; PR não dispara.
7. 1433 não está aberto no firewall público.

---

## 18. Fora de escopo (de propósito)

- Troca SQL Server → MySQL Hostinger
- Reescrita da API em Node para caber no Web App
- Staging VPS separado (pode entrar depois)
- CDN Hostinger / PageSpeed (front é SPA pequeno; Caddy gzip basta)
- E-mail transacional Hostinger (o produto tem [Emails](https://docs.hostinger.com/); o Formarca ainda não tem SMTP próprio no `appsettings`)

---

## 19. Próximo passo de implementação

Quando for para código, nesta ordem:

1. Dockerfiles + compose prod + Caddy (sobe local com `docker compose -f docker-compose.prod.yml up --build` apontando SQL Express).
2. Ajustes `Program.cs` (forwarded headers) e build-args do Vite.
3. Workflows GitHub.
4. Secrets + VPS real.

Não misturar o compose local de SQL (`Your_password123`, porta 1433 publicada) com o de produção.
