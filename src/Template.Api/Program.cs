using Template.Api.Endpoints;
using Template.Application;
using Template.Infrastructure;

namespace Template.Api;

public class Program
{
    public void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();

        app.MapProductEndpoints();

        app.Run();
    }
}