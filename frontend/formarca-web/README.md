# Formarca Web

Frontend da fundação do Painel do Cliente (React + TypeScript + Vite + Ant Design).

## Setup

```bash
cd frontend/formarca-web
npm install
cp .env.example .env   # se ainda não existir
npm run dev
```

Abre em `http://localhost:5173`.

## Variáveis de ambiente

| Variável | Descrição | Exemplo |
|---|---|---|
| `VITE_API_URL` | URL base da API (sem barra final; deve bater com `Formarca.Web` em `http://localhost:5000`) | `http://localhost:5000` |

## Scripts

- `npm run dev` — servidor de desenvolvimento
- `npm run build` — build de produção
- `npm run preview` — preview do build
