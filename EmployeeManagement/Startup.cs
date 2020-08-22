using EmployeeManagement.Models;
using EmployeeManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace EmployeeManagement
{
    public class Startup
    {
        private IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextPool<AppDbContext>(options => options.UseSqlServer(_config.GetConnectionString("EmployeeDbConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>(
                    options =>
                    {
                        options.Password.RequiredLength = 5;
                        options.Password.RequiredUniqueChars = 3;

                        options.SignIn.RequireConfirmedEmail = true;
                        options.Tokens.EmailConfirmationTokenProvider = "CustomEmailConfirmation";

                        options.Lockout.MaxFailedAccessAttempts = 5;
                        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    }
                ).AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<CustomEmailConfirmationTokenProvider<ApplicationUser>>("CustomEmailConfirmation");

            //for all the token types
            services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(5));
            services.Configure<CustomEmailConfirmationTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromDays(3));

            //services.AddMvc(options => options.EnableEndpointRouting = false).AddXmlSerializerFormatters();
            services.AddMvc(options =>
            {               
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                options.EnableEndpointRouting = false;
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddXmlSerializerFormatters();


            //https://console.developers.google.com/
            // https://developers.facebook.com
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = "588043458448-r90vtffemt42hm6fod1euqq4mf7t3m8r.apps.googleusercontent.com";
                    options.ClientSecret = "SwCJ4iqtFui7efxUFHE_UXRK";
                })
                .AddFacebook(options =>
                {
                    options.AppId = "axy";
                    options.AppSecret = "xtz";
                });

            

            services.ConfigureApplicationCookie(options => 
            {
                options.AccessDeniedPath = new PathString("/Administration/AccessDenied");
            });

            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("DeleteRolePolicy", policy => policy.RequireClaim("Delete Role"));
            //    //options.AddPolicy("EditRolePolicy", policy => policy.RequireClaim("Edit Role", "true"));
            //    //options.AddPolicy("DeleteRolePolicy", policy => policy.RequireClaim("Delete Role").RequireClaim("Create Role"));
            //    //options.AddPolicy("EditRolePolicy", policy => policy.RequireClaim("Edit Role", "true")
            //    //                                                     .RequireRole("Admin")
            //    //                                                     .RequireRole("Super Admin"));

            //    options.AddPolicy("EditRolePolicy", 
            //        policy => policy.RequireAssertion(context => 
            //        (context.User.IsInRole("Admin") && 
            //        context.User.HasClaim(claim => claim.Type == "Edit Role" && claim.Value == "true"))
            //        || 
            //        context.User.IsInRole("Super Admin")
            //    ));

            //    options.AddPolicy("AdminRolePolicy", policy => policy.RequireRole("Admin"));
            //});

            services.AddAuthorization(options =>
            {
                options.AddPolicy("DeleteRolePolicy", policy => policy.RequireClaim("Delete Role"));

                options.AddPolicy("EditRolePolicy",
                    policy => policy.AddRequirements(new ManageAdminRolesAndClaimsRequirement()));

                //options.InvokeHandlersAfterFailure = false;

                options.AddPolicy("AdminRolePolicy", policy => policy.RequireRole("Admin"));
            });


            services.AddScoped<IEmployeeRepository, SqlEmployeeRepository>();
            services.AddSingleton<IAuthorizationHandler, CanEditOnlyOtherAdminRolesAndClaimsHandler>();
            services.AddSingleton<IAuthorizationHandler, SuperAdminHandler>();
            services.AddSingleton<DataProtectionPurposeString>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            } else
            {
                app.UseExceptionHandler("/Error");
                app.UseStatusCodePagesWithReExecute("/Error/{0}");
                //app.UseStatusCodePagesWithRedirects("/Error/{0}");
            }

            app.UseStaticFiles();
            app.UseAuthentication();
            
            app.UseMvc(routes => {
                routes.MapRoute("default", "/{controller=Home}/{action=Index}/{id?}");
             });
            
        }
    }
}
