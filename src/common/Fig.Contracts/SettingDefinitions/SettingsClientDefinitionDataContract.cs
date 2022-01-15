﻿using System.Collections.Generic;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingsClientDefinitionDataContract
    {
        public string Name { get; set; }

        public string Instance { get; set; }

        public List<SettingDefinitionDataContract> Settings { get; set; }
        
        public List<SettingVerificationDefinitionDataContract> Verifications { get; set; }
    }
}

