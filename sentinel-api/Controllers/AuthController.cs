using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using sentinel_api.Data;
using sentinel_api.Models;
using sentinel_api.Services;

namespace sentinel_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly AppDbContext _context;

        public AuthController(UserManager<User> userManager,
                               SignInManager<User> signInManager,
                               IConfiguration configuration,
                               EmailService emailService,
                               AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailService = emailService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Register model)
        {
            var user = new User { UserName = model.Name, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var tokenEntity = new EmailConfirmToken
            {
                UserId = user.Id,
                Token = token
            };

            _context.EmailConfirmTokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            var confirmationLink = $"{Request.Scheme}://{Request.Host}/api/auth/confirm-email?id={tokenEntity.Id}";

            var htmlMessage = $@"
            <p>Olá {user.UserName},</p>
            <p>Clique no botão abaixo para confirmar seu e-mail:</p>
            <p><a style='padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none;' href='{confirmationLink}'>Confirmar E-mail</a></p>
            <p>Se você não se registrou, ignore este e-mail.</p>
             ";

            await _emailService.SendEmailAsync(model.Email, "confirmação de e-mail", htmlMessage);

            return Ok(new { message = "Usuário registrado! Verifique seu e-mail para confirmar a conta." });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(Guid id)
        {
            var tokenEntry = await _context.EmailConfirmTokens
                .FirstOrDefaultAsync(t => t.Id == id && t.Expiration > DateTime.UtcNow);

            if (tokenEntry == null)
                return BadRequest("Token inválido ou expirado.");

            var user = await _userManager.FindByIdAsync(tokenEntry.UserId);
            if (user == null)
                return BadRequest("Usuário não encontrado.");

            var result = await _userManager.ConfirmEmailAsync(user, tokenEntry.Token);

            _context.EmailConfirmTokens.Remove(tokenEntry);
            await _context.SaveChangesAsync();

            return result.Succeeded ? Ok("E-mail confirmado com sucesso!") : BadRequest("Erro ao confirmar e-mail.");
        }      

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized("Credenciais inválidas.");
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized("Confirme seu e-mail antes de fazer login.");
            }

            var token = GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

