# LLM Web API

OpenAIとOllamaを利用したLLM Web APIです。

## 機能

- OpenAI APIを使用したチャット
- Ollamaを使用したローカルLLM
- APIキー認証
- Swagger UI

## 必要条件

- .NET 8.0
- Docker（オプション）
- OpenAI APIキー（OpenAI機能を使用する場合）
- Ollama（Ollama機能を使用する場合）

## インストール

### ローカル実行

1. リポジトリのクローン
```bash
git clone https://github.com/forest611/LLMWebAPI.git
cd LLMWebAPI
```

2. 環境変数の設定
```bash
# OpenAI APIキー
export OpenAI__ApiKey="your-openai-key"

# APIキー認証用のキー
export Authentication__ApiKey="your-api-key"
```

3. アプリケーションの実行
```bash
dotnet run
```

### Docker実行

1. イメージのプル
```bash
docker pull forest611/llm-web-api:latest
```

2. コンテナの実行
```bash
docker run -d \
  -p 7070:7070 \
  -p 7071:7071 \
  -e Authentication__ApiKey="your-api-key" \
  -e OpenAI__ApiKey="your-openai-key" \
  -e Ollama__BaseUrl="http://host.docker.internal:11434" \
  forest611/llm-web-api
```

### Docker Compose

1. 環境変数の設定
```bash
# .envファイルを作成
echo "OPENAI_API_KEY=your-openai-key" > .env
echo "API_KEY=your-api-key" >> .env
```

2. サービスの起動
```bash
docker compose up -d
```

## 使用方法

### APIエンドポイント

#### OpenAI

- エンドポイント: `POST /api/openai/chat`
- 機能: OpenAI APIを使用したチャット応答の生成
- リクエスト例：
```json
{
  "prompt": "こんにちは！"
}
```

#### Ollama

- エンドポイント: `POST /api/ollama/chat`
- 機能: Ollamaを使用したチャット応答の生成
- リクエスト例：
```json
{
  "prompt": "こんにちは！"
}
```

### 認証

すべてのAPIリクエストに`X-API-KEY`ヘッダーが必要です：

```bash
curl -H "X-API-KEY: your-api-key" http://localhost:7070/api/ollama/models
```

### Swagger UI

APIドキュメントとテストは`/swagger`で利用可能です：

1. `https://localhost:7071/swagger`にアクセス
2. 右上の「Authorize」ボタンをクリック
3. APIキーを入力
4. APIをテスト

## トラブルシューティング

- ポートが使用中の場合は、別のポートを指定してください
- 証明書エラーの場合は、開発環境では`-k`オプションを使用してください
```bash
curl -k -H "X-API-KEY: your-api-key" https://localhost:7071/api/ollama/models
