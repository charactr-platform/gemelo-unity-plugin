#!/bin/bash
TAG=$(git fetch --tags && git describe --tags `git rev-list --tags --max-count=1`)
echo "Release for: $TAG"
echo "Press [ENTER] to continue..."
read -s < /dev/tty
echo "Pushing to upm-release..."
git push origin upm-release  --tags
echo "Pushing to upm-repo main branch..."
git push upm upm-release:main --tags



