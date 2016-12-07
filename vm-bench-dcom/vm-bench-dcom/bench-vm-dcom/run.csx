using System;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Resource.Fluent;

public static void Run(string message, TraceWriter log)
{
    var rgName = myQueueItem.Replace("-vm", "");
    var client = System.Configuration.ConfigurationManager.AppSettings["ClientId"];
    var key = System.Configuration.ConfigurationManager.AppSettings["ClientKey"];
    var tenant = System.Configuration.ConfigurationManager.AppSettings["TenantId"];

    var credentials = AzureCredentials.fromServicePrincipal(client, key, tenant, AzureEnvironment.AZURE);
    var azure = Azure.authenticate(credentials).withSubscription(subscriptionId);
    azure.ResourceGroups.Delete(rgName2);
}