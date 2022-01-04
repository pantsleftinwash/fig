using System.Collections.Generic;

namespace Fig.Contracts.Settings
{
    public class SettingsClientDataContract
    {
        public string Name { get; set; }
        
        public string? Instance { get; set; }
        
        public List<SettingDataContract> Settings { get; set; }
    }
}