using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NSE.Identidade.API.Data;
using System;

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

            services.AddDefaultIdentity<IdentityUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            // Esse "AddDefaultTokenProviders", não tem haver com json web token; são aqueles tokens que ele gera
            // para caso você precise resetar uma senha, ou até mesmo autenticar uma conta recém criada, ele vai criar um
            // token, uma chave, que é para identificar quando você recebeu um e-mail, clicando naquele link, que você é
            // aquele usuário, que aquele e-mail foi enviado para você, e que você realmente é a pessoa que deveria receber
            // aquele e-mail; é uma criptografia dentro de um link para te reconhecer;

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
