#!/bin/bash

# DockerHubのユーザー名を設定
DOCKER_USER="forest611"
IMAGE_NAME="llm-web-api"

# イメージをビルド
docker build -t $DOCKER_USER/$IMAGE_NAME .

# DockerHubにプッシュ
docker push $DOCKER_USER/$IMAGE_NAME
