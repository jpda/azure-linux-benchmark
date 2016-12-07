using System;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Resource.Fluent;
using Microsoft.Azure.Management.Resource.Fluent.Authentication;

public static void Run(string message, TraceWriter log)
{
    log.Info($"Received {message}");

    var rgName = message.Replace("-vm", "");
    //assume AWS if ip-. should probably figure out a better way to do this but c'est la vie
    if (rgName.StartsWith("ip-"))
    {
        //TODO: decom @ aws
        log.Info("AWS, you should manually turn this off ASAP. exiting...");
        return;
    }

    log.Info($"Attempting to delete resource group {rgName}...");
    var client = System.Configuration.ConfigurationManager.AppSettings["ClientId"];
    var key = System.Configuration.ConfigurationManager.AppSettings["Key"];
    var tenant = System.Configuration.ConfigurationManager.AppSettings["TenantId"];
    var subscriptionId = System.Configuration.ConfigurationManager.AppSettings["SubscriptionId"];

    try
    {
        var credentials = AzureCredentials.FromServicePrincipal(client, key, tenant, AzureEnvironment.AzureGlobalCloud);
        var azure = Azure.Authenticate(credentials).WithSubscription(subscriptionId);
        azure.ResourceGroups.Delete(rgName);
        log.Info($"Deleted {rgName}");
    }
    catch (Exception ex)
    {
        log.Error($"Blow'd up: {ex.Message}");
    }
}