# Despliegue en AWS (una instancia EC2 con Docker Compose)

Arquitectura: el navegador entra por **Caddy** (puertos 80/443), que sirve el frontend y reenvía `/api/*` al gateway.
SQL Server, Azurite y los servicios no se publican. Todo corre en una sola instancia.

## Requisitos de AWS

- Instancia **t3.small** (2 GB de RAM) como mínimo: SQL Server no corre bien con 1 GB. Confirme en su consola qué cubre el plan gratuito vigente.
- Ubuntu 24.04, disco gp3 de 30 GB, Elastic IP.
- Security Group: 22 (solo su IP), 80 y 443. **No abra** 1433 ni 5000–5005.

## Pasos

1. Cree la instancia, asígnele una Elastic IP y conéctese: `ssh -i clave.pem ubuntu@IP_ELASTICA`.
2. Clone y prepare el servidor (instala Docker, crea swap, genera llaves JWT y el `.env`):
   ```bash
   git clone https://github.com/DAMT777/Asistente_Universitario.git
   cd Asistente_Universitario
   bash infra/despliegue/preparar-servidor.sh
   exit    # y vuelva a entrar por SSH para que aplique el grupo docker
   ```
3. Edite `infra/despliegue/.env`:
   - `PUBLIC_URL=http://IP_ELASTICA` (o `https://su-dominio` si tiene dominio).
   - `SQL_SA_PASSWORD` con una contraseña fuerte.
   - `LLM_API_KEY` con su clave de Groq.
   - Con dominio: `SITE_ADDRESS=su-dominio` (el DNS debe apuntar a la Elastic IP; Caddy obtiene el certificado solo).
4. Levante todo (la primera vez tarda varios minutos en compilar):
   ```bash
   docker compose -f infra/despliegue/docker-compose.aws.yml up --build -d
   docker compose -f infra/despliegue/docker-compose.aws.yml ps
   ```
5. Abra `http://IP_ELASTICA` e ingrese con `P0001` o `E0001` (contraseña `Demo1234!`).

## Operación

- Actualizar: `bash infra/despliegue/actualizar.sh`
- Logs: `docker compose -f infra/despliegue/docker-compose.aws.yml logs -f gateway` (o el servicio que falle)
- Si cambia `PUBLIC_URL`, reconstruya `web`: `docker compose -f infra/despliegue/docker-compose.aws.yml up --build -d web gateway`
- Para no gastar créditos: detenga la instancia, libere la Elastic IP y borre el disco al terminar.

## Limitaciones conocidas

- Los servicios corren con `ASPNETCORE_ENVIRONMENT=Development` porque los usuarios de prueba y la base de usuarios en memoria
  solo existen así. Es válido para una demo; para datos reales hay que crear un proveedor de usuarios persistente.
- Los usuarios se pierden al reiniciar el contenedor `usuarios` (base en memoria).
- Azurite reemplaza a S3/Blob: los archivos de entregas viven en un volumen de la instancia.
- Sin respaldo automático: haga un snapshot del volumen EBS si los datos importan.
