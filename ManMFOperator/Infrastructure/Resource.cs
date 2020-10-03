using System;
using System.Collections.Generic;
using System.Resources;
using System.Threading;
using System.Web.Mvc;

namespace ManMFOperator.Infrastructure
{
    //This class is intended to extract localized strings by their keys (unlocalized) according to the current UI culture
    //Localized strings are stored as parts of embedded resources in the current assembly
    class Resource
    {
        //Static members
        //List of objects of this class for all resources requested during the execution
        static Dictionary<String, Resource> s_ResMans=new Dictionary<string,Resource>();
        //A method to return a class instance for the specifie resource name
        static public Resource GetResource(String ResName)
        {
            //Try to create and add to the list an object for the resource specified if not created yet
            if (!s_ResMans.ContainsKey(ResName)) s_ResMans.Add(ResName, new Resource(ResName)); 
            //Return an object of this class for the specified resource
            return s_ResMans[ResName]; 
        }

        static public String GetResourceString(String ResName, String StringName)
        {
            return GetResource(ResName).GetResourceString(StringName);
        }

        //Instance members
        ResourceManager m_ResMan; //resoure manager for the specified resource
        //Initialize an instance of this class using specified resource name
        private Resource(String ResName)
        {
            //Load the resource manger for the resource with the name specified
            m_ResMan = new ResourceManager(typeof(Resource).Assembly.GetName().Name + "." + ResName, typeof(Resource).Assembly);
        }

        //Return a localized string specified by the key
        public String GetResourceString(String StringName) 
        {
            return m_ResMan.GetString(StringName, Thread.CurrentThread.CurrentUICulture)??StringName;
        }

    }

    //Helper class containing helper method(s) to be used with Razor templates
    public static class ResourceHtmlExtensions
    {
        public const String LOCALIZER = "Localizer"; //Name of the property in ViewData dictionary containing Resource object to be used for localization

        //Helper method returning localized string for the specified StringName
        //Rerurns StringName itself if appropriate resource is not found
        static public String LocalizedString(this HtmlHelper Html, String StringName)
        {
            Resource res=null; //Resource object to be used for localization
            Object temp; //Temporary variable to accept datat from ViewData dictionary
            //Try to find Resource object to use for the localization
            if (Html.ViewData.TryGetValue(LOCALIZER, out temp)) res = temp as Resource;
            if (null == res) return StringName; //Resource objestdoes not exist. Have to return StringName itself
            else return res.GetResourceString(StringName); //Return localized string if such exists or else return StringName itself
        }
    }
}