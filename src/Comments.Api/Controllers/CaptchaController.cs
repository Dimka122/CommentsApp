using Comments.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Comments.Api.Controllers;

[ApiController, Route("api/captcha")]
public class CaptchaController(ICaptchaService captcha) : ControllerBase
{
    [HttpGet, EnableRateLimiting("read")]
    public async Task<CaptchaChallenge> Get(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return await captcha.CreateAsync(ct);
    }
}
