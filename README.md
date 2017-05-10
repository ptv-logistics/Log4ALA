### Log4ALA

Azure Log Analytics (ALA) appender for log4net... sending data to Azure Log Analytics with the [HTTP Data Collector API](https://docs.microsoft.com/en-us/azure/log-analytics/log-analytics-data-collector-api).
The data will also be logged/sent asynchronously for high performance and to avoid blocking the caller thread.

It uses the [HTTPDataCollectorAPI](https://github.com/ealsur/HTTPDataCollectorAPI) package internally.

## Get it

You can obtain this project as a [Nuget Package](https://www.nuget.org/packages/Log4ALA) **coming soon...** 

    Install-Package Log4ALA

Or reference it and use it according to the [License](./LICENSE).

## Use it

**coming soon...** 

## Configure it

**coming soon...** 

## Example Log4Net Configuration file

**coming soon...** 

## Issues

Keep in mind that this library won't assure that your JSON payloads are being indexed, it will make sure that the HTTP Data Collection API [responds an Accept](https://azure.microsoft.com/en-us/documentation/articles/log-analytics-data-collector-api/#return-codes) but there is no way (right now) to know when has the payload been indexed completely. 
