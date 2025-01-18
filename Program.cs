using LLMWebAPI.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // コントローラーを追加
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HTTPクライアントの設定
builder.Services.AddHttpClient("OpenAI", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("Ollama", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// サービスの登録
builder.Services.AddSingleton<OllamaService>();
builder.Services.AddSingleton<OpenAIService>();
builder.Services.AddSingleton<ILLMService>(sp => sp.GetRequiredService<OllamaService>());
builder.Services.AddSingleton<ILLMService>(sp => sp.GetRequiredService<OpenAIService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting(); // ルーティングを追加
app.UseAuthorization();
app.MapControllers(); // コントローラーのルートを登録

app.Run();