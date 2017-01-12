using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Antiforgery;
using Newtonsoft.Json;
using Roeds.Models;
using Roeds.Interfaces;
using Roeds.Data;
using Roeds.Middleware;

namespace Roeds {
    public class Startup {
        public IConfigurationRoot Configuration { get; }
        private static readonly string _secretKey = "!MySuperSecret_secretKey#1992";

        public Startup(IHostingEnvironment env) {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            // Add settings from appsettings.json to the services container
            services.Configure<Settings>(options => {
                options.ConenctionString = Configuration
                    .GetSection("MongoDB:ConnectionString")
                    .Value;

                options.Database = Configuration
                    .GetSection("MongoDB:Database")
                    .Value;
            });

            // Add in memory caching
            services.AddMemoryCache();

            // Add framework services.
            services.AddMvc()
            .AddJsonOptions(options => options
                .SerializerSettings
                .NullValueHandling = NullValueHandling.Ignore
            );

            // Add and allow Cross-Origin Resource Sharing
            services.AddCors(
                settings => settings
                .AddPolicy(
                    "DefaultPolicy",
                    builder => builder
                        // Which domains or IP-adresse to allow
                        .AllowAnyOrigin()
                        // Which HTTP methods to allow (e.g. GET, POST, PUT and DELETE)
                        .AllowAnyMethod()
                        // Which HTTP headers to allow
                        .AllowAnyHeader()
                        // Specifies whether and which credentials for authorization or/and authentication is allowed
                        .AllowCredentials()
                )
            );

            // Add dependencies to be injected
            // Singleton    -> Instantiated on first request and used repeatedly for every sub-sequent request
            // Scoped       -> Instantiated once for every request
            // Transient    -> Instantiated and injected all places the dependency is needed on every request
            services.AddScoped<IMongoContext, MongoContext>();
            services.AddScoped<IPropertyRepository, PropertyRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IAntiforgery antiforgery, ILoggerFactory logger) {
            logger.AddConsole(Configuration.GetSection("Logging"));
            logger.AddDebug();
            
            // Generate siging key from secret to hash tokens with
            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secretKey));
            
            var tokenValidationParameters = new TokenValidationParameters {
                // Validate the key used to sign the token
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                // Validate the issuer and make sure it match valid issuers
                ValidateIssuer = true,
                ValidIssuer = "Roeds",

                // Validate the audience 
                ValidateAudience = true,
                ValidAudience = "RoedsAudience",

                // Validate the token expiration time
                ValidateLifetime = true,

                // Clock drift
                ClockSkew = TimeSpan.Zero
            };

            app.UseJwtBearerAuthentication(new JwtBearerOptions {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                TokenValidationParameters = tokenValidationParameters
            });

            app.UseCookieAuthentication(new CookieAuthenticationOptions {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                AuthenticationScheme = "Cookie",
                CookieHttpOnly = true,
                //CookieSecure = true,
                CookieName = "access_token",
                TicketDataFormat = new JwtDataFormat(
                    SecurityAlgorithms.HmacSha256,
                    tokenValidationParameters
                )
            });

            var options = new Token {
                Issuer = "Roeds",
                Audience = "RoedsAudience",
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            };

            app.UseMiddleware<TokenMiddleware>(Options.Create(options));

            app.UseMvc();
            app.UseCors("DefaultPolicy");
            app.UseStatusCodePages();
        }
    }
}