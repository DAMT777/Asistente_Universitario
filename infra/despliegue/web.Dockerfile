# Construye el frontend (React + Vite) y lo sirve con Caddy. Contexto: raíz del repositorio.
FROM node:22-alpine AS build
WORKDIR /app
COPY Frontend/Interfaz/code/package*.json ./
RUN npm ci
COPY Frontend/Interfaz/code/ ./
# URL pública del sitio, sin barra final (p. ej. http://3.90.10.20 o https://notas.midominio.com).
ARG PUBLIC_URL
ENV VITE_API_URL=${PUBLIC_URL}/api
RUN npm run build

FROM caddy:2-alpine
COPY infra/despliegue/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /app/dist /srv
