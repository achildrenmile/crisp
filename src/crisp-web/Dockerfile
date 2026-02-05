# CRISP Web UI Dockerfile
# Multi-stage build for React + Vite with nginx

# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY package.json package-lock.json* ./

# Install dependencies
RUN npm install

# Copy source code
COPY . .

# Build the application
RUN npm run build

# Production stage with nginx
FROM nginx:alpine AS runtime

# Install apache2-utils for htpasswd
RUN apk add --no-cache apache2-utils

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built assets from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Create htpasswd file from environment variables (will be populated at runtime)
# Default credentials: demo / demo (override with BASIC_AUTH_USER and BASIC_AUTH_PASS env vars)
RUN htpasswd -cb /etc/nginx/.htpasswd demo demo

# Copy entrypoint script
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Create non-root user (but keep htpasswd writable for entrypoint)
RUN adduser -D -g '' crisp && \
    chown -R crisp:crisp /usr/share/nginx/html && \
    chown -R crisp:crisp /var/cache/nginx && \
    chown -R crisp:crisp /var/log/nginx && \
    chown crisp:crisp /etc/nginx/.htpasswd && \
    touch /var/run/nginx.pid && \
    chown -R crisp:crisp /var/run/nginx.pid

# Switch to non-root user
USER crisp

# Expose port
EXPOSE 3000

# Health check (use 127.0.0.1 to avoid IPv6 resolution issues, with auth bypass)
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider --user=demo --password=demo http://127.0.0.1:3000/ || exit 1

# Start nginx via entrypoint
ENTRYPOINT ["/docker-entrypoint.sh"]
