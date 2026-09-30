# Front: build do Vite com Node e arquivos estáticos servidos pelo nginx.
# VITE_API_BASE_URL é lida em tempo de BUILD (fica gravada no JavaScript), e não na execução do contêiner.
# Por isso ela aponta para o endereço do BFF visto pelo navegador: a porta publicada no host.
FROM node:24.21-alpine AS build
WORKDIR /web
COPY monitoramento-web/package.json monitoramento-web/package-lock.json ./
RUN npm ci
COPY monitoramento-web/ .
ARG VITE_API_BASE_URL=http://localhost:5101/api/
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
RUN npm run build

FROM nginx:1.31-alpine
COPY docker/nginx-web.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist /usr/share/nginx/html
