//--------------------------------------------------------------
// Copyright (c) 2016 PTV Group
// 
// For license details, please refer to the file LICENSE, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using log4net.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Log4ALA
{
    public class LoggingEventSerializer
    {
        public static char[] AllowedCharPlus = new char[] { '_' };

        private Log4ALAAppender appender;

        private static Regex validString = new Regex(@"[a-zA-Z0-9_]+", RegexOptions.Compiled);
        private static bool perfBoost = false;


        public LoggingEventSerializer(Log4ALAAppender appender)
        {
            this.appender = appender;
        }

        public string SerializeLoggingEvents(IEnumerable<LoggingEvent> loggingEvents)
        {
            var sb = new StringBuilder();

            foreach (var loggingEvent in loggingEvents)
            {
                sb.AppendLine(SerializeLoggingEvent(loggingEvent));
            }

            return sb.ToString();
        }

        private string SerializeLoggingEvent(LoggingEvent loggingEvent)
        {

            IDictionary<string, Object> payload = new ExpandoObject() as IDictionary<string, Object>;

            if (!appender.EnablePassThroughTimeStampField)
            {
                payload.Add(appender.coreFields.DateFieldName, loggingEvent.TimeStamp.ToUniversalTime().ToString("o"));
            }

            bool msgIsValidString = loggingEvent.MessageObject is System.String && !string.IsNullOrWhiteSpace((string)loggingEvent.MessageObject);
            bool isKeyValueDetection = appender.KeyValueDetection;
            bool isKeyToLowerCase = appender.KeyToLowerCase;
            bool isMsgObjNotNull = loggingEvent.MessageObject != null;
            bool isMsgObjValidSysStrFormat = isMsgObjNotNull && loggingEvent.MessageObject is log4net.Util.SystemStringFormat;


            var valObjects = new ExpandoObject() as IDictionary<string, Object>;
            if ((bool)appender.JsonDetection && msgIsValidString && ((string)loggingEvent.MessageObject).IsValidJson())
            {
                Dictionary<string, string> values = JsonConvert.DeserializeObject<Dictionary<string, string>>((string)loggingEvent.MessageObject);
                foreach (var val in values)
                {
                    if (!valObjects.ContainsKey(val.Key))
                    {
                        var key = val.Key.TrimFieldName((int)appender.MaxFieldNameLength);
                        payload.Add((isKeyToLowerCase ? key.ToLower() : key ), val.Value.TypeConvert((int)appender.MaxFieldByteLength));
                    }
                }
            }
            else
            {
                if (isKeyValueDetection && msgIsValidString)
                {
                    ConvertKeyValueMessage(payload, (string)loggingEvent.MessageObject, appender.MaxFieldByteLength, appender.coreFields.MiscMessageFieldName, appender.MaxFieldNameLength, appender.KeyValueSeparator, appender.KeyValuePairSeparator);
                }
                else if (isKeyValueDetection && isMsgObjValidSysStrFormat && !string.IsNullOrWhiteSpace(loggingEvent.RenderedMessage))
                {
                    ConvertKeyValueMessage(payload, loggingEvent.RenderedMessage, appender.MaxFieldByteLength, appender.coreFields.MiscMessageFieldName, appender.MaxFieldNameLength, appender.KeyValueSeparator, appender.KeyValuePairSeparator);
                }
                else if (isMsgObjValidSysStrFormat)
                {
                    payload.Add(appender.coreFields.MiscMessageFieldName, loggingEvent.RenderedMessage);
                }
                else if (appender.DisableAnonymousPropsPrefix && isMsgObjNotNull && loggingEvent.MessageObject.IsAnonymousType())
                {
                    var anonymous = loggingEvent.MessageObject;
                    foreach (PropertyInfo propertyInfo in anonymous.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    {
                        var key = propertyInfo.Name.TrimFieldName(appender.MaxFieldNameLength);
                        payload.Add((isKeyToLowerCase ? key.ToLower() : key), propertyInfo.GetValue(anonymous, null));
                    }
                }
                else
                {
                    payload.Add(appender.coreFields.MiscMessageFieldName, loggingEvent.MessageObject);
                }
            }

            if (appender.AppendLogger)
            {
                payload.Add(appender.coreFields.LoggerFieldName, loggingEvent.LoggerName);
            }
            if (appender.AppendLogLevel)
            {
                payload.Add(appender.coreFields.LevelFieldName, loggingEvent.Level.DisplayName.ToUpper());
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
                payload.Add("ExMsg", exception.Message);
                payload.Add("ExType", exception.GetType().Name);
                payload.Add("ExStackTrace", exception.StackTrace);
                if (exception.InnerException != null)
                {
                    payload.Add("InnerExMsg", exception.InnerException.Message);
                    payload.Add("InnerExType", exception.InnerException.GetType().Name);
                    payload.Add("InnerExStackTrace", exception.InnerException.StackTrace);
                }
            }

            return JsonConvert.SerializeObject(payload, Formatting.None);
        }

        private void ConvertKeyValueMessage(IDictionary<string, Object> payload, string message, int maxByteLength, string miscMsgFieldName = ConfigSettings.DEFAULT_MISC_MSG_FIELD_NAME, int maxFieldNameLength = ConfigSettings.DEFAULT_MAX_FIELD_NAME_LENGTH, string KeyValueSeparator = ConfigSettings.DEFAULT_KEY_VALUE_SEPARATOR, string KeyValuePairSeparator = ConfigSettings.DEFAULT_KEY_VALUE_PAIR_SEPARATOR, bool isKeyToLowerCase = ConfigSettings.DEFAULT_KEY_TO_LOWER_CASE)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                string[] le1Sp = null;

                if (KeyValuePairSeparator.Length == 1)
                {
                    le1Sp = message.Split(KeyValuePairSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    //remove empty objects
                    //le1Sp = le1Sp.Where(ll => !string.IsNullOrWhiteSpace((ll))).Select(l => l.Trim()).ToArray();
                }
                else
                {
                    le1Sp = message.Split(appender.KeyValuePairSeparators, StringSplitOptions.RemoveEmptyEntries);
                }

                StringBuilder misc = new StringBuilder();

                ConcurrentDictionary<string, int> duplicates = new ConcurrentDictionary<string, int>();

                foreach (var le1p in le1Sp)
                {

                    if (le1p.Occurences(KeyValueSeparator) > 1)
                    {
                        string[] le1pSP = le1p.Split(' ');
                        //remove whitespaces
                        le1pSP = le1pSP.Select(l => l.Trim()).ToArray();
                        foreach (var le1pp in le1pSP)
                        {
                            int keyValueCount = le1pp.Occurences(KeyValueSeparator);
                            if (keyValueCount == 1)
                            {
                                string[] le1ppSP;
                                if (KeyValueSeparator.Length == 1)
                                {
                                    le1ppSP = le1pp.Split(Convert.ToChar(KeyValueSeparator));
                                }
                                else
                                {
                                    le1ppSP = le1pp.Split(appender.KeyValueSeparators, StringSplitOptions.None);
                                }

                                if (!string.IsNullOrWhiteSpace(le1ppSP[0]) && le1ppSP.Length == 2)
                                {
                                    CreateAlaField(payload, duplicates, le1ppSP[0], le1ppSP[1].TypeConvert(maxByteLength), maxFieldNameLength, isKeyToLowerCase);
                                }
                            }
                            else if(keyValueCount == 2) {
                                string[] le1ppSP;
                                if (KeyValueSeparator.Length == 1)
                                {
                                    le1ppSP = le1pp.Split(Convert.ToChar(KeyValueSeparator));
                                }
                                else
                                {
                                    le1ppSP = le1pp.Split(appender.KeyValueSeparators, StringSplitOptions.None);
                                }

                                if (le1ppSP.Length == 3 && !string.IsNullOrWhiteSpace(le1ppSP[0]) && !string.IsNullOrWhiteSpace(le1ppSP[1]) && 
                                    string.IsNullOrWhiteSpace(le1ppSP[2]) && le1pp.Trim().EndsWith(KeyValueSeparator) && 
                                    $"{le1ppSP[1].Trim()}{KeyValueSeparator}".IsBase64())
                                {
                                    CreateAlaField(payload, duplicates, le1ppSP[0], $"{le1ppSP[1].Trim()}{KeyValueSeparator}".TypeConvert(maxByteLength), maxFieldNameLength, isKeyToLowerCase);

                                }
                            }
                            else
                            {
                                misc.Append(le1pp.TypeConvert(maxByteLength));
                                misc.Append(" ");
                            }
                        }
                    }
                    else
                    {
                        if (le1p.Occurences(KeyValueSeparator) == 1)
                        {
                            string[] le1ppSP;
                            if (KeyValueSeparator.Length == 1)
                            {
                                le1ppSP = le1p.Split(Convert.ToChar(KeyValueSeparator));
                            }
                            else
                            {
                                le1ppSP = le1p.Split(appender.KeyValueSeparators, StringSplitOptions.None);
                            }

                            if (!string.IsNullOrWhiteSpace(le1ppSP[0]) && le1ppSP.Length == 2)
                            {
                                CreateAlaField(payload, duplicates, le1ppSP[0], le1ppSP[1].TypeConvert(maxByteLength), maxFieldNameLength, isKeyToLowerCase);
                            }
                        }
                        else
                        {
                            misc.Append(le1p.TypeConvert(maxByteLength));
                            misc.Append(" ");
                        }
                    }
                }

                string miscStr = misc.ToString().Trim();

                if (!string.IsNullOrWhiteSpace(miscStr))
                {
                    payload.Add(miscMsgFieldName, miscStr.OfMaxBytes(maxByteLength));
                }

            }
        }

        private void ConvertKeyValueMessageNew(IDictionary<string, Object> payload, string message, int maxByteLength, string miscMsgFieldName = ConfigSettings.DEFAULT_MISC_MSG_FIELD_NAME, int maxFieldNameLength = ConfigSettings.DEFAULT_MAX_FIELD_NAME_LENGTH, string KeyValueSeparator = ConfigSettings.DEFAULT_KEY_VALUE_SEPARATOR, string KeyValuePairSeparator = ConfigSettings.DEFAULT_KEY_VALUE_PAIR_SEPARATOR)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {

                if (KeyValuePairSeparator.Length == 1)
                {
                    if (KeyValueSeparator.Length == 1)
                    {
                        payload = message.Split(appender.KeyValuePairSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(KeyValueSeparator.ToCharArray())).GroupBy(x => x[0]).ToDictionary(x => x.Key, x => x.First()[1].TypeConvert(maxByteLength)); //.ToDictionary(x => x[0], x => x[1].TypeConvert(maxByteLength, perfBoost));
                    }
                    else
                    {
                        payload = message.Split(appender.KeyValuePairSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(appender.KeyValueSeparators, StringSplitOptions.None)).GroupBy(x => x[0]).ToDictionary(x => x.Key, x => x.First()[1].TypeConvert(maxByteLength)); //.ToDictionary(x => x[0], x => x[1].TypeConvert(maxByteLength, perfBoost));
                    }
                }
                else
                {
                    if (KeyValueSeparator.Length == 1)
                    {
                        payload = message.Split(appender.KeyValuePairSeparators, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(KeyValueSeparator.ToCharArray())).GroupBy(x => x[0]).ToDictionary(x => x.Key, x => x.First()[1].TypeConvert(maxByteLength)); //.ToDictionary(x => x[0], x => x[1].TypeConvert(maxByteLength, perfBoost));
                    }
                    else
                    {
                        payload = message.Split(appender.KeyValuePairSeparators, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(appender.KeyValueSeparators, StringSplitOptions.None)).GroupBy(x => x[0]).ToDictionary(x => x.Key, x => x.First()[1].TypeConvert(maxByteLength));
                    }
                }
            }
        }


        private static void CreateAlaField(IDictionary<string, object> payload, ConcurrentDictionary<string, int> duplicates, string key, object value, int maxFieldNameLength, bool isKeyToLowerCase)
        {

            key = key.TrimFieldName(maxFieldNameLength).Trim();

            key = isKeyToLowerCase ? key.ToLower() : key;

            if (!payload.ContainsKey(key))
            {
                payload.Add(key, value);
            }
            else
            {
                int duplicateCounter;
                if (duplicates.ContainsKey(key))
                {
                    duplicates.TryRemove(key, out duplicateCounter);
                    duplicates.TryAdd(key, ++duplicateCounter);
                }
                else
                {
                    duplicateCounter = 0;
                    duplicates.TryAdd(key, duplicateCounter);
                }

                payload.Add($"{key}_Duplicate{duplicateCounter}", value);

            }
        }



        private static string RemoveSpecialCharacters(string str)
        {
            var sb = new StringBuilder(str.Length);
            foreach (var c in str.Where(c => (validString.Match(c.ToString()).Success)))
            {
                sb.Append(c);
            }
            return sb.ToString();
        }

       

    }


    static class StringExtension
    {
        public static Regex yearRegex = new Regex(@"\d{4}", RegexOptions.Compiled);

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

        public static string TrimFieldName(this string str, int length = ConfigSettings.DEFAULT_MAX_FIELD_NAME_LENGTH)
        {
            return str.Length > length ? str.Substring(0, length) : str;
        }

        public static bool IsBase64(this string base64value)
        {
            try
            {
                byte[] converted = System.Convert.FromBase64String(base64value);
                return base64value.EndsWith("=");
            }
            catch
            {
                return false;
            }
        }

        public static object TypeConvert(this string messageValue, int maxByteLength)
        {
            string value = messageValue;
            object convertedValue = null;
            DateTime parsedDateTime;
            double parsedDouble;
            bool parsedBool;
            if (Double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedDouble))
            {
                convertedValue = parsedDouble;
            }
            else if (yearRegex.Matches(value).Count == 1 &&  DateTime.TryParse(value, out parsedDateTime))
            {
                convertedValue = parsedDateTime.ToUniversalTime();
            }
            else if (Boolean.TryParse(value, out parsedBool))
            {
                convertedValue = parsedBool;
            }
            else
            {
                convertedValue = messageValue.OfMaxBytes(maxByteLength).TrimEnd(new char[] { ',' });
            }

            return convertedValue;
        }

        public static bool IsValidJson(this string stringValue)
        {
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                var value = stringValue.Trim();
                if ((value.StartsWith("{") && value.EndsWith("}")) || //For object
                    (value.StartsWith("[") && value.EndsWith("]"))) //For array
                {
                    try
                    {
                        var obj = JToken.Parse(value);
                        return true;
                    }
                    catch (JsonReaderException)
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        public static int Occurences(this string str, string val)
        {
            int occurrences = 0;
            int startingIndex = 0;

            while ((startingIndex = str.IndexOf(val, startingIndex)) >= 0)
            {
                ++occurrences;
                ++startingIndex;
            }

            return occurrences;
        }

    }

    static class ObjectExtensions
    {
        public static bool IsAnonymousType(this object instance)
        {

            if (instance == null)
                return false;

            return instance.GetType().Namespace == null;
        }
    }
}
