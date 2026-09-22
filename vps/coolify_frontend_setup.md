# Coolify Frontend Setup — Test Deployment (dev.ardh.co.in)

**Server:** `200.234.37.191` (srv1903336)
**Date:** Sep 18, 2026
**Status:** ✅ Complete and verified working

Goal: deploy the frontend to a subdomain via Coolify to validate the setup, without touching the live `ardh.co.in` / `api.ardh.co.in` / `ardh-frontend` / `ardh-api` / `ardh-db` stack in any way. Backend test deployment (`dev-api.ardh.co.in`) was already done before this (see `coolify-setup-progress.md`).

---

## Repo files added (frontend repo)

- **`Dockerfile`** (repo root) — multi-stage build:
  - Stage 1 (`node:20-alpine`): `npm ci` → `npm run build` (Vite build)
  - Stage 2 (`nginx:1.27-alpine`): serves the built `dist/` folder
  - Declares a build-time default for the API base path:
    ```dockerfile
    ARG VITE_API_BASE_URL=/api
    ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
    RUN npm run build
    ```
  - `EXPOSE 80`
- **`docker/nginx.conf`** — container-internal Nginx config: SPA fallback (`try_files $uri /index.html`), static asset caching. Does **not** handle `/api` — that's done one level up, by the host Nginx vhost (see below).
- **`.dockerignore`** — excludes `node_modules`, `dist`, `.git`, `vps/`, `.env*`, etc. from the build context.

---

## Git / repo routing (this tripped us up once — worth remembering)

The local repo has **three** remotes:

| Remote | URL | Role |
|---|---|---|
| `origin` | `github.com/ArunRoy404/ardh-frontend` | Main dev remote, branch `roy` |
| `org` | `github.com/TECHREION/ARDH-Frontend` | Org mirror |
| `deploy` | `github.com/ROY-0404/ARDH-Frotnend` | **This is what Coolify's GitHub App is connected to** |

Coolify pulls from `ROY-0404/ARDH-Frotnend`, branch **`main`** — a *different* repo/account than `origin`. When the Dockerfile was first added and pushed to `origin/roy`, it happened to already be in sync with `deploy/main` (same commit SHA), so it worked — but this isn't automatic in general. **Before relying on a Coolify rebuild, confirm the commit you expect is actually present on `deploy`'s `main` branch**, not just `origin`.

---

## Coolify resource

- **Project:** `ARDH-Test` → **Environment:** `production`
- **Resource name:** `a-r-d-h--frotnend:main-tv81apjvgdmqziu1uqpcwrpl`
- **Source:** Private GitHub App → `ROY-0404/ARDH-Frotnend`, branch `main`
- **Build pack:** Dockerfile, base directory `/`

### Networking (Settings → Networking)
- **Ports exposes:** `80` (matches the container's Nginx, `EXPOSE 80` in the Dockerfile — Coolify's default placeholder of `3000` had to be changed)
- **Port mappings:** `127.0.0.1:8090:80` — binds the container to `127.0.0.1:8090` on the host, localhost-only, same pattern as the backend's `127.0.0.1:8081`. Never exposed directly to the internet.
- Domains/FQDN/sslip.io auto-domain: **left unused** — Coolify's own Traefik proxy is not fronting this app. Host Nginx handles public routing instead (see below), because Coolify's built-in proxy is not bound to 80/443 on this server (Nginx already holds those ports — see `coolify-setup-progress.md`).

### Environment Variables (Settings → Environment Variables)
- `VITE_API_BASE_URL` = `/api`
  - **Build time:** `Available during build` ✅ (this is the important toggle — passed to `docker build` as a `--build-arg`, matching the Dockerfile's `ARG VITE_API_BASE_URL`)
  - **Runtime:** irrelevant/unused — the running container is just static Nginx, nothing reads env vars after build
  - This **overrides** the Dockerfile's own `/api` default when present; if removed, falls back to the Dockerfile default automatically. Either source currently resolves to the same value (`/api`), so behavior is identical either way — but the dashboard variable is now the "visible" source of truth going forward.

---

## The bug we hit and fixed: missing API base URL

**Symptom:** after first deploy, sign-in returned `405 Not Allowed`. DevTools showed the request going to `https://dev.ardh.co.in/auth/sign-in` — missing the `/api` prefix.

**Root cause:** `VITE_API_BASE_URL=/api` only ever existed in `.env.local`, which is gitignored (`*.local` in `.gitignore`) and never reached GitHub. Coolify's build had no env file to read, `import.meta.env.VITE_API_BASE_URL` was `undefined`, and axios's `baseURL` ([axios.config.js](../src/axios/axios.config.js)) ended up empty — so calls went to `/auth/sign-in` instead of `/api/auth/sign-in`, which the frontend's own container Nginx then rejected with 405 (POST to a static-file location).

**Fix:** baked a build-time default directly into the Dockerfile (`ARG VITE_API_BASE_URL=/api`), later also mirrored as a Coolify "available during build" env var for visibility. Vite inlines this into the compiled JS bundle at `npm run build` time — it's a one-time bake, not something read at container runtime (fundamentally different from the backend's runtime env vars like `MSSQL_SA_PASSWORD`).

**Why `.env.local`'s correct value never helped:** that file is local-machine-only, used solely by `npm run dev`. It has zero connection to what Coolify builds. Similarly, `VITE_PROXY_TARGET` (also in `.env.local`) only affects Vite's local dev server proxy (`vite.config.js`'s `server.proxy` block) — irrelevant to the production build entirely.

---

## Host Nginx vhost: `dev-frontend`

File: `/etc/nginx/sites-available/dev-frontend`, symlinked into `/etc/nginx/sites-enabled/`.

Modeled directly on the existing `dev-api` / `ardh-frontend` vhosts — one server block, two locations, so the frontend's relative `/api` calls keep working via same-origin proxying (no CORS, no cross-origin cookie complications):

```nginx
server {
    listen 80;
    server_name dev.ardh.co.in;

    client_max_body_size 25m;

    location /api/ {
        proxy_pass http://127.0.0.1:8081;   # dev-api backend container
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }

    location / {
        proxy_pass http://127.0.0.1:8090;   # this frontend container
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

Certbot (`sudo certbot --nginx -d dev.ardh.co.in`) then rewrote this file in place to add the HTTPS server block + HTTP→HTTPS redirect (same pattern Certbot used on `dev-api`).

---

## DNS

- `dev.ardh.co.in` → `A` → `200.234.37.191` (added via Hostinger DNS panel)

---

## Final verified state

| Piece | Value |
|---|---|
| Frontend container | `127.0.0.1:8090 → 80` (Coolify-managed) |
| Backend container (dev) | `127.0.0.1:8081 → 8080` (Coolify-managed, `api-td8e4wtlefrrgsvhzmv1o1w0-...`) |
| Public URL | `https://dev.ardh.co.in` (Let's Encrypt SSL, auto-renews) |
| `/api/*` routing | Host Nginx proxies to `127.0.0.1:8081` — same-origin from the browser's perspective |
| Live prod | `ardh.co.in`, `api.ardh.co.in`, `ardh-frontend`, `ardh-api`, `ardh-db` — **completely untouched** throughout |

### How it was verified (not just assumed)
1. `curl -I http://127.0.0.1:8090` → confirmed container serving the built app directly
2. `curl -i -X POST https://dev.ardh.co.in/api/auth/sign-in ...` → `200 OK`, cookie set with `Secure; SameSite=None` (matches prod's cookie behavior, confirms HTTPS + same-origin proxy working correctly)
3. `docker logs -f api-td8e4wtlefrrgsvhzmv1o1w0-...` tailed live while using the site in browser — confirmed requests (`GET /api/notifications/count`, `GET /api/expenses`) actually landing on this exact container, with JWT cookie validating for "Super Admin", hitting the dev DB (not prod's)
4. Confirmed via `docker ps` that only one backend container exists on Coolify (`api-td8e4wtlefrrgsvhzmv1o1w0`) distinct from the native `ardh-api` (different ports: `8081` vs `8080`) — no ambiguity about which backend the dev frontend talks to
5. Confirmed via Coolify dashboard that `VITE_API_BASE_URL` is correctly baked in even with zero (at the time) or explicit (after) Coolify env vars — proven by the deployed JS bundle's actual network behavior, not just config inspection

---

## Open items / not done yet

- Cutover to live domains (`ardh.co.in`, `api.ardh.co.in`) — **on hold, requires client approval**, per the original migration plan in `ardh-vps-current-state.md`
- Coolify's own Traefik proxy remains "Exited" / not fronting 80/443 — intentional, host Nginx stays the single entry point for now
