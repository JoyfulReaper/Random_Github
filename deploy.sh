#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
STACK_DIR="/opt/joyful-stack"
ENV_FILE="$STACK_DIR/.env"

SERVICE="randomgithub"
IMAGE_TAG_VAR="RANDOMGITHUB_IMAGE_TAG"
CONTAINER="joyful-stack-randomgithub-1"

HEALTH_URL="http://127.0.0.1:5183/health/live"
HEALTH_HOST="randomgit.kgivler.com"

cd "$REPO_DIR"

echo "==> Checking repository"

if [[ -n "$(git status --porcelain)" ]]; then
    echo "ERROR: Working tree has uncommitted changes."
    git status --short
    exit 1
fi

BRANCH="$(git branch --show-current)"
SHA="$(git rev-parse --short=7 HEAD)"

echo "    Branch: $BRANCH"
echo "    Commit: $SHA"

if git rev-parse --abbrev-ref '@{upstream}' >/dev/null 2>&1; then
    UPSTREAM="$(git rev-parse --abbrev-ref '@{upstream}')"

    if [[ "$(git rev-parse HEAD)" != "$(git rev-parse "$UPSTREAM")" ]]; then
        echo
        echo "ERROR: Local HEAD does not match $UPSTREAM."
        echo "Push/pull/rebase first so the deployed commit exists on GitHub."
        echo
        git status -sb
        exit 1
    fi
fi

if [[ ! -f "$ENV_FILE" ]]; then
    echo "ERROR: $ENV_FILE does not exist."
    exit 1
fi

if ! grep -q "^${IMAGE_TAG_VAR}=" "$ENV_FILE"; then
    echo "ERROR: ${IMAGE_TAG_VAR} is missing from $ENV_FILE."
    exit 1
fi

echo
echo "==> Setting ${IMAGE_TAG_VAR}=$SHA"

sudo sed -i \
    "s/^${IMAGE_TAG_VAR}=.*/${IMAGE_TAG_VAR}=${SHA}/" \
    "$ENV_FILE"

grep "^${IMAGE_TAG_VAR}=" "$ENV_FILE"

cd "$STACK_DIR"

echo
echo "==> Building $SERVICE"

sudo docker compose build "$SERVICE"

echo
echo "==> Recreating $SERVICE"

sudo docker compose up \
    -d \
    --no-deps \
    --force-recreate \
    "$SERVICE"

echo
echo "==> Waiting for health check"

healthy=0

for attempt in {1..20}; do
    if curl -fsS \
        -H "Host: $HEALTH_HOST" \
        "$HEALTH_URL" >/dev/null 2>&1; then
        healthy=1
        break
    fi

    sleep 1
done

echo
sudo docker compose ps "$SERVICE"

echo
echo "Running image:"
sudo docker inspect "$CONTAINER" \
    --format '{{.Config.Image}}'

if [[ "$healthy" -ne 1 ]]; then
    echo
    echo "ERROR: Health endpoint did not become healthy."
    echo
    echo "Recent logs:"
    sudo docker compose logs --tail=50 "$SERVICE"
    exit 1
fi

echo
echo "Health: OK"
echo "Deployed Random GitHub @ $SHA"
