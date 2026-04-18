using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Template.Application.Common.Behaviors;
using Template.Application.Common.Interfaces;
using Template.Application.Products.Guards;
using Template.Domain.Entities;

namespace Template.Application;

public static class DependencyInjection {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        services.AddMediator(options => {
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IQueryGuard<Product>, FacilityProductGuard>();

        return services;
    }
}
