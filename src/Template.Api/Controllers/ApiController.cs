using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Template.Api.Controllers;

[ApiController]
public abstract class ApiController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    protected ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        return _sender.Send(request, HttpContext.RequestAborted);
    }
}
