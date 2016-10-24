using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Yaml;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class RoutingTests : IDisposable
    {
        private IWebHost _builder;
        private readonly Steps _steps;

        public RoutingTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_response_404_when_no_configuration_at_all()
        {
            this.Given(x => _steps.GivenThereIsAConfiguration(new YamlConfiguration()))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url()
        {
            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:51879/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get",
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879", 200, "Hello from Laura"))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_complex_url()
        {
            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:51879/api/products/{productId}",
                            UpstreamTemplate = "/products/{productId}",
                            UpstreamHttpMethod = "Get"
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879/api/products/1", 200, "Some Product"))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/products/1"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Some Product"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_201_with_simple_url()
        {
            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:51879/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Post"
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879", 201, string.Empty))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenThePostHasContent("postContent"))
                .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode, string responseBody)
        {
            _builder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.StatusCode = statusCode;
                        await context.Response.WriteAsync(responseBody);
                    });
                })
                .Build();

            _builder.Start();
        }

        public void Dispose()
        {
            _builder?.Dispose();
            _steps.Dispose();
        }
    }
}