using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Onion.Core
{
    public class OnionConfig
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(1500)]
        public int ConnectTimeout { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(5000)]
        public int ClientSyncInterval { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(3000)]
        public int DisconnectTimeout { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(6)]
        public int MaximumRelay { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(2000)]
        public int ResponseTimeout { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(3)]
        public int MaximumResend { get; set; }
        public string[] ServerList { get; set; }

        public static OnionConfig Default
        {
            get
            {
                OnionConfig result = new OnionConfig();
                foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(result))
                {
                    DefaultValueAttribute defaultValueAttribute = (DefaultValueAttribute)propertyDescriptor.Attributes[typeof(DefaultValueAttribute)];
                    if (defaultValueAttribute != null)
                        propertyDescriptor.SetValue(result, defaultValueAttribute.Value);
                }
                return result;
            }
        }
    }
}
