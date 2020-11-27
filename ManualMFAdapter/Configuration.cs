using System;
using System.Collections.Specialized;
using System.IO;
using System.Xml;

namespace ManualMF
{
    //This class contains a function to read ManualMFAdapter configuration from XML stream
    static class Configuration
    {
        //Default connection string for ManualMFAdapter
        static public String DBConnString = @"Server=.\SQLExpress;Integrated Security=true;Database=ManualMF";
        static public int DecisionTimeMinutes = 15;

        //The function to read ManualMFAdapter configuration from XML stream
        static public void ReadConfiguration(Stream ConfigStream)
        {
            //The function search for connection string in two places: 
            // in the appSettings section (key="DBConnString")
            // and in the connectionString section (connection name is "Default")
            NameValueCollection appSettings = new NameValueCollection(); //Data from appSettings section (key/value pairs)
            NameValueCollection connectionStrings = new NameValueCollection(); ////Data from connectionStrings section (name/connectionString pairs)

            XmlReaderSettings settings = new XmlReaderSettings() { CloseInput = true, IgnoreProcessingInstructions = true, IgnoreWhitespace = true };
            using (XmlReader rdr = XmlReader.Create(ConfigStream, settings)) //Create XML stream to read data
            {
                if (rdr.MoveToContent() == XmlNodeType.Element && "configuration" == rdr.Name) //A root section must be <configuration>
                {
                    using (XmlReader config_rdr = rdr.ReadSubtree()) //Read root section
                    {
                        config_rdr.Read(); //Read <configuration> element start node
                        while (config_rdr.Read())
                        {
                            if (config_rdr.NodeType == XmlNodeType.EndElement) break; //End of the section. We've done
                            if (config_rdr.NodeType != XmlNodeType.Element) continue; //Skip non-element
                            if (config_rdr.Name == "appSettings") ReadSection(config_rdr.ReadSubtree(), "key", "value", appSettings); //Read <appSettings> section
                            else if (config_rdr.Name == "connectionStrings") ReadSection(config_rdr.ReadSubtree(), "name", "connectionString", connectionStrings); //Read <connectionStrings> section
                            else config_rdr.ReadSubtree().Close(); //Skip any other section to its end tag 
                        }
                    }
                }
            }
            //Set DBConnString
            String new_dbconnstring = appSettings["DBConnString"]; //Try to use data from <appSetiings>
            if (null == new_dbconnstring) new_dbconnstring = connectionStrings["Default"]; //None? Try <connectionStrings> next
            if (null != new_dbconnstring) DBConnString = new_dbconnstring; //If connection string was found in configuration data, remeber it
            //Set DecisionTimeMinutes
            String new_DecisionTimeMinutesString = appSettings["DecisionTimeMinutes"];
            int new_DecisionTimeMinutes;
            if (new_DecisionTimeMinutesString != null &&
                Int32.TryParse(new_DecisionTimeMinutesString, out new_DecisionTimeMinutes) &&
                new_DecisionTimeMinutes > 0
            ) 
                DecisionTimeMinutes = new_DecisionTimeMinutes;
        }

        //Function to read the name/value pairs of the specified attributes from <add/> elements in the current processing section
        //Parameters: 
        // Rdr - XML reader for the section
        // NameAttribute,ValueAttribute - attribute names for name and value attributes respectively
        // Contents - name/value collection with results
        private static void ReadSection(XmlReader Rdr, string NameAttribute, string ValueAttrubute, NameValueCollection Contents)
        {
            //Only <add/> elements supported
            try
            {
                if (Rdr.IsStartElement())
                {
                    while (Rdr.Read())
                    {
                        if (Rdr.NodeType == XmlNodeType.EndElement) break; //End of section
                        if (Rdr.NodeType != XmlNodeType.Element) continue; //Skip non-element
                        if (Rdr.Name == "add") Contents.Add(Rdr[NameAttribute], Rdr[ValueAttrubute]);
                        if (!Rdr.IsEmptyElement) Rdr.ReadSubtree().Close(); //Skip entire non-<add> element contents to its end tag 
                    }
                }
            }
            finally
            {
                Rdr.Close();
            }
            return;
        }
    }
}
