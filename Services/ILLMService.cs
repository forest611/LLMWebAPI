using LLMWebAPI.Models;

namespace LLMWebAPI.Services;

/// <summary>
/// LLM（Large Language Model）サービスの共通インターフェース
/// </summary>
public interface ILLMService
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

    /// <summary>
    /// 指定されたセッションIDのチャットセッションを取得する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <returns>チャットセッション。セッションが存在しない場合はnull</returns>
    ChatSession? GetChatSession(string id);

    /// <summary>
    /// このサービスで利用可能なモデル一覧を取得する
    /// </summary>
    /// <returns>利用可能なモデル名のリスト</returns>
    Task<List<string>> GetAvailableModelsAsync();

    /// <summary>
    /// 指定されたモデルがこのサービスで利用可能かどうかを確認する
    /// </summary>
    /// <param name="model">確認するモデル名</param>
    /// <returns>利用可能な場合はtrue</returns>
    Task<bool> IsModelAvailableAsync(string model);
}
