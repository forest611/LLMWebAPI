# LLM Web API

OllamaとOpenAIに対応したチャットAPIを提供するASP.NET Core Web APIプロジェクトです。

## 機能

- Ollamaを使用したチャットAPI
- OpenAIを使用したチャットAPI（実装予定）
- チャットセッション管理
- Swagger UIによるAPI文書化

## 必要要件

- .NET 8.0 SDK
- Ollama（ローカルで実行する場合）
- OpenAI API Key（OpenAIを使用する場合）

## セットアップ

1. プロジェクトのクローン
```bash
git clone [repository-url]
cd LLMWebAPI
```

2. 依存関係のインストール
```bash
dotnet restore
```

3. 設定ファイルの準備
`appsettings.json`に以下の設定を追加：

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "gemma2:2b"
  },
  "OpenAI": {
    "BaseUrl": "https://api.openai.com/v1",
    "ApiKey": "your-api-key-here"
  }
}
```

4. アプリケーションの実行
```bash
dotnet run
```

## Docker での起動方法

### 前提条件
- Docker がインストールされていること
- .NET 8.0 SDK（開発時のみ）

### クイックスタート

1. イメージのビルドとDockerHubへのプッシュ
```bash
# スクリプトに実行権限を付与
chmod +x scripts/docker-push.sh

# DockerHubユーザー名を設定（初回のみ）
# scripts/docker-push.sh 内の DOCKER_USER を変更

# ビルドとプッシュを実行
./scripts/docker-push.sh
```

2. コンテナの起動
```bash
# HTTP のみを使用する場合
docker run -d -p 7070:7070 forest611/llm-web-api

# HTTPS を使用する場合（推奨）
docker run -d -p 7070:7070 -p 7071:7071 forest611/llm-web-api
```

### HTTPS の設定（開発環境）

1. 開発用証明書の生成
```bash
mkdir -p certs
cd certs
dotnet dev-certs https -ep ./aspnetapp.pfx -p password123
```

2. 証明書を信頼（オプション）
```bash
dotnet dev-certs https --trust
```

### アクセス方法

- HTTP: `http://localhost:7070`
  - HTTPSが有効な場合、自動的にHTTPSにリダイレクトされます
- HTTPS: `https://localhost:7071`
  - 開発用証明書を使用している場合、ブラウザで警告が表示されることがあります

### Swagger UI

API仕様の確認やテストは、以下のURLで行えます：
- HTTP: `http://localhost:7070/swagger`
- HTTPS: `https://localhost:7071/swagger`

### 環境変数

以下の環境変数を設定することで、APIキーなどを設定できます：

```bash
docker run -d \
  -p 7070:7070 \
  -p 7071:7071 \
  -e OpenAI__ApiKey="your-api-key" \
  -e Ollama__BaseUrl="http://your-ollama-server:11434" \
  forest611/llm-web-api
```

### 注意事項

1. 開発環境での利用
   - 自己署名証明書を使用しているため、ブラウザで警告が表示される場合があります
   - 開発・テスト目的にのみ使用してください

2. 本番環境での利用
   - 正式なSSL証明書を使用してください
   - 証明書のパスワードは必ず変更してください
   - 適切なセキュリティ設定を行ってください

3. トラブルシューティング
   - ポートが既に使用されている場合は、別のポートを指定してください
   - 証明書関連のエラーが発生した場合は、証明書の設定を確認してください

## Docker

### イメージのプル

Docker Hubから最新のイメージをプルします：

```bash
docker pull forest611/llm-web-api:latest
```

### コンテナの実行

以下のコマンドでコンテナを起動します：

```bash
docker run -d \
  -p 7070:7070 \
  -p 7071:7071 \
  -e Authentication__ApiKey="your-api-key" \
  -e OpenAI__ApiKey="your-openai-key" \
  -e Ollama__BaseUrl="http://host.docker.internal:11434" \
  forest611/llm-web-api
```

### 環境変数

以下の環境変数を設定する必要があります：

| 環境変数 | 説明 | 必須 |
|----------|------|------|
| `Authentication__ApiKey` | APIキー認証用のキー | ✅ |
| `OpenAI__ApiKey` | OpenAI APIのキー | ✅ |
| `Ollama__BaseUrl` | OllamaサーバーのベースURL | ✅ |

### ポート

- `7070`: HTTP
- `7071`: HTTPS

### 注意事項

1. Ollamaサーバーへの接続
   - Docker内から`localhost`でOllamaサーバーにアクセスする場合は、`host.docker.internal`を使用してください
   - 例：`http://host.docker.internal:11434`

2. HTTPS
   - デフォルトでHTTPSが有効になっています
   - 自己署名証明書を使用しているため、開発環境では`-k`オプションを使用してください
   ```bash
   curl -k -H "X-API-KEY: your-api-key" https://localhost:7071/api/ollama/models
   ```

## 認証

このAPIはAPIキー認証を使用しています。

### APIキーの設定

1. `appsettings.json`での設定：
```json
{
  "Authentication": {
    "ApiKey": "your-api-key-here"
  }
}
```

2. 環境変数での設定（推奨）：
```bash
export Authentication__ApiKey="your-secret-key"
```

3. Docker実行時の設定：
```bash
docker run -d \
  -p 7070:7070 \
  -p 7071:7071 \
  -e Authentication__ApiKey="your-secret-key" \
  forest611/llm-web-api
```

### APIの使用方法

1. curlでの使用例：
```bash
curl -H "X-API-KEY: your-secret-key" http://localhost:7070/api/openai/chat
```

2. HTTPクライアントでの使用：
すべてのリクエストに`X-API-KEY`ヘッダーを追加してください。

3. Swagger UIでのテスト：
   1. Swagger UI（`/swagger`）にアクセス
   2. 右上の「Authorize」ボタンをクリック
   3. APIキーを入力
   4. 「Authorize」をクリック

### セキュリティに関する注意

1. 本番環境では、必ず環境変数でAPIキーを設定してください
2. APIキーは強力なランダム文字列を使用してください
3. APIキーは定期的に更新することをお勧めします
4. HTTPSと組み合わせて使用してください

## API エンドポイント

基本的なAPIの使用方法については以下を参照してください。
詳細なAPIリファレンスは[API Reference](docs/api-reference.md)を参照してください。

### Ollama Chat API

- エンドポイント: `POST /llm/ollama/chat`
- 機能: Ollamaを使用したチャット応答の生成
- リクエスト例：
```json
{
  "prompt": "こんにちは！"
}
```

## 開発

- ASP.NET Core 8.0
- Swagger UI
- HTTPSサポート
- ログ機能

## ライセンス

[ライセンス情報を追加]
