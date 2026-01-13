var builder = WebApplication.CreateBuilder(args);

// add services and controllers to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// add and configure swagger endpoint documentation
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();
});

var app = builder.Build();

// configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// register controller routes to HTTP request pipeline
app.MapControllers();
app.UseHttpsRedirection();

app.Run();