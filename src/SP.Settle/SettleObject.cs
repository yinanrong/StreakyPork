using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Sp.Settle
{
    public class SettleObject
    {
        private readonly IDictionary<string, object> _values;

        public SettleObject()
        {
            _values = new Dictionary<string, object>();
        }

        public SettleObject(IDictionary<string, object> values)
        {
            _values = values;
        }

        public static implicit operator SettleObject(Dictionary<string, object> values)
        {
            return new SettleObject(values);
        }

        public static implicit operator SettleObject(SortedDictionary<string, object> values)
        {
            return new SettleObject(values);
        }


        public static implicit operator SortedDictionary<string, object>(SettleObject values)
        {
            return new SortedDictionary<string, object>(values.GetValues());
        }


        public void SetValue(string key, object value)
        {
            _values[key] = value;
        }

        public T GetValue<T>(string key)
        {
            if (_values.TryGetValue(key, out var o))
                return (T) Convert.ChangeType(o, typeof(T));
            return default(T);
        }

        public object GetValue(string key)
        {
            if (!_values.ContainsKey(key))
                return null;
            return _values[key];
        }

        public IDictionary<string, object> GetValues()
        {
            return _values;
        }

        public bool Any()
        {
            return _values.Any();
        }

        public void RemoveValue(string key)
        {
            _values.Remove(key);
        }

        public bool IsSet(string key)
        {
            return _values.ContainsKey(key);
        }


        public string ToUrl()
        {
            var prestr = new StringBuilder();
            foreach (var pair in _values)
                prestr.AppendFormat("{0}={1}&", pair.Key, WebUtility.UrlEncode(pair.Value?.ToString()));
            prestr.Remove(prestr.Length - 1, 1);
            return prestr.ToString();
        }

        public string ToUrlNoEncode()
        {
            var prestr = new StringBuilder();
            foreach (var pair in _values)
                prestr.AppendFormat("{0}={1}&", pair.Key, pair.Value);
            prestr.Remove(prestr.Length - 1, 1);
            return prestr.ToString();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(_values);
        }


        public string ToUrlForSign(bool ignoreSignType = false, bool ignoreEmpty = true)
        {
            var prestr = new StringBuilder();
            foreach (var pair in _values)
            {
                if (ignoreEmpty && (pair.Value == null || pair.Value.ToString() == ""))
                    continue;
                if (pair.Value is IDictionary<string, object>)
                {
                    prestr.AppendFormat("{0}={1}&", pair.Key, JsonConvert.SerializeObject(pair.Value));
                    continue;
                }

                if (pair.Key != "sign" && pair.Key.ToLower() != "signature" &&
                    !(ignoreSignType && pair.Key == "sign_type"))
                    prestr.AppendFormat("{0}={1}&", pair.Key, pair.Value);
            }

            return prestr.Remove(prestr.Length - 1, 1).ToString();
        }


        public void FromFormData(string formData, bool ignoreEmpty = true)
        {
            if (string.IsNullOrEmpty(formData))
                return;
            var request = ParseNullableQuery(formData);
            foreach (var ent in request)
                if (!ignoreEmpty || !string.IsNullOrEmpty(ent.Key) && !string.IsNullOrEmpty(ent.Value))
                    SetValue(ent.Key, ent.Value.ToString());
        }

        public static Dictionary<string, StringValues> ParseNullableQuery(string queryString,string ignoreTrimKey="sign")
        {
            var accumulator = new KeyValueAccumulator();

            if (string.IsNullOrEmpty(queryString) || queryString == "?")
            {
                return null;
            }

            var scanIndex = 0;
            if (queryString[0] == '?')
            {
                scanIndex = 1;
            }

            var textLength = queryString.Length;
            var equalIndex = queryString.IndexOf('=');
            if (equalIndex == -1)
            {
                equalIndex = textLength;
            }

            while (scanIndex < textLength)
            {
                var delimiterIndex = queryString.IndexOf('&', scanIndex);
                if (delimiterIndex == -1)
                {
                    delimiterIndex = textLength;
                }

                if (equalIndex < delimiterIndex)
                {
                    while (scanIndex != equalIndex && char.IsWhiteSpace(queryString[scanIndex]))
                    {
                        ++scanIndex;
                    }
                    var name = queryString.Substring(scanIndex, equalIndex - scanIndex);
                    var value = queryString.Substring(equalIndex + 1, delimiterIndex - equalIndex - 1);
                    if (name==ignoreTrimKey)
                    {
                        accumulator.Append(
                            Uri.UnescapeDataString(name),
                            Uri.UnescapeDataString(value));
                        equalIndex = queryString.IndexOf('=', delimiterIndex);
                    }
                    else
                    {
                        accumulator.Append(
                            Uri.UnescapeDataString(name),
                            Uri.UnescapeDataString(value.Replace('+', ' ')));
                        equalIndex = queryString.IndexOf('=', delimiterIndex);
                    }
                 
                    if (equalIndex == -1)
                    {
                        equalIndex = textLength;
                    }
                }
                else
                {
                    if (delimiterIndex > scanIndex)
                    {
                        accumulator.Append(queryString.Substring(scanIndex, delimiterIndex - scanIndex), string.Empty);
                    }
                }

                scanIndex = delimiterIndex + 1;
            }

            if (!accumulator.HasValues)
            {
                return null;
            }

            return accumulator.GetResults();
        }
    }
}