using System.Text;
using Newtonsoft.Json;
using Nexus.Models;

namespace Nexus.Services;

public class ConsulApiService
{
    private const string PolicyApiUrl = "http://localhost:8500/v1/acl/policy";
    private const string TokenApiUrl = "http://localhost:8500/v1/acl/token";
    private const string KvApiUrl = "http://localhost:8500/v1/kv/{0}/app-config";
    
    public PolicyCreationResult CreateConsulPolicy(string globalToken, string rules, string serviceName)
    {
        HttpClient? client = GetConsulHttpClient(globalToken);
        PolicyCreationResult policy = GetConsulPolicyBody(serviceName, rules);
        StringContent content = new (policy.Json, Encoding.UTF8, "application/json");
        HttpResponseMessage response = client.PutAsync(PolicyApiUrl, content).Result;
        string responseContent = response.Content.ReadAsStringAsync().Result;
        dynamic? jsonResponse = JsonConvert.DeserializeObject(responseContent);

        if (jsonResponse == null)
        {
            return new PolicyCreationResult();
        }
        
        policy.Id = jsonResponse.ID;
        
        return policy;
    }

    private static HttpClient GetConsulHttpClient(string globalToken)
    {
        HttpClient client = new ();
        client.DefaultRequestHeaders.Add("X-Consul-Token", globalToken);
        return client;
    }
    
    private static PolicyCreationResult GetConsulPolicyBody(string serviceName, string rules)
    {
        string policyName = $"kv-{serviceName}";
        var bodyJson = new
        {
            Name = policyName,
            Description = $"Policy for {serviceName} key prefix",
            Rules = rules
        };

        return new PolicyCreationResult
        {
            Json = JsonConvert.SerializeObject(bodyJson),
            Name = policyName,
        };
    }

    private static string GetConsulTokenBody(string serviceName, string policyName)
    {
        var body = new
        {
            Description = $"Token for {serviceName} service",
            Policies = new[]
            {
                new
                {
                    Name = policyName,
                },
            },
            ServiceIdentities = new[]
            {
                new
                {
                    ServiceName = serviceName,
                },
            },
        };

        return JsonConvert.SerializeObject(body);
    }

    public string CreateToken(string globalToken, string serviceName, string policyName)
    {
        HttpClient? client = GetConsulHttpClient(globalToken);
        string? tokenJson = GetConsulTokenBody(serviceName, policyName);
        
        StringContent content = new (tokenJson, Encoding.UTF8, "application/json");
        HttpResponseMessage response = client.PutAsync(TokenApiUrl, content).Result;
        string responseContent = response.Content.ReadAsStringAsync().Result;
        dynamic? jsonResponse = JsonConvert.DeserializeObject(responseContent);

        return jsonResponse?.SecretID ?? string.Empty;
    }

    public void UploadKv(string serviceName, string json, string globalToken)
    {
        HttpClient? client = GetConsulHttpClient(globalToken);
        StringContent content = new (json, Encoding.UTF8, "application/json");
        string kvUrl = string.Format(KvApiUrl, serviceName);
        HttpResponseMessage response = client.PutAsync(kvUrl, content).Result;
        string responseContent = response.Content.ReadAsStringAsync().Result;
        dynamic? jsonResponse = JsonConvert.DeserializeObject(responseContent);
    }
}