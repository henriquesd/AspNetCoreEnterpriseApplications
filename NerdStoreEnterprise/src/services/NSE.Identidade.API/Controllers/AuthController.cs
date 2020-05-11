using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSE.Identidade.API.Extensions;
using NSE.Identidade.API.Models;

namespace NSE.Identidade.API.Controllers
{
    // Se você colocar este atributo 'ApiController', decorando sua Controller, você está dizendo que ela é uma ApiController
    // ela herda de Controller para obter as capacidades de uma Controller, só que você está dizendo que tipo de Controller ela é,
    // e pelo fato de você fazer isso você já libera o entendimento dos schemas do Swagger;
    [ApiController]
    [Route("api/identidade")]
    public class AuthController : Controller
    {
        // Gerenciar questões de logins;
        private readonly SignInManager<IdentityUser> _signInManager;
        // Gerenciar a questão de como eu administro esse usuário;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppSettings _appSettings;

        public AuthController(SignInManager<IdentityUser> signInManager,
                            UserManager<IdentityUser> userManager,
                            // Esse Options é uma opção de leitura que o próprio ASP.NET te da como suporte
                            // para que você leia arquivos de configuração, etc;
                            IOptions<AppSettings> appSettings)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _appSettings = appSettings.Value; // O valor de Options é a instância de appSettings;
        }

        [HttpPost("nova-conta")]
        public async Task<ActionResult> Registrar(UsuarioRegistro usuarioRegistro)
        {
            if (!ModelState.IsValid) return BadRequest();

            var user = new IdentityUser
            {
                // O Identity ele difere, você pode tanto ter um nome de usuário que não seja o seu email,
                // quanto o nome de usuário que seja o email, então é recomendável usar do usuário como email,
                // porque dai o seu usuário não precisa ficar lembrando qual o nome de usuário que ele usou na sua
                // aplicação, basta ele lembrar qual que é o email - e ainda você pode contrar se ele é único ou não,
                // de uma forma mais simples;
                UserName = usuarioRegistro.Email,
                Email = usuarioRegistro.Email,
                EmailConfirmed = true
            };

            // a instância do user não requer a senha, a senha será passada a parte (ela será criptografada);
            var result = await _userManager.CreateAsync(user, usuarioRegistro.Senha);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return Ok(await GerarJwt(usuarioRegistro.Email));
            }

            return BadRequest();
        }

        [HttpPost("autenticar")]
        public async Task<ActionResult> Login(UsuarioLogin usuarioLogin)
        {
            if (!ModelState.IsValid) return BadRequest();

            var result = await _signInManager.PasswordSignInAsync(usuarioLogin.Email, usuarioLogin.Senha,
                false, true);
               
            if (result.Succeeded)
            {
                return Ok(await GerarJwt(usuarioLogin.Email));
            }

            return BadRequest();
        }

        private async Task<UsuarioRespostaLogin> GerarJwt(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            var claims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);

            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            // O Nbf é sobre quando o token vai expirar;
            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()));
            // O Iat é sobre quando o token foi emitido;
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));

            foreach (var userRole in userRoles)
            {
                // Uma Role é um papel;
                // Uma Claim é um dado aberto, ele pode representar tanto uma permissão
                // quanto um dado do usuário; Só que como Claims e Roles são diferentes, mas 
                // ao mesmo tempo você vê da mesma forma, eu estou adicionado as roles como claim,
                // apesar do Identity ver diferença, estamos tratanto tudo da mesma forma;
                claims.Add(new Claim("role", userRole));
            }

            // O ClaimsIdentity é o objeto real, da coleção de Claims que aquele usuário vai ter na representação dele;
            var identityClaims = new ClaimsIdentity();
            identityClaims.AddClaims(claims);

            // Esse JwtSecurityTokenHandler ele vai pegar com base na chave que temos, e gerar o nosso token;
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);

            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _appSettings.Emissor,
                Audience = _appSettings.ValidoEm,
                Subject = identityClaims,
                Expires = DateTime.UtcNow.AddHours(_appSettings.ExpiracaoHoras),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            });

            var encodedToken = tokenHandler.WriteToken(token);

            var response = new UsuarioRespostaLogin
            {
                AccessToken = encodedToken,
                ExpiresIn = TimeSpan.FromHours(_appSettings.ExpiracaoHoras).TotalSeconds,
                UsuarioToken = new UsuarioToken
                {
                    Id = user.Id,
                    Email = user.Email,
                    Claims = claims.Select(c => new UsuarioClaim { Type = c.Type, Value = c.Value })
                }
            };

            return response;
        }

        private static long ToUnixEpochDate(DateTime date)
          => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
    }
}
