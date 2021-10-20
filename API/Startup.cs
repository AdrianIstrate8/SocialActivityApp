using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Extensions;
using API.Middleware;
using API.SignalR;
using Application.Activities;
using Application.Core;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Persistence;

namespace API
{
    public class Startup
    {
        //Constructor where is specified that we want our configuration to be injected into Startup class
        //The configuration, when injected, gives us access to anything that we specify in our 
        //configuration files (appsettings.Development.json) whitch is a key:value pairs
        private readonly IConfiguration _config;
        public Startup(IConfiguration config)
        {
            _config = config;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // This container is referred to as the dependency injection container.
        // If we want to add something that we want to use or inject into another class in the application
        // then we typically add it as a service inside this container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(opt =>
            {
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                opt.Filters.Add(new AuthorizeFilter(policy));
            })
            .AddFluentValidation(config =>
            {
                config.RegisterValidatorsFromAssemblyContaining<Create>();
            });

            services.AddApplicationsServices(_config);

            services.AddIdentityServices(_config);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        // Which is an API project that receive http requests and return http responses and if we want to add
        // middleware so we can do something with either the request or response as it makes its way to the pipeline,
        // then we can add them inside to this Configure method
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<ExceptionMiddleware>();

            app.UseXContentTypeOptions();
            //our browser will not send any referreal information
            app.UseReferrerPolicy(opt => opt.NoReferrer());
            //gives us cross site scripting protection
            app.UseXXssProtection(opt => opt.EnabledWithBlockMode());
            //prevent the application to be used in an iframe
            app.UseXfo(opt => opt.Deny());
            //content security policy
            app.UseCsp(opt => opt
                //we won't be able to have http and https content on our site
                //it's all gonna be https
                .BlockAllMixedContent()
                //where our css, font, formactions, frame, image, scripts come from
                //self = the domein where is comming from
                .StyleSources(s => s.Self().CustomSources(
                    "https://fonts.googleapis.com",
                    "sha256-yChqzBduCCi4o4xdbXRXh4U/t1rP4UUUMJt+rB+ylUI=",
                    "sha256-r3x6D0yBZdyG8FpooR5ZxcsLuwuJ+pSQ/80YzwXS5IU="
                ))
                .FontSources(s => s.Self().CustomSources("https://fonts.gstatic.com", "data:"))
                .FormActions(s => s.Self())
                .FrameAncestors(s => s.Self())
                .ImageSources(s => s.Self().CustomSources(
                    "https://res.cloudinary.com",
                    "https://www.facebook.com",
                    "https://scontent.fotp3-3.fna.fbcdn.net",
                    "https://platform-lookaside.fbsbx.com",
                    "data:"
                ))
                .ScriptSources(s => s.Self().CustomSources(
                    "https://connect.facebook.net",
                    "sha256-TT5oUAYpNdnopISKk/6Bwz8ou7fhRfEHihuGjlEfxyo=",
                    "sha256-GV9857QR1ujpaNK/nwtQlVtK29kJn6FOKgnAEIhQ06k="
                ))
            );

            if (env.IsDevelopment())
            {

                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1"));
            }
            else
            {
                //a header for strict transport security
                //and we enable this only in production because browsers will cache this information
                app.Use(async (context, next) =>
                {
                    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
                    await next.Invoke();
                });
            }

            app.UseHttpsRedirection();

            // We need to route this requests to the apropiate API controller
            app.UseRouting();

            //default files is going to look for anything inside the wwwroot folder that is called index.html
            app.UseDefaultFiles();

            //support for serving static files
            app.UseStaticFiles();

            app.UseCors("CorsPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ChatHub>("/chat");
                endpoints.MapFallbackToController("Index", "Fallback");
            });
        }
    }
}
