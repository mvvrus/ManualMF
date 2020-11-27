using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManualMF;
using System.IO;
using System.Text;

namespace ManualMFAdapter_UnitTests
{
    [TestClass]
    public class ConfigurationTest
    {
        static String[] ConfigData = new String[] {
            "<?xml version=\"1.0\"?>",
            "<configuration>",
            "    <appSettings>",
            "        <add key=\"DecisionTimeMinutes\" value=\"5\" />",
            "    </appSettings>",
            "    <connectionStrings>",
            @"        <add name=""Default"" providerName=""System.Data.SqlClient"" connectionString=""Server=(LocalDB)\v11.0; Integrated Security=true ;AttachDbFileName=C:\Projects\ManualMF\Database\ManualMF.mdf""/>",
            "    </connectionStrings>",
            "</configuration>"
        };
        [TestMethod]
        public void ReadConfigurationData()
        {
            String config = String.Join("\r\n", ConfigData);
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(config), false);
            Configuration.ReadConfiguration(stream);
            Assert.AreEqual(5, Configuration.DecisionTimeMinutes);
            Assert.AreEqual(@"Server=(LocalDB)\v11.0; Integrated Security=true ;AttachDbFileName=C:\Projects\ManualMF\Database\ManualMF.mdf", Configuration.DBConnString);
        }
    }
}
