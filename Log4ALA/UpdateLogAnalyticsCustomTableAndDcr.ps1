# **********************
# VAR DEFINITIONS
# **********************


# If you want to add new table columns to the current table schema:
# 1) set $saveCurrentTableSchema2File to $true and run this script to get the current table schema
# 2) now modify (add columns) the curent table schema file $TableSchemaFilePath and the data collection rule (dcr) definition file $DcrFilePath
# 3) set $saveCurrentTableSchema2File to $false and run the script to update the table schema and dcr rule definition
# 4) check table and depending data collection rule (dcr) definition if the update succeeded via Azure Portal
# 5) now set $saveCurrentTableSchema2File back to $true 

$saveCurrentTableSchema2File = $true

# Azure Login mechanism to run the script
$userManagedIdentity = $true

# if $isManagedIdentityLogin is true set the $userManagedIdentity  
$userManagedIdentity = "YOUR_USER_MG_IDENTIY"

# if $isManagedIdentityLogin is false set azure $azureCredUser + $azureCredPwd  
$azureCredUser = "YOUR_AZURE_LOGIN_USER"
$azureCredPwd = "YOUR_AZURE_LOGIN_USER_PASSWORD"


# Azure subscription id of which contains the log analytics custom table and dcr rule
$subscriptionId = "YOUR_AZURE_SUBSCRIPTION_ID"

# Azure resource group name of the log analytics custom table and data collection rule (dcr)
# both in the same resourc group
$resourceGroupName = "YOUR_RESOURCE_GROUP_NAME"

# Azure Log Analytics Workspace name which contains the log analytics custom table
$workSpace = "YOUR_LOG_ANALYTICS_WORKSPACE_NAME"

# log analytics custom table name with _CL suffix
$dcrTable = "YOUR_LOG_ANALYTICS_TABLE_NAME_WITH_CL_SUFFIX"

# name of the data collection rule (dcr)
$dcrName = "YOUR_DCR_RULE_NAME"




# **********************
# FUNCTION DEFINITIONS
# **********************


function Log([string] $msg){
    Write-Host "$([System.DateTime]::Now) - $msg"
}

function DoAzureUserMgmtIdentityLoginWithSub([string]$userManagedIdentity, [string]$subscriptionId, [bool]$reconnect = $false){

    	if($reconnect -or !(Get-AzContext) -or (Get-AzContext).Account.Id -ne $userManagedIdentity){
		# sign in
		Log "Clear-AzContext -Scope Process -Force..."
		Clear-AzContext -Scope Process -Force

		Log "Logging in (identity) ...";
		Connect-AzAccount -Scope Process -Identity -AccountId $userManagedIdentity
	}else{
		Log "Identity '$userManagedIdentity' already logged in";
	}
	# select subscription
	Log "Selecting subscription '$subscriptionId'";
		
	Try{
		Set-AzContext -Scope Process -Subscription $subscriptionId  -ErrorAction Stop
	}
	Catch
	{
        	Log "Clear-AzContext -Scope Process -Force..."
		Clear-AzContext -Scope Process -Force
		DoAzureUserMgmtIdentityLoginWithSub -userManagedIdentity $userManagedIdentity -subscriptionId $subscriptionId
	}

	return $true

}

function DoAzureUserLoginWithSub([string]$subscriptionId, [string]$azureCredUser, [string]$azureCredPwd){

	# sign in
	Log "Logging in (user)...";

	try{
		$azureRMCtx = Get-AzContext -ErrorAction SilentlyContinue
	}catch{
		#continue
	}

	if(!$azureRMCtx -or 
		!$azureRMCtx.Account -or 
		!$azureRMCtx.Account.Id -or 
		($azureRMCtx.Account.Id -ne $azureCredUser)){

		$securePassword = ConvertTo-SecureString -String "$azureCredPwd" -AsPlainText -Force;
		$cred = New-Object System.Management.Automation.PSCredential($azureCredUser, $securePassword);
		Connect-AzAccount -Credential $cred;

	}else{
		Log "already logged in to [$($azureRMCtx.Account.Id)]"               
	}


	# select subscription
	Log "Selecting subscription '$subscriptionId'";
	
	Try{
		Set-AzContext -Subscription $subscriptionId  -ErrorAction Stop
	}
	Catch
	{
        	Log "Clear-AzContext -Scope Process -Force..."
		Clear-AzContext -Scope Process -Force
		DoAzureUserLoginWithSub -subscriptionId $subscriptionId -azureCredUser $azureCredUser -azureCredPwd $azureCredPwd
	}

	return $true

}



# **********************
# MAIN SCRIPT 
# **********************

$saveDCRDefinition = $saveCurrentTableSchema2File


$tableParams = @'
{
    "properties": {
        "schema": {
            "name": "-TABLE_NAME-"
        }
    }
}
'@

$tableParams = $tableParams.Replace("-TABLE_NAME-", $dcrTable)
$ResourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Insights/dataCollectionRules/$dcrName" # Resource ID of the DCR to edit
$DcrFilePath = "$PSScriptRoot\$dcrName.json" # File to store DCR content
$TableSchemaFilePath = "$PSScriptRoot\$dcrTable.json" # File to store DCR content

if($userManagedIdentity){
	DoAzureUserMgmtIdentityLoginWithSub -userManagedIdentity $userManagedIdentity -subscriptionId $subscriptionId -reconnect $false
}else{
	DoAzureUserLoginWithSub -subscriptionId $subscriptionId -azureCredUser $azureCredUser -azureCredPwd $azureCredPwd
}


#https://learn.microsoft.com/en-us/azure/azure-monitor/essentials/data-collection-rule-create-edit?tabs=powershell




if($saveCurrentTableSchema2File){

    write-warning "get current table schema via Azure REST API"
    # Get current table schema via Azure REST API
    $tables = (Invoke-AzRestMethod -Path "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/microsoft.operationalinsights/workspaces/$workSpace/tables/$($dcrTable)?api-version=2021-12-01-preview" -Method GET -payload $tableParams).Content  |  ConvertFrom-Json

}else{

    write-warning "Get current table schema via local file"
    # Get current table schema via local file 
    $tables = Get-Content -Raw -Path $TableSchemaFilePath | ConvertFrom-Json 

}

$tableParamsNew = @'
{
    "properties": {
        "schema": {
            "name": "-TABLE_NAME-",
            "columns" : "-COLUMNS-"
        }
    }
}
'@


$tableJson = ($tableParamsNew).Replace("""-COLUMNS-""", ($tables.properties.schema.columns  | ConvertTo-Json)).Replace("-TABLE_NAME-", $dcrTable)

if($saveCurrentTableSchema2File){

    write-warning "Save current tabel schema to file $TableSchemaFilePath"
    # Save current tabel schema to file $TableSchemaFilePath
    $tableJson | ConvertFrom-Json | ConvertTo-Json -Depth 20 | Out-File -FilePath $TableSchemaFilePath
    if($saveDCRDefinition){
        $DCR = Invoke-AzRestMethod -Path ("$ResourceId"+"?api-version=2023-03-11") -Method GET
        $DCR.Content | ConvertFrom-Json | ConvertTo-Json -Depth 20 | Out-File -FilePath $DcrFilePath
    }
}


if(!$saveCurrentTableSchema2File){

    write-warning "update table schema"
    # Update analytics table schema definition in $TableSchemaFilePath  and run the following command:
    Invoke-AzRestMethod -Path "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/microsoft.operationalinsights/workspaces/$workSpace/tables/$($dcrTable)?api-version=2021-12-01-preview" -Method PUT -payload $tableJson


    write-warning "update dcr schema"
    # Update DCR definition in $DcrFilePath and run the following command: 
    New-AzDataCollectionRule -Name $dcrName -ResourceGroupName $resourceGroupName -JsonFilePath $DcrFilePath

}