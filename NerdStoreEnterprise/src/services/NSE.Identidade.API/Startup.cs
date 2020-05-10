using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSE.Identidade.API.Data;

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
            // Esse "AddDefaultTokenProviders", n�o tem haver com json web token; s�o aqueles tokens que ele gera
            // para caso voc� precise resetar uma senha, ou at� mesmo autenticar uma conta rec�m criada, ele vai criar um
            // token, uma chave, que � para identificar quando voc� recebeu um e-mail, clicando naquele link, que voc� �
            // aquele usu�rio, que aquele e-mail foi enviado para voc�, e que voc� realmente � a pessoa que deveria receber
            // aquele e-mail; � uma criptografia dentro de um link para te reconhecer;

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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
