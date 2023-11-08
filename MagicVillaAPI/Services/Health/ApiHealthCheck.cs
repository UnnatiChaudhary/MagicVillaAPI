using Azure.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RestSharp;

namespace MagicVillaAPI.Services.Health
{
        public class ApiHealthCheck : IHealthCheck
        {
            public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            {
                var url = "http://localhost:5066/apihealth";

                var client = new RestClient();

                // making request
                var request = new RestRequest(url, Method.Get);
                //request.AddHeader("X-RapidAPI-Key", "2f137258e8mshea440cda633b608p1a0a0bjsn79357f76b228");
               // request.AddHeader("X-RapidAPI-Host", "chuck-norris-jokes.p.rapidapi.com");

                var response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    return Task.FromResult(HealthCheckResult.Healthy());
                }
                else
                    return Task.FromResult(HealthCheckResult.Unhealthy());
            }
        }
    }
