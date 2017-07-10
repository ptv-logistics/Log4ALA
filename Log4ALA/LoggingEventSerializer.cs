//--------------------------------------------------------------
// Copyright (c) 2016 PTV Group
// 
// For license details, please refer to the file LICENSE, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using log4net.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System;
using System.Collections.Concurrent;

namespace Log4ALA
{
    public class LoggingEventSerializer
    {
        public static char[] AllowedCharPlus = new char[] {'_'};

        public string SerializeLoggingEvents(IEnumerable<LoggingEvent> loggingEvents, Log4ALAAppender appender)
        {
            var sb = new StringBuilder();

            foreach (var loggingEvent in loggingEvents)
            {
                sb.AppendLine(SerializeLoggingEvent(loggingEvent, appender));
            }

            return sb.ToString();
        }

        private string SerializeLoggingEvent(LoggingEvent loggingEvent, Log4ALAAppender appender)
        {

            dynamic payload = new ExpandoObject();
            payload.DateValue = loggingEvent.TimeStamp.ToUniversalTime().ToString("o");
           

            var valObjects = new ExpandoObject() as IDictionary<string, Object>;
            if ((bool)appender.JsonDetection && typeof(System.String).IsInstanceOfType(loggingEvent.MessageObject) && !string.IsNullOrWhiteSpace((string)loggingEvent.MessageObject) && ValidateJSON((string)loggingEvent.MessageObject))
            {
                Dictionary<string, string> values = JsonConvert.DeserializeObject<Dictionary<string, string>>((string)loggingEvent.MessageObject);
                foreach (var val in values)
                {
                    if (!valObjects.ContainsKey(val.Key))
                    {
                        valObjects.Add(val.Key, val.Value.OfMaxBytes((int)appender.MaxFieldByteLength));
                    }
                }
                payload.LogMessage = valObjects;
            }
            else
            {
                if ((bool)appender.KeyValueDetection && typeof(System.String).IsInstanceOfType(loggingEvent.MessageObject) && !string.IsNullOrWhiteSpace((string)loggingEvent.MessageObject) && !ValidateJSON((string)loggingEvent.MessageObject))
                {
                    payload.LogMessage = ConvertKeyValueMessage((string)loggingEvent.MessageObject, (int)appender.MaxFieldByteLength);
                }
                else
                {
                    payload.LogMessage = loggingEvent.MessageObject;
                }
            }

            if ((bool)appender.AppendLogger)
            {
                payload.Logger = loggingEvent.LoggerName;
            }
            if ((bool)appender.AppendLogLevel)
            {
                payload.Level = loggingEvent.Level.DisplayName.ToUpper();
            }

            //If any custom properties exist, add them to the dynamic object
            //i.e. if someone added loggingEvent.Properties["xx:traceId"] = "helloWorld"
            foreach (var key in loggingEvent.Properties.GetKeys())
            {
                ((IDictionary<string, object>)payload)[RemoveSpecialCharacters(key)] = loggingEvent.Properties[key];
            }

            var exception = loggingEvent.ExceptionObject;
            if (exception != null)
            {
                string errMessage = $"loggingEvent.Exception: {exception}";
                appender.log.Err(errMessage);
                appender.extraLog.Err(errMessage);
                payload.exception = new ExpandoObject();
                payload.exception.message = exception.Message;
                payload.exception.type = exception.GetType().Name;
                payload.exception.stackTrace = exception.StackTrace;
                if (exception.InnerException != null)
                {
                    payload.exception.innerException = new ExpandoObject();
                    payload.exception.innerException.message = exception.InnerException.Message;
                    payload.exception.innerException.type = exception.InnerException.GetType().Name;
                    payload.exception.innerException.stackTrace = exception.InnerException.StackTrace;
                }
            }

            return JsonConvert.SerializeObject(payload, Formatting.None);
        }

        private static dynamic ConvertKeyValueMessage(string message, int maxByteLength)
        {
            var msgPayload = new ExpandoObject() as IDictionary<string, Object>;

            if (!string.IsNullOrWhiteSpace(message))
            {
                string[] le1Sp = message.Split(';');

                //remove empty objects
                le1Sp = le1Sp.Where(ll => !string.IsNullOrWhiteSpace((ll))).Select(l => l.Trim()).ToArray();

                StringBuilder misc = new StringBuilder();

                ConcurrentDictionary<string, int> duplicates = new ConcurrentDictionary<string, int>();

                foreach (var le1p in le1Sp)
                {
                    if (le1p.Count(c => c == '=') > 1)
                    {
                        string[] le1pSP = le1p.Split(' ');
                        //remove whitespaces
                        le1pSP = le1pSP.Select(l => l.Trim()).ToArray();
                        foreach (var le1pp in le1pSP)
                        {
                            if (le1pp.Count(c => c == '=') == 1)
                            {
                                string[] le1ppSP = le1pp.Split('=');
                                if (!string.IsNullOrWhiteSpace(le1ppSP[0]) && le1ppSP.Length == 2)
                                {
                                    string value = Convert(le1ppSP[1], maxByteLength);

                                    if (!msgPayload.ContainsKey(le1ppSP[0]))
                                    {
                                        msgPayload.Add(le1ppSP[0], value);
                                    }
                                    else
                                    {
                                        int duplicateCounter;
                                        if (duplicates.ContainsKey(le1ppSP[0]))
                                        {
                                            duplicates.TryRemove(le1ppSP[0], out duplicateCounter);
                                            duplicates.TryAdd(le1ppSP[0], ++duplicateCounter);
                                        }
                                        else
                                        {
                                            duplicateCounter = 0;
                                            duplicates.TryAdd(le1ppSP[0], duplicateCounter);
                                        }

                                        msgPayload.Add($"{le1ppSP[0]}_Duplicate{duplicateCounter}", value);

                                    }
                                }
                            }
                            else
                            {
                                misc.Append(le1pp);
                                misc.Append(" ");
                            }
                        }
                    }
                    else
                    {
                        if (le1p.Count(c => c == '=') == 1)
                        {
                            string[] le1ppSP = le1p.Split('=');
 
                            if (!string.IsNullOrWhiteSpace(le1ppSP[0]) && le1ppSP.Length == 2)
                            {
                                string value = Convert(le1ppSP[1], maxByteLength);


                                if (!msgPayload.ContainsKey(le1ppSP[0]))
                                {
                                    msgPayload.Add(le1ppSP[0], (value).TrimEnd(new char[] { ',' }));
                                }
                                else
                                {
                                    int duplicateCounter;
                                    if (duplicates.ContainsKey(le1ppSP[0]))
                                    {
                                        duplicates.TryRemove(le1ppSP[0], out duplicateCounter);
                                        duplicates.TryAdd(le1ppSP[0], ++duplicateCounter);
                                    }
                                    else
                                    {
                                        duplicateCounter = 0;
                                        duplicates.TryAdd(le1ppSP[0], duplicateCounter);
                                    }

                                    msgPayload.Add($"{le1ppSP[0]}_Duplicate{duplicateCounter}", (value).TrimEnd(new char[] { ',' }));

                                }
                            }
                        }
                        else
                        {
                            misc.Append(le1p);
                            misc.Append(" ");
                        }
                    }
                }

                string miscStr = misc.ToString().Trim();

                if (!string.IsNullOrWhiteSpace(miscStr))
                {
                    msgPayload.Add("Misc", miscStr.OfMaxBytes(maxByteLength));
                }

            }
 
            return msgPayload;
        }

        public static string Convert(string messageValue, int maxByteLength)
        {
            string value = messageValue;
            DateTime parsedDateTime;
            if (DateTime.TryParse(value, out parsedDateTime))
            {
                value = parsedDateTime.ToUniversalTime().ToString("o");
            }
            else
            {
                value = messageValue.OfMaxBytes(maxByteLength);
            }

            return value;
        }

        private static string RemoveSpecialCharacters(string str)
        {
            var sb = new StringBuilder(str.Length);
            foreach (var c in str.Where(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || AllowedCharPlus.Any(ch => ch.Equals(c))))
            {
                sb.Append(c);
            }
            return sb.ToString();
        }

        public bool ValidateJSON(string s)
        {
            try
            {
                JToken.Parse(s);
                return true;
            }
            catch (JsonReaderException ex)
            {
                return false;
            }
        }

    }


    static class StringExtension
    {

        public static string OfMaxBytes(this string str, int maxByteLength)
        {
            return str.Aggregate("", (s, c) =>
            {
                if (Encoding.UTF8.GetByteCount(s + c) <= maxByteLength)
                {
                    s += c;
                }
                return s;
            });
        }

    }

}
