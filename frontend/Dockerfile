# Production Dockerfile for the WalkerFCB Angular frontend.
# Multi-stage: node builds the Angular app, nginx serves the static output.

FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine AS runtime

COPY --from=build /app/dist/walker-fcb/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80
