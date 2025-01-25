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

## API エンドポイント

基本的なAPIの使用方法については以下を参照してください。
詳細なAPIリファレンスは[API Reference](docs/api-reference.md)を参照してください。

### Ollama Chat API

- エンドポイント: `POST /llm/ollama/chat`
- 機能: Ollamaを使用したチャット応答の生成
- リクエスト例:
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
