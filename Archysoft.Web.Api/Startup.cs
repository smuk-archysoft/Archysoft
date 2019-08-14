﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Archysoft.Data;
using Archysoft.Data.Entities;
using Archysoft.Data.Repositories.Abstract;
using Archysoft.Data.Repositories.Concrete;
using Archysoft.Domain.Model.Mappings;
using Archysoft.Domain.Model.Model.Auth;
using Archysoft.Domain.Model.Model.Settings;
using Archysoft.Domain.Model.Services.Abstract;
using Archysoft.Domain.Model.Services.Concrete;
using Archysoft.Web.Api.Utilities.Filters;
using Archysoft.Web.Api.Utilities.Middleware;
using AutoMapper;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Archysoft.Web.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private readonly ISettingsService _settingsService;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var jwtSettings = new JwtSettings();
            Configuration.Bind(nameof(JwtSettings), jwtSettings);

            var emailNotificationSettings = new EmailNotificationSettings();
            Configuration.Bind(nameof(EmailNotificationSettings), emailNotificationSettings);

            var uiUrlSettings = new UIUrlSettings();
            Configuration.Bind(nameof(uiUrlSettings), uiUrlSettings);

            _settingsService = new SettingsService(jwtSettings, emailNotificationSettings,uiUrlSettings);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DataContext>();

            services.AddIdentity<User, Role>(opts =>
                {
                    opts.Password.RequiredLength = 1;
                    opts.Password.RequireNonAlphanumeric = false;
                    opts.Password.RequireLowercase = false;
                    opts.Password.RequireUppercase = false;
                    opts.Password.RequireDigit = false;
                    opts.SignIn.RequireConfirmedEmail = true;
                    opts.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<DataContext>()
                .AddDefaultTokenProviders();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _settingsService.JwtSettings.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settingsService.JwtSettings.Key)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddCors();
            services.AddResponseCaching();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();
            services.AddSingleton(Log.Logger);

            services.AddSingleton<IMapper>(new Mapper(new MapperConfiguration(options =>
            {
                options.CreateMissingTypeMaps = false;
                options.AddProfile<UserMapping>();
                options.AddProfile<EmployeeMapping>();
                options.AddProfile<EducationMapping>();
                options.AddProfile<ExperienceMapping>();
            })));

            services.AddMvc(config =>
            {
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                config.Filters.Add(new AuthorizeFilter(policy));
                config.Filters.Add(new ValidateModelStateFilter());
            }).AddFluentValidation(x => x.RegisterValidatorsFromAssemblyContaining<LoginModel>())
             /* .AddJsonOptions(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore)*/;

         
            // Services
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IEmailNotificationService, EmailNotificationService>();
            services.AddTransient<IEmployeesService, EmployeesService>();
            services.AddTransient<IPdfService, PdfService>();
            services.AddTransient<IUserService, UserService>();
            services.AddSingleton(new Sha256Encryption());

            // Repositories
            services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
            services.AddTransient<IUserRepository, UserRepository>();

            // Settings
            services.AddSingleton(_settingsService);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IDatabaseInitializer databaseInitializer)
        {
            app.Use(async (ctx, next) =>
            {
                await next();
                if (ctx.Response.StatusCode == 204)
                {
                    ctx.Response.ContentLength = 0;
                }
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                databaseInitializer.Initialize();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("X-File-Name"));
            app.UseAuthentication();             
            app.UseMiddleware(typeof(ErrorHandlingMiddleware));
            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
