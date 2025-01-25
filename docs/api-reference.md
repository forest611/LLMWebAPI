# API リファレンス

このドキュメントでは、LLM Web APIの詳細な仕様を説明します。

## エンドポイント一覧

### Ollama API

#### チャットメッセージの生成

新しいチャットメッセージを生成します。

- **エンドポイント**: `POST /llm/ollama/generate`
- **Content-Type**: `application/json`

##### リクエスト

```json
{
  "prompt": "string",    // 必須: ユーザーからの入力テキスト
  "model": "string"      // オプション: 使用するモデル名（デフォルト: "gemma2:2b"）
}
```

##### レスポンス

```json
{
  "id": "string",        // セッションID
  "response": "string",  // 生成されたレスポンス
  "status": "string"     // ステータス（"Processing" | "Complete" | "Error"）
}
```

##### ステータスコード

- `200 OK`: リクエスト成功
- `400 Bad Request`: 不正なリクエスト
- `500 Internal Server Error`: サーバーエラー

##### エラーレスポンス

```json
{
  "error": {
    "code": "string",
    "message": "string"
  }
}
```

#### チャットセッションの継続

既存のチャットセッションを継続します。

- **エンドポイント**: `POST /llm/ollama/chat/{sessionId}`
- **Content-Type**: `application/json`

##### パスパラメータ

- `sessionId`: チャットセッションのID（必須）

##### リクエスト

```json
{
  "prompt": "string"    // 必須: ユーザーからの入力テキスト
}
```

##### レスポンス

```json
{
  "id": "string",        // セッションID
  "response": "string",  // 生成されたレスポンス
  "status": "string"     // ステータス
}
```

### OpenAI API

[実装予定]

## 共通仕様

### エラーコード

| コード | 説明 |
|--------|------|
| `INVALID_REQUEST` | リクエストパラメータが不正 |
| `MODEL_NOT_FOUND` | 指定されたモデルが見つからない |
| `SESSION_NOT_FOUND` | 指定されたセッションが見つからない |
| `INTERNAL_ERROR` | 内部エラー |

### レート制限

- 1分あたり60リクエスト
- 超過した場合は429 Too Many Requestsを返却

### 認証

[実装予定]
