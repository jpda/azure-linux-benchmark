param (
    [Parameter(Mandatory=$false)]
    
    [Parameter(Mandatory=$false)]
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "benchmark",
    [Parameter(Mandatory=$false)]
    [string]$VMNamePrefix = "jpd-",
    [Parameter(Mandatory=$false)]
    [string]$Sizes = "Basic_A1,Basic_A2,Standard_A2m_v2,Standard_A4m_v2,Standard_D1_v2,Standard_F4",
    [Parameter(Mandatory=$false)]
    [string]$Location = "East US 2",
    [Parameter(Mandatory=$false)]
    [string]$StorageType = "Standard_LRS",
    [Parameter(Mandatory=$false)]
    [string]$VNetName = "bench-net",
    [Parameter(Mandatory=$false)]
    [string]$SubnetName = "Subnet-1",
    [Parameter(Mandatory=$false)]
    [string]$UserName = "jpd"
)

Login-AzureRmAccount
Select-AzureRmSubscription -SubscriptionName $SubscriptionName
#$ResultsStorageAccount = New-AzureRmStorageAccount -ResourceGroupName $ResourceGroupName -Name jpdbenchresult -Type $StorageType -Location $Location

## Network
$VNetAddressPrefix = "10.0.0.0/16"
$VNetSubnetAddressPrefix = "10.0.0.0/24"

# Resource Group - should check here too
New-AzureRmResourceGroup -Name $ResourceGroupName -Location $Location

# Network - should probably check if exists here to make it easy to redeploy into existing
$SubnetConfig = New-AzureRmVirtualNetworkSubnetConfig -Name $SubnetName -AddressPrefix $VNetSubnetAddressPrefix
$VNet = New-AzureRmVirtualNetwork -Name $VNetName -ResourceGroupName $ResourceGroupName -Location $Location -AddressPrefix $VNetAddressPrefix -Subnet $SubnetConfig

$vmSizes = $Sizes.Split(',')

$keyData = [System.IO.File]::ReadAllText($PubFilePath)

foreach($size in $vmSizes){
    Write-Host Creating $size VM...

    ## Compute
    $safeSize = $size.Replace("_", "-")
    # make sure VMName in Azure and Computer name match!
    $VMName = $VMNamePrefix + $safeSize
    $ComputerName = $VMName
    $VMSize = $size
    $OSDiskName = $VMName + "OSDisk"
    $StorageAccountName = $VMName.ToLower().Replace("-", "")
    $InterfaceName = $VMName + "nic"

    # Storage
    $StorageAccount = New-AzureRmStorageAccount -ResourceGroupName $ResourceGroupName -Name $storageAccountName -Type $StorageType -Location $Location

    # Network
    $PIp = New-AzureRmPublicIpAddress -Name $InterfaceName -ResourceGroupName $ResourceGroupName -Location $Location -AllocationMethod Dynamic -DomainNameLabel $VMName.ToLower()
    $Interface = New-AzureRmNetworkInterface -Name $InterfaceName -ResourceGroupName $ResourceGroupName -Location $Location -SubnetId $VNet.Subnets[0].Id -PublicIpAddressId $PIp.Id

    # Compute
    $VirtualMachine = New-AzureRmVMConfig -VMName $VMName -VMSize $VMSize
    $cred = New-Object System.Management.Automation.PSCredential($UserName, (New-Object System.Security.SecureString))
    $VirtualMachine = Set-AzureRmVMOperatingSystem -VM $VirtualMachine -ComputerName $ComputerName -Linux -Credential $cred -DisablePasswordAuthentication
    $VirtualMachine = Set-AzureRmVMSourceImage -VM $VirtualMachine -PublisherName "Canonical" -Offer "UbuntuServer" -Skus "16.04-LTS" -Version "latest"
    $VirtualMachine = Add-AzureRmVMNetworkInterface -VM $VirtualMachine -Id $Interface.Id
    $OSDiskUri = $StorageAccount.PrimaryEndpoints.Blob.ToString() + "vhds/" + $OSDiskName + ".vhd"
    $VirtualMachine = Set-AzureRmVMOSDisk -VM $VirtualMachine -Name $OSDiskName -VhdUri $OSDiskUri -CreateOption FromImage -Caching ReadWrite
    Add-AzureRmVMSshPublicKey -VM $VirtualMachine -KeyData $keyData -Path "/home/$userName/.ssh/authorized_keys"
    Write-Host Getting ready to create VM $VMName...
    
    ## Create the VM in Azure
    New-AzureRmVM -ResourceGroupName $ResourceGroupName -Location $Location -VM $VirtualMachine

    ## Set the custom script extension
    $PublicConf = (Get-Content "deploy/public-config.json" -Raw)
    $PrivateConf = (Get-Content "deploy/private-config.json" -Raw)
    $ExtensionName = 'CustomScript'
    $Publisher = 'Microsoft.Azure.Extensions'
    $Version = '2.0'
    Set-AzureRmVMExtension -ResourceGroupName $RGName -VMName $VmName -Location $Location `
        -Name $ExtensionName -Publisher $Publisher `
        -ExtensionType $ExtensionName -TypeHandlerVersion $Version `
        -SettingString $PublicConf -ProtectedSettingString $PrivateConf
}
Write-Host All done.