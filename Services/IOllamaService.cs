using LLMWebAPI.Models;
using System.Collections.Generic;

namespace LLMWebAPI.Services;

/// <summary>
/// Ollamaサービスのインターフェース
/// チャットの処理と履歴管理を担当
/// </summary>
public interface IOllamaService
{
    /// <summary>
    /// チャットリクエストを処理し、AIからの応答を生成する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <param name="model">使用するモデル名</param>
    /// <param name="prompt">ユーザーからの入力</param>
    /// <returns>AI応答を含むチャットレスポンス</returns>
    Task<ChatResponse> ProcessChatAsync(string id, string model, string prompt);

    /// <summary>
    /// 指定されたセッションIDのチャット履歴を取得する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <returns>チャットメッセージのリスト。セッションが存在しない場合は空のリスト</returns>
    List<ChatMessage> GetChatHistory(string id);
}
