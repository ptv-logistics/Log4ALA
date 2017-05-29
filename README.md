## Log4ALA

Log4Net appender fo Azure Log Analytics (ALA)... sending data to Azure Log Analytics with the [HTTP Data Collector API](https://docs.microsoft.com/en-us/azure/log-analytics/log-analytics-data-collector-api).
The data will also be logged/sent asynchronously for high performance and to avoid blocking the caller thread.

It uses the [HTTPDataCollectorAPI](https://github.com/ealsur/HTTPDataCollectorAPI) package internally.

## Get it

You can obtain this project as a [Nuget Package](https://www.nuget.org/packages/Log4ALA)

    Install-Package Log4ALA

Or reference it and use it according to the [License](./LICENSE).

## Use it

This example is also available as a [LoggerTests.cs](https://github.com/ptv-logistics/Log4ALA/blob/master/Log4ALA/LoggerTests.cs):

```csharp
using log4net;
using System;

namespace Log4ALATest
{
    class LoggerTests
    {

        private static ILog alaLogger = LogManager.GetLogger("Log4ALALogger");

        static void Main(string[] args)
        {
 
            for (int i = 0; i < 10; i++)
            {
                alaLogger.Info(new { id = $"log-{i}", message = $"test-{i}" });

            }


            System.Threading.Thread.Sleep(new TimeSpan(0, 10, 0));
        }
    }
}

``` 

## Proxy settings

The [HTTPDataCollectorAPI](https://github.com/ealsur/HTTPDataCollectorAPI) used under the hood doesn't support 
explicit proxy settings so the only way at the moment is to set the default proxy by config (Web.config or App.config):

Refer to this [article](https://msdn.microsoft.com/en-us/library/kd3cf2ex(v=vs.110).aspx) for more information.
```xml
<configuration>
    <system.net>
        <defaultProxy>
            <proxy
              proxyaddress="http://IP:PORT"
              bypassonlocal="true" />
        </defaultProxy>
    </system.net>
<configuration>
``` 


or by code:

```csharp

System.Net.WebRequest.DefaultWebProxy = new System.Net.WebProxy("http://IP:PORT/", true);

``` 



## Example App Configuration file

This configuration is also available as a [App.config](https://github.com/ptv-logistics/Log4IoTHub/blob/master/Log4IoTHubTest/App.config):


```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5.2" />
  </startup>

  <appSettings>
    <!--
    don't forget to add [assembly: log4net.Config.XmlConfigurator()] to AssemblyInfo.cs
    -->
    <add key="log4net.Config" value="log4net.config"/>
    <add key="log4net.Config.Watch" value="True"/>
  </appSettings>

</configuration>
``` 

## Example Log4Net Configuration file

```xml
﻿<?xml version="1.0" encoding="utf-8" ?>
<log4net>
  
  <appender name="Log4ALAAppender"
               type="Log4ALA.Log4ALAAppender, Log4ALA">

    <!--mandatory id of the Azure Log Analytics WorkspaceID -->
    <workspaceId value="YOUR_WORKSPACE_ID" />
    <!--the primary key Primary Key => OMS Portal Overview/Settings/Connected Sources-->
    <SharedKey value="YOUR_SHARED_KEY" />
    <!-- the log type... the name of the record type that you'll be creating-->
    <logType value="YOUR_LOG_TYPE" />
    <!-- The API version of the HTTP Data Collector API -->
    <azureApiVersion value="2016-04-01" />
 
    <!-- 
    optional debug setting which should only be used during development or on testsystem.
    Set logMessageToFile=true to inspect your messages (in log4ALA_info.log) which will be sent to the Azure Log Analytics Workspace.
    -->
    <logMessageToFile value="true"/>
    
    <!-- 
    optional name of an logger defined further down with an depending appender e.g. logentries to log internal errors. If the value is empty or the property isn't defined 
    errors will only be logged to log4ALA_error.log
    -->
    <!--<errLoggerName value="Log4ALAErrors2LogentriesLogger"/>-->
    
   
  </appender>


  <!--
  <appender name="LeAppender" type="log4net.Appender.LogentriesAppender, LogentriesLog4net">
    <immediateFlush value="true" />
    <useSsl value="true" />
    <token value="YOUR_LOGENTRIES_TOKEN" />
    <layout type="log4net.Layout.PatternLayout">
      <param name="ConversionPattern" value="%d{yyyy-MM-dd HH:mm:ss.fff zzz};loglevel=%level%;operation=%m;" />
    </layout>
    <filter type="log4net.Filter.LevelRangeFilter">
      <levelMin value="INFO" />
      <levelMax value="FATAL" />
    </filter>
  </appender>

  <logger name="Log4ALAErrors2LogentriesLogger" additivity="false">
    <level value="ALL" />
    <appender-ref ref="LeAppender" />
  </logger>
  -->

  <logger name="Log4ALALogger" additivity="false">
    <appender-ref ref="Log4ALAAppender" />
  </logger>
  
</log4net>
``` 

## Issues

Keep in mind that this library won't assure that your JSON payloads are being indexed, it will make sure that the HTTP Data Collection API [responds an Accept](https://azure.microsoft.com/en-us/documentation/articles/log-analytics-data-collector-api/#return-codes) but there is no way (right now) to know when has the payload been indexed completely... see also [SLA for Log Analytics](https://azure.microsoft.com/en-gb/support/legal/sla/log-analytics/v1_1/)
