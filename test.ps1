param(
    [Parameter(Mandatory=$false)]
    [string]$sizeList = "Standard_D2_v2"
)

$vmSizes = $sizeList.Split(",")
foreach($size in $vmSizes){
    Start-Job -ScriptBlock {
        param($s)
        $cred = New-Object System.Management.Automation.PSCredential(<service principal>)
        Login-AzureRmAccount -Credential $cred -ServicePrincipal -TenantId <tenant>
        $rgName = "jpd-" + ($s.Replace("_","-"))
        Write-Host Creating $rgName...
        New-AzureRmResourceGroup -Name $rgName -Location "East US 2" -Force
        New-AzureRmResourceGroupDeployment -ResourceGroupName $rgName -TemplateFile D:\code2\git\azure-linux-benchmark\arm\create-azure-vm-rg-arm.json `
            -rg_name $rgName `
            -vm_size $s `
            -TemplateParameterFile D:\code2\git\azure-linux-benchmark\arm\create-azure-vm-rg-arm.param.json `
            -Verbose
    } -ArgumentList $size
}