using System;
using System.Collections.Specialized;
using System.IO;
using System.Xml;

namespace ManualMF
{
    static class Configuration
    {
        static public String DBConnString = @"Server=.\SQLExpress;Integrated Security=true;Database=ManualMF";

        static public void ReadConfiguration(Stream ConfigStream)
        {
            NameValueCollection appSettings = new NameValueCollection();
            NameValueCollection connectionStrings = new NameValueCollection();

            XmlReaderSettings settings = new XmlReaderSettings() { CloseInput = true, IgnoreProcessingInstructions = true, IgnoreWhitespace = true };
            using (XmlReader rdr = XmlReader.Create(ConfigStream, settings))
            {
                if (rdr.MoveToContent() == XmlNodeType.Element && "configuration" == rdr.Name)
                {
                    using (XmlReader config_rdr = rdr.ReadSubtree())
                    {
                        config_rdr.Read(); //Read <configuration> element start node
                        while (config_rdr.Read())
                        {
                            if (config_rdr.NodeType == XmlNodeType.EndElement) break;
                            if (config_rdr.NodeType != XmlNodeType.Element) continue; //Skip non-element
                            if (config_rdr.Name == "appSettings") ReadSection(config_rdr.ReadSubtree(), "key", "value", appSettings); //Read <appSettings> section
                            else if (config_rdr.Name == "connectionStrings") ReadSection(config_rdr.ReadSubtree(), "name", "connectionString", connectionStrings); //Read <connectionStrings> section
                            else config_rdr.ReadSubtree().Close(); //Skip entire element contents to its end tag 
                        }
                    }
                }
            }
            //TODO Set DBConnString
            String new_dbconnstring = appSettings["DBConnString"];
            if (null == new_dbconnstring) new_dbconnstring = connectionStrings["Default"];
            if (null != new_dbconnstring) DBConnString = new_dbconnstring;
        }

        private static void ReadSection(XmlReader Rdr, string NameAttribute, string ValueAttrubute, NameValueCollection Contents)
        {
            //Only <add/> elements supported
            try
            {
                if (Rdr.IsStartElement())
                {
                    while (Rdr.Read())
                    {
                        if (Rdr.NodeType == XmlNodeType.EndElement) break;
                        if (Rdr.NodeType != XmlNodeType.Element) continue; //Skip non-element
                        if (Rdr.Name == "add") Contents.Add(Rdr[NameAttribute], Rdr[ValueAttrubute]);
                        if (!Rdr.IsEmptyElement) Rdr.ReadSubtree().Close(); //Skip entire element contents to its end tag 
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
