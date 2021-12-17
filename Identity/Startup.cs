// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using IdentityServerHost.Quickstart.UI;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore.Design;

namespace MicroTest1
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            var connectionString = Configuration.GetConnectionString("DefaultConnection");

            var builder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                options.EmitStaticAudienceClaim = true;
            })
                .AddTestUsers(TestUsers.Users)
                // this adds the config data from DB (clients, resources, CORS)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = builder => builder.UseNpgsql(connectionString);
                })
                // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = builder => builder.UseNpgsql(connectionString);

                    // this enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                });

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();


        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                // app.UseDatabaseErrorPage();
            }

            app.UseStaticFiles();

            app.UseRouting();
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }

        public class ConfigurationContextFactory : IDesignTimeDbContextFactory<ConfigurationDbContext>
        {
            public ConfigurationDbContext CreateDbContext(string[] args)
            {
                var optionsBuilder = new DbContextOptionsBuilder<ConfigurationDbContext>();
                optionsBuilder.UseNpgsql("Host=localhost;port=5432;database=postgres;username=postgres;password=example;", 
                    sql => sql.MigrationsAssembly(typeof(SeedData).Assembly.FullName));
 

                return new ConfigurationDbContext(optionsBuilder.Options,new IdentityServer4.EntityFramework.Options.ConfigurationStoreOptions());
            }
        }

        public class PersistedGrantContextFactory : IDesignTimeDbContextFactory<PersistedGrantDbContext>
        {
            public PersistedGrantDbContext CreateDbContext(string[] args)
            {
                var optionsBuilder = new DbContextOptionsBuilder<PersistedGrantDbContext>();
                optionsBuilder.UseNpgsql("Host=localhost;port=5432;database=postgres;username=postgres;password=example;",
                    sql => sql.MigrationsAssembly(typeof(SeedData).Assembly.FullName));


                return new PersistedGrantDbContext(optionsBuilder.Options, new IdentityServer4.EntityFramework.Options.OperationalStoreOptions());
            }
        }
    }
}
