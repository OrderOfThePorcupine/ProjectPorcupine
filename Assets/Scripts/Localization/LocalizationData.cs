#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using Newtonsoft.Json;

namespace ProjectPorcupine.Localization
{
    /// <summary>
    /// Holds information about a language's configuration data.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class LocalizationData
    {
        [JsonProperty("rtl")]
        public bool isRightToLeft = false;

        [JsonProperty("code")]
        private readonly string localizationCode;

        // Even for RTL languages, this is kept as defined in the config files. The property does the character reversal
        [JsonProperty("name")]
        private string localName;

        public LocalizationData(string localizationCode, string localName, bool isRightToLeft = false)
        {
            this.localizationCode = localizationCode;
            this.localName = localName ?? localizationCode;
            this.isRightToLeft = isRightToLeft;
        }

        public string LocalName
        {
            get
            {
                if (isRightToLeft == false)
                {
                    return localName;
                }
                else
                {
                    return LocalizationTable.ReverseString(localName);
                }
            }

            set
            {
                LocalName = value;
            }
        }

        public override string ToString()
        {
            return localizationCode + "," + isRightToLeft + "," + LocalName;
        }
    }
}
