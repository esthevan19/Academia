using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    public sealed record LoginRequest(string Email, string Password);

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { success = false, message = "Informe usuário/e-mail e senha." });

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        user ??= await _userManager.FindByNameAsync(request.Email.Trim());

        if (user is null)
            return Unauthorized(new { success = false, message = "Usuário ou senha inválidos." });

        var passwordResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!passwordResult.Succeeded)
            return Unauthorized(new { success = false, message = "Usuário ou senha inválidos." });

        var keyText = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(keyText) || Encoding.UTF8.GetByteCount(keyText) < 32)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { success = false, message = "Autenticação mobile ainda não foi configurada na hospedagem." });

        var issuer = _configuration["Jwt:Issuer"] ?? "GymFlow";
        var audience = _configuration["Jwt:Audience"] ?? "GymFlow.Mobile";
        var roles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);
        var fullName = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? user.Email ?? user.Id : fullName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(userClaims.Where(claim => claim.Type == "gymflow:member_id"));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: credentials);

        return Ok(new
        {
            success = true,
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt = expires,
            user = new
            {
                id = user.Id,
                email = user.Email,
                userName = user.UserName,
                fullName = string.IsNullOrWhiteSpace(fullName) ? user.UserName : fullName,
                roles
            }
        });
    }
}
