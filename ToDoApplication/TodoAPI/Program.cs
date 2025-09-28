
using ToDoApp.ClassLibrary;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register your existing services
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<ITodoDataAccess, TodoDatabaseDataAccess>(); // or TodoDataAccess for file storage

// Add CORS for frontend development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseCors("AllowAll");

//app.UseAuthorization();

app.MapControllers();

app.Run();