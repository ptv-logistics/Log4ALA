# **********************
# VAR DEFINITIONS
# **********************


# How to create initial:
# ------------------------
# log analytics custom table, data collection rule (dcr) and data collection endpoint (dce)
# 1) set your values under the REQUIRED SETTINGS section
# 2) set $saveCurrentTableSchema2File to $false
# 3) run this Powershell script
# 4) if the script succeeded you will find a custom table schema definition script/$name_CL.json and 
#    data collection rule definition script/$name_DCR file in the script folder with example columns for 
#    later modification the column TimeGenerated of type datetime is requird and
#    should't be removed the 3 other columns can be removed in script/$name_CL.json and script/$name_DCR with your own column definitions.
# 5) you will also find the dependent Azure Resources in the Azure Portal:
#     => the log analytics custom table      ... Log Analytics Workspaces/$workSpaceName/Tables/$name_CL
#     => the data collection rule (dcr)      ... Data Collection rules/$name_DCR
#     => the data collection endpoint (dce)  ... Data Collection endpoints/$name-DCE
# 6) there is also logfile script_dcCrud.ps1.log under script/



# How to update already created table dcr and dce:
# ------------------------------------------------
# If you want to add new table columns to the current table schema:
# 1) set $saveCurrentTableSchema2File to $true and run this script to get the current table schema
# 2) now modify (add/remove columns) the curent table schema file script/$name_CL.json and the 
#    data collection rule (dcr) definition file script/$name_DCR
# 3) set $saveCurrentTableSchema2File to $false and run the script to update the table schema and dcr rule definition
# 4) check table Log Analytics (Workspaces/$workSpaceName/Tables/$name_CL) and 
#    depending data collection rule (Data Collection rules/$name_DCR) definition if the update succeeded via Azure Portal
# 5) now set $saveCurrentTableSchema2File back to $true 
# 6) there is also logfile script_dcCrud.ps1.log under script/



# !!!!!!!!!!
# Important:
# !!!!!!!!!!
# 
# 1) avoid to modify the template files templateTable.json and templateDataCollectionRule.json
# 2) don't remove the required column TimeGenerated of type datetime.





# **********************
# REQUIRED SETTINGS START
# **********************

# Use indentity or user login
$saveCurrentTableSchema2File = $false

# Azure Login mechanism to run the script
$isUserManagedIdentity = $true

# if $isManagedIdentityLogin is true set the $userManagedIdentity client id
$userManagedIdentity = "YOUR_USER_MG_IDENTIY_CLIENT_ID"

# if $isManagedIdentityLogin is false set azure $azureCredUser + $azureCredPwd  
$azureCredUser = "YOUR_AZURE_LOGIN_USER"
$azureCredPwd = "YOUR_AZURE_LOGIN_USER_PASSWORD" # clear text pwd


# Azure subscription id of which contains the log analytics custom table and dcr rule
$subscriptionId = "YOUR_AZURE_SUBSCRIPTION_ID"

# Azure resource group name of the log analytics custom table and data collection rule (dcr)
# both in the same resourc group
$resourceGroupName = "YOUR_RESOURCE_GROUP_NAME"

# log analytics workspace name and id which contains the log analytics custom table
$workSpaceName = "YOUR_LOG_ANALYTICS_WORKSPACE_NAME"
$workSpaceId = "YOUR_LOG_ANALYTICS_WORKSPACE_ID"

# global name
$name = "YOUR_GLOBAL_NAME"

# **********************
# REQUIRED SETTINGS END
# **********************





# log analytics custom table name with _CL suffix
$dcrTable = "$($name)_CL"

# name of the data collection rule (dcr)
$dcrName = "$($name.TrimEnd("_DCR"))_DCR"

# name of the data collection endpoint (dce)
$dcEndpointName = "$($name)-DCE"


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
# Resource ID of the log analytics workspace
$workspaceResourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/microsoft.operationalinsights/workspaces/$workSpaceName"
# Resource ID of the log analytics custom table
$tableResourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/microsoft.operationalinsights/workspaces/$workSpaceName/tables/$($dcrTable)"
# Resource ID of the data collection rule
$dcrResourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Insights/dataCollectionRules/$dcrName" # Resource ID of the DCR to edit
# Resource ID of the data collection endpoint
$dceResourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/microsoft.insights/dataCollectionEndpoints/$dcEndpointName"
# File to store the data collection rule (dcr) schema definition
$DcrFilePath = "$PSScriptRoot\$dcrName.json"
# File to store Log Analytics custom table schema definition
$TableSchemaFilePath = "$PSScriptRoot\$dcrTable.json"


# Template for initial log analytics custom table creation don't change the template content
$TableTemplate = "$PSScriptRoot\templateTable.json"
# Template for initial data collection rule creation don't change the template content
$DcrTemplate = "$PSScriptRoot\templateDataCollectionRule.json"

if($isUserManagedIdentity){
	DoAzureUserMgmtIdentityLoginWithSub -userManagedIdentity $userManagedIdentity -subscriptionId $subscriptionId -reconnect $false
}else{
	DoAzureUserLoginWithSub -subscriptionId $subscriptionId -azureCredUser $azureCredUser -azureCredPwd $azureCredPwd
}


#https://learn.microsoft.com/en-us/azure/azure-monitor/essentials/data-collection-rule-create-edit?tabs=powershell




$tableParamsNew = (@'
{
    "properties": {
        "schema": {
            "name": "-TABLE_NAME-",
            "columns" : []
        }
    }
}
'@  | ConvertFrom-Json)


$dcEndpointJson = @'
{
  "location": "northeurope",
  "properties": {
    "networkAcls": {
      "publicNetworkAccess": "Enabled"
    }
  }
}
'@

$WarningPreference = "Continue"
$ErrorActionPreference = "SilentlyContinue"
Stop-Transcript -ErrorAction SilentlyContinue | out-null
$scriptName = $MyInvocation.MyCommand.Definition 
$scriptNameSplit = $scriptName.Split("\")
$logFile = "$($scriptNameSplit[($scriptNameSplit.Length - 2)])_$($scriptNameSplit[($scriptNameSplit.Length - 1)])"
$FileOut = "$PSScriptRoot\$($logFile).log"
$host.ui.RawUI.WindowTitle = "$logFile"
$OutputFileLocation = $FileOut
Start-Transcript -path $OutputFileLocation -append

$tableExists = ((Invoke-AzRestMethod -Path "$($tableResourceId)?api-version=2025-02-01" -Method GET).StatusCode -eq 200)

if (!$tableExists){
    Log "create initial table $($dcrTable)"

    $tableTemplate = Get-Content -Raw -Path $TableTemplate | ConvertFrom-Json

    $tableTemplate.properties.schema.name = $dcrTable

    $initTableResponse = (Invoke-AzRestMethod -Path "$($tableResourceId)?api-version=2025-02-01" -Method PUT -payload ($tableTemplate | ConvertTo-Json -Depth 20 ) )

    $initTableResponse 
   
    if($initTableResponse.StatusCode -eq 200 -or $initTableResponse.StatusCode -eq 202){
        # get initial table
        $initTableGetResponse = (Invoke-AzRestMethod -Path "$($tableResourceId)?api-version=2025-02-01" -Method GET)
        while($initTableGetResponse.StatusCode -ne 200 -or($initTableGetResponse.StatusCode -eq 200 -and ($initTableGetResponse.Content | ConvertFrom-Json).properties.provisioningState -eq "Updating")){
            $initTableGetResponse = (Invoke-AzRestMethod -Path "$($tableResourceId)?api-version=2025-02-01" -Method GET)
            Start-Sleep -Seconds 5
        }
        $tableExists = ($initTableGetResponse.StatusCode -eq 200)
        $initialTable = $initTableGetResponse.Content | ConvertFrom-Json
    
        Log "save initial tabel schema to file $TableSchemaFilePath"

        $tableParamsNew.properties.schema.columns = $initialTable.properties.schema.columns
        $tableParamsNew.properties.schema.name = $dcrTable
       
        $tableParamsNew | ConvertTo-Json -Depth 20 | Out-File -FilePath $TableSchemaFilePath
        Log "initial table $($dcrTable) successfully created $($initTableResponse.StatusCode)"
    }else{
        Log "initial table $($dcrTable) couldn't be created $($initTableResponse.StatusCode) => $($initTableResponse.Content)"
    }
}


if($tableExists -and $saveCurrentTableSchema2File){

    Log "get current table schema via Azure REST API"
    # Get current table schema via Azure REST API
    $tables = (Invoke-AzRestMethod -Path "$($tableResourceId)?api-version=2021-12-01-preview" -Method GET).Content  |  ConvertFrom-Json

}else{

    if (Test-Path -Path $TableSchemaFilePath){
        Log "Get current table schema via local file"
        # Get current table schema via local file 
        $tables = Get-Content -Raw -Path $TableSchemaFilePath | ConvertFrom-Json 
    }else{
        Log "current table schema file $($TableSchemaFilePath) not available"
    }

}


$tableParamsNew.properties.schema.columns = $tables.properties.schema.columns
$tableParamsNew.properties.schema.name = $dcrTable


$dceExists = ((Invoke-AzRestMethod -Path ("$dceResourceId"+"?api-version=2023-03-11") -Method GET).StatusCode -eq 200)


if(!$dceExists){

    Log "create initial data collection endpoint $dcEndpointName"
    # Create data collection endpoint if required:

    $createDcEndpointResponse = Invoke-AzRestMethod -Path "$($dceResourceId)?api-version=2023-03-11" -Method PUT -payload $dcEndpointJson

    if ($createDcEndpointResponse.StatusCode -eq 200 -or $createDcEndpointResponse.StatusCode -eq 202){            
        Log "initial dce $($dcEndpointName) successfully created $($createDcEndpointResponse.StatusCode)"
    }else{
        Log "initial dce $($dcEndpointName) couldn't be created $($createDcEndpointResponse.StatusCode) => $($createDcEndpointResponse.Content)"
    }

}



$dcrExists = ((Invoke-AzRestMethod -Path ("$dcrResourceId"+"?api-version=2023-03-11") -Method GET).StatusCode -eq 200)

if(!$dcrExists){
    Log "create initial data collection rule $($dcrName)"

    $dcrTemplate = Get-Content -Raw -Path $DcrTemplate | ConvertFrom-Json

    $dcrTemplate.properties.dataCollectionEndpointId = $dceResourceId
    $dcrTemplate.properties.destinations.logAnalytics[0].workspaceResourceId = $workspaceResourceId
    $dcrTemplate.properties.destinations.logAnalytics[0].workspaceId = $workSpaceId
    $dcrTemplate.properties.dataFlows[0].outputStream = "Custom-$($dcrTable)"
   
    $dcrTemplateJson = $dcrTemplate | ConvertTo-Json -Depth 20 
    $dcrTemplateJson = $dcrTemplateJson.Replace("Custom-Stream_CL","Custom-$($dcrTable)")
 
    $initDcrResponse = (Invoke-AzRestMethod -Path "$($dcrResourceId)?api-version=2023-03-11" -Method PUT -payload $dcrTemplateJson )


    if($initDcrResponse.StatusCode -eq 200 -or $initDcrResponse.StatusCode -eq 202){
        # get initial table
        $initDcrGetResponse = (Invoke-AzRestMethod -Path ("$dcrResourceId"+"?api-version=2023-03-11") -Method GET)
        while($initDcrGetResponse.StatusCode -ne 200 -or($initDcrGetResponse.StatusCode -eq 200 -and ($initDcrGetResponse.Content | ConvertFrom-Json).properties.provisioningState -eq "Updating")){

            $initDcrGetResponse = (Invoke-AzRestMethod -Path ("$dcrResourceId"+"?api-version=2023-03-11") -Method GET)
            Start-Sleep -Seconds 5
        }
        $dcrExists = ($initDcrGetResponse.StatusCode -eq 200)
        $initialDcr = $initDcrGetResponse.Content
    
        Log "save initial dcr data collection rule to file $DcrFilePath"
        $initialDcr | ConvertFrom-Json | ConvertTo-Json -Depth 20 | Out-File -FilePath $DcrFilePath
        Log "initial $($dcrName) successfully created $($initDcrResponse.StatusCode)"
        $saveDCRDefinition = $false
    }else{
        Log "initial dcr $($dcrName) couldn't be created $($initDcrResponse.StatusCode) => $($initDcrResponse.Content) "
    }

}

if($saveCurrentTableSchema2File){

    Log "Save current tabel schema to file $TableSchemaFilePath"
    # Save current tabel schema to file $TableSchemaFilePath
    $tableParamsNew | ConvertTo-Json -Depth 20 | Out-File -FilePath $TableSchemaFilePath
    if($saveDCRDefinition){
        $DCR = Invoke-AzRestMethod -Path ("$dcrResourceId"+"?api-version=2023-03-11") -Method GET
        $DCR.Content | ConvertFrom-Json | ConvertTo-Json -Depth 20 | Out-File -FilePath $DcrFilePath
    }
}


if(!$saveCurrentTableSchema2File){

    Log "update table schema"
    # Update analytics table schema definition in $TableSchemaFilePath  and run the following command:
    $updateTableResponse = (Invoke-AzRestMethod -Path "$($tableResourceId)?api-version=2025-02-01" -Method PUT -payload ($tableParamsNew | ConvertTo-Json -Depth 20))
    if($updateTableResponse.StatusCode -ne 200 -and $updateTableResponse.StatusCode -ne 202){
        Log "current table $($dcrTable) couldn't be updated $($updateTableResponse.StatusCode)  => $($updateTableResponse.Content)"
    }

    # Update DCR definition in $DcrFilePath and run the following command: 
    if (Test-Path -Path $DcrFilePath){
        Log "Get current dcr schema via local file"
        # Get current dcr schema via local file 
        $dcrSchema = Get-Content -Raw -Path $DcrFilePath 
        Log "update dcr schema"
        # Update DCR definition in $DcrFilePath and run the following command: 
        $updateDcrResponse = (Invoke-AzRestMethod -Path "$($dcrResourceId)?api-version=2023-03-11" -Method PUT -payload $dcrSchema )
        if($updateDcrResponse.StatusCode -ne 200 -and $updateDcrResponse.StatusCode -ne 202){
             Log "initial dcr $($dcrName) couldn't be updted $($updateDcrResponse.StatusCode) => $($updateDcrResponse.Content) "
        }
    }else{
        Log "current dcr schema file $($DcrFilePath) not available dcr schema couldn't be updated"
    }

}