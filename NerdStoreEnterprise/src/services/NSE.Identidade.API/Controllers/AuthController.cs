using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSE.Identidade.API.Models;
using System.Threading.Tasks;

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

        public AuthController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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
                return Ok();
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
                return Ok();
            }

            return BadRequest();
        }
    }
}
