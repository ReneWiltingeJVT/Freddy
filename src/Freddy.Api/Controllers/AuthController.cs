using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Freddy.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Freddy.Api.Controllers;

/// <summary>
/// Authentication endpoints for development use.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IConfiguration configuration) : ControllerBase
{
    private const string DevUserId = "00000000-0000-0000-0000-000000000001";

    /// <summary>
    /// Generates a JWT token for development. Only available in Development environment.
    /// </summary>
    [HttpPost("dev-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetDevToken([FromServices] IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            return NotFound();
        }

        SymmetricSecurityKey key = new(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, DevUserId),
            new Claim(JwtRegisteredClaimNames.Name, "Dev User"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
        ];

        JwtSecurityToken token = new(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return Ok(new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token)));
    }
}
