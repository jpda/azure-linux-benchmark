using System;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Resource.Fluent;
using Microsoft.Azure.Management.Resource.Fluent.Authentication;

public static void Run(string message, TraceWriter log)
{
    log.Info($"Received {message}");

    //assume AWS if i-. should probably figure out a better way to do this but c'est la vie
    if (message.StartsWith("i-"))
    {
        TerminateAwsInstance(message, log);
        return;
    }

    var rgName = message.Replace("-vm", "");
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

public static void TerminateAwsInstance(string instanceId, TraceWriter log)
{
    var request = new TerminateInstancesRequest { InstanceIds = new List<string>() { instanceId } };

    try
    {
        var awsId = System.Configuration.ConfigurationManager.AppSettings["AwsId"];
        var awsKey = System.Configuration.ConfigurationManager.AppSettings["AwsKey"];

        var awsCredentials = new BasicAWSCredentials(awsId, awsKey);
        // TODO: make region configurable
        var ec2Client = new AmazonEC2Client(awsCredentials, RegionEndpoint.USEast1);
        var response = ec2Client.TerminateInstances(request);
        foreach (var item in response.TerminatingInstances)
        {
            log.Info("Terminated instance: " + item.InstanceId);
            log.Info("Instance state: " + item.CurrentState.Name);
        }
    }
    catch (AmazonEC2Exception ex)
    {
        if ("InvalidInstanceID.NotFound" == ex.ErrorCode)
        {
            log.Info("Instance {0} does not exist.", instanceId);
        }
        else
        {
            log.Error($"Blow'd up: {ex.Message}");
        }
    }
}