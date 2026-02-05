#!/bin/sh
set -e

# Update htpasswd if credentials are provided via environment variables
if [ -n "$BASIC_AUTH_USER" ] && [ -n "$BASIC_AUTH_PASS" ]; then
    htpasswd -cb /etc/nginx/.htpasswd "$BASIC_AUTH_USER" "$BASIC_AUTH_PASS"
    echo "Updated basic auth credentials for user: $BASIC_AUTH_USER"
fi

# Start nginx
exec nginx -g "daemon off;"
