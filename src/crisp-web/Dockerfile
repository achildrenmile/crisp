# CRISP Web UI Dockerfile
# Multi-stage build for React + Vite with nginx

# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY package.json package-lock.json* ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build the application
RUN npm run build

# Production stage with nginx
FROM nginx:alpine AS runtime

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built assets from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Create non-root user
RUN adduser -D -g '' crisp && \
    chown -R crisp:crisp /usr/share/nginx/html && \
    chown -R crisp:crisp /var/cache/nginx && \
    chown -R crisp:crisp /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown -R crisp:crisp /var/run/nginx.pid

# Switch to non-root user
USER crisp

# Expose port
EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:3000/ || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
