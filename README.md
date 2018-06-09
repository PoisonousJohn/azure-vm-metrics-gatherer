# Azure VM Metrics Gatherer

This app gathers metrics (now only CPU Percentage), from all VMs you have on the subscription.

## Usage

```
$ dotnet publish
$  AZURE_CLIENT_ID="yourClientId" AZURE_CLIENT_SECRET="yourClientSecret" AZURE_TENANT_ID="yourTenantId" AZURE_SUBSCRIPTION_ID="yourSubscriptionId" dotnet bin/Debug/netcoreapp2.1/azure-vm-metrics-gatherer.dll > metrics.csv
```

It will spit out flat csv of metrics that perfectly fits for PowerBI.