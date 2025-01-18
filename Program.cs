using LLMWebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // コントローラーを追加
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Ollama service
builder.Services.AddScoped<IOllamaService, OllamaService>();

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