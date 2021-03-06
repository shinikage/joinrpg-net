using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Joinrpg.Web.Identity;
using Autofac;
using JoinRpg.DataModel;
using JoinRpg.DI;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using System.Diagnostics;

namespace JoinRpg.Portal
{
    public class Startup
    {
        private readonly IWebHostEnvironment environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            this.environment = environment;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<RecaptchaOptions>(Configuration.GetSection("Recaptcha"));

            services.Configure<PasswordHasherOptions>(options => options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2);

            services
                .AddIdentity<JoinIdentityUser, string>(options => options.Password.ConfigureValidation())
                .AddDefaultTokenProviders()
                .AddUserStore<MyUserStore>()
                .AddRoleStore<MyUserStore>();

            services.ConfigureApplicationCookie(AuthenticationConfigurator.SetCookieOptions());

            services.AddLogging();

            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddRouting(options => options.LowercaseUrls = true);
            var mvc = services
                .AddMvc(options =>
                {
                    if (!environment.IsEnvironment("IntegrationTest"))
                    {
                        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                    }
                    options.Filters.Add(new SetIsProductionFilterAttribute());
                    options.Filters.Add(new TypeFilterAttribute(typeof(SetUserDataFilterAttribute)));
                })
                .AddControllersAsServices()
                .AddViewComponentsAsServices();

            if (environment.IsDevelopment())
            {
                //That's make local debug more easy
                mvc.AddRazorRuntimeCompilation();
            }

            services.AddAuthorization();

            services.AddTransient<IAuthorizationPolicyProvider, AuthPolicyProvider>();

            services
                .AddAuthentication()
                .ConfigureJoinExternalLogins(Configuration.GetSection("Authentication"));

            services.AddSwaggerGen(Swagger.ConfigureSwagger);
        }


        /// <summary>
        /// Runs after ConfigureServices
        /// </summary>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new JoinrpgMainModule());
            builder.RegisterModule(new JoinRpgPortalModule());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else if (env.IsEnvironment("IntegrationTest"))
            {
                //need this to ensure that exceptions from controller will fall directly to integration test
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 404)
                {
                    context.Request.Path = "/error/404";
                    await next();
                }
            });


            app.UseSwagger(Swagger.Configure);
            app.UseSwaggerUI(Swagger.ConfigureUI);

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseMiddleware<DiscoverProjectMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //TODO endpoints.MapHealthChecks("/health");
                endpoints.MapControllers();
                endpoints.MapAreaControllerRoute("Admin_default", "Admin", "Admin/{controller}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
