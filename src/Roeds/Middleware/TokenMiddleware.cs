﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Roeds.Models;

namespace Roeds.Middleware {
    public class TokenMiddleware {
        private readonly RequestDelegate _next;
        private readonly Token _options;

        public TokenMiddleware(RequestDelegate next, IOptions<Token> options) {
            _next = next;
            _options = options.Value;
        }

        public Task Invoke(HttpContext context) {
            // If the request path doesn't match, skip
            if (!context.Request.Path.Equals(_options.Path, StringComparison.Ordinal)) {
                return _next(context);
            }

            // Request must be POST with Content-Type: application/x-www-form-urlencoded
            if (!context.Request.Method.Equals("POST") || !context.Request.HasFormContentType) {
                context.Response.StatusCode = 400;
                return context.Response.WriteAsync("Bad request.");
            }

            return GenerateToken(context);
        }

        private async Task GenerateToken(HttpContext context) {
            var username = context.Request.Form["username"];
            var password = context.Request.Form["password"];

            var identity = await GetIdentity(username, password);

            if (identity == null) {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid username or password.");
                return;
            }

            var now = DateTime.UtcNow;

            // Specifically add the jti (random nonce), iat (issued timestamp), and sub (subject/user) claims.
            // You can add other claims here, if you want:
            var claims = new Claim[] {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(now).ToString(), ClaimValueTypes.Integer64)
            };

            // Create the JWT and write it to a string
            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: now.Add(_options.Expiration),
                signingCredentials: _options.SigningCredentials
            );

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            context.Response.Cookies.Append("access_token", encodedJwt, new CookieOptions {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddMinutes(5)
            });

            var response = new {
                access_token = encodedJwt,
                expires_in = (int) _options.Expiration.TotalSeconds
            };

            // Serialize and return the response
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonConvert.SerializeObject(
                    response,
                    new JsonSerializerSettings {
                        Formatting = Formatting.Indented
                    }
                )
            );
        }

        private Task<ClaimsIdentity> GetIdentity(string username, string password) {
            // DON'T do this in production, obviously!
            if (username == "TEST" && password == "TEST123")
                return Task.FromResult(
                    new ClaimsIdentity(
                        new System
                            .Security
                            .Principal
                            .GenericIdentity(username, "Token"),
                        new Claim[] { }
                    )
                );

            // Credentials are invalid, or account doesn't exist
            return Task.FromResult<ClaimsIdentity>(null);
        }

        public static long ToUnixEpochDate(DateTime date) => new DateTimeOffset(date).ToUniversalTime().ToUnixTimeSeconds();
    }
}
