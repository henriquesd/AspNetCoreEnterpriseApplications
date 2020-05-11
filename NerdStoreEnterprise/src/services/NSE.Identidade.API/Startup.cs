using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NSE.Identidade.API.Data;
using NSE.Identidade.API.Extensions;
using System;
using System.Text;

namespace NSE.Identidade.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Esse "AddDefaultTokenProviders", não tem haver com json web token; são aqueles tokens que ele gera
            // para caso você precise resetar uma senha, ou até mesmo autenticar uma conta recém criada, ele vai criar um
            // token, uma chave, que é para identificar quando você recebeu um e-mail, clicando naquele link, que você é
            // aquele usuário, que aquele e-mail foi enviado para você, e que você realmente é a pessoa que deveria receber
            // aquele e-mail; é uma criptografia dentro de um link para te reconhecer;
            services.AddDefaultIdentity<IdentityUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();


            // JWT
 
            // Vai até o arquivo de configuração e vai pegar o nó 'AppSettings';
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);

            // Adicionado o middleware de autenticação e dizendo que vou trabalhar com os padrões do Json webtoken;
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(bearerOptions =>
            {
                bearerOptions.RequireHttpsMetadata = true;
                bearerOptions.SaveToken = true;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key), // aqui vai ser meu segredo, como que eu vou embaralhar os dados dentro do token - essa vai ser minha chave de criptografia;
                    ValidateIssuer = true, // vai sempre validar o emissor; nao vai ser aceito token emitido de um outro emissor;
                    ValidateAudience = true, // onde esse token é valido, para quais domínios esse token é válido;
                    ValidAudience = appSettings.ValidoEm,
                    ValidIssuer = appSettings.Emissor
                };
            });

            services.AddControllers();

            // A documentação do Swagger não é mandatória, então ela pode ser colocada por último;
            // ela permite algum tipo de configuração;
            //services.AddSwaggerGen();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "NerdStore Enterprise Identity API",
                    Description = "Esta API faz parte do curso ASP.NET Core Enterprise Applications.",
                    Contact = new OpenApiContact() { Name = "Henrique S. Domareski", Email = "contato@contato.com" },
                    License = new OpenApiLicense() { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
                });
            });

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
