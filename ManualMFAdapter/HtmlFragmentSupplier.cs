using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;


namespace ManualMF
{
    public enum FormMode { NormalForm, WaitMoreForm, AlreadyAuthForm, DeniedForm, ErrorForm, FinalClose };

    //This class is used as a source of localized HTML fragments, returned by ManualMFPresentation instances
    //For each locale a single instance of this class is created

    class HtmlFragmentSupplier
    {
        static Dictionary<int, HtmlFragmentSupplier> s_suppliers;
        static ResourceManager s_LocalizedStrings;
        static NameValueCollection s_Fragments;
        const String CANCELBUTTON = "CancelButton";

        static NameValueCollection GetStringsForCulture(ResourceManager ResManager, CultureInfo culture)
        {
            ResourceSet resources;
            try
            {
                resources = ResManager.GetResourceSet(culture, true, true);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            NameValueCollection result = new NameValueCollection();
            foreach (DictionaryEntry res in resources)
            {
                if (res.Value is String) result.Add((String)res.Key, (String)res.Value);
            }
            return result;
        }

        static HtmlFragmentSupplier()
        {
            s_suppliers = new Dictionary<int, HtmlFragmentSupplier>();
            System.Reflection.Assembly assembly = typeof(HtmlFragmentSupplier).Assembly;
            String assembly_name = assembly.GetName().Name;
            ResourceManager html_fragments_resource = new ResourceManager(assembly_name+".HTMLFragments", assembly);
            html_fragments_resource.IgnoreCase = true;
            String test = html_fragments_resource.GetString("REQUIRED_HIDDEN",new CultureInfo("en"));
            s_LocalizedStrings = new ResourceManager(assembly_name + ".LocalizedStrings", typeof(HtmlFragmentSupplier).Assembly);
            s_Fragments = GetStringsForCulture(html_fragments_resource,new CultureInfo("en"));
        }

        //Name of Cancel button (from resources)
        static public String CancelButtonName { get { return s_Fragments[CANCELBUTTON]; } }

        NameValueCollection m_LocalizedStrings;

        private HtmlFragmentSupplier(int lcid)
        {
            m_LocalizedStrings = GetStringsForCulture(s_LocalizedStrings, new CultureInfo(lcid));
        }

        public static HtmlFragmentSupplier GetFragmentSupplier(int lcid) {
            HtmlFragmentSupplier result;
            if (!s_suppliers.TryGetValue(lcid, out result)) {
                //No instance for specified locale exists yet. Create it/
                result = new HtmlFragmentSupplier(lcid);
                s_suppliers.Add(lcid, result);
            }
            return result;
        }

        static void SubstituteVars(StringBuilder Template, String LeftDelimiter, String RightDelimiter,NameValueCollection VarSource)
        {
            int ndx; //Position of left delimiter found
            int pos = 0; //Position from which start search
            do
            {
                ndx = Template.ToString().IndexOf(LeftDelimiter, pos); //Search for left delimiter denoting variable to substite
                if (ndx >= 0)
                {  //Left delimiter found
                    int lmgn = ndx; //Start of substring to be replaced (or removed if no value for the variable will be found)
                    pos = ndx+LeftDelimiter.Length; //Skip RightDelimiter found - to avoid endless loop in the case where no RightDelmiter exists
                    int right_pos = Template.ToString().IndexOf(RightDelimiter, pos); //Search for the right delimiter
                    if (right_pos >= 0) 
                    { //Right delimiter found
                        int rmgn = right_pos + RightDelimiter.Length; //End of substring to replace
                        String var_name = Template.ToString(pos, right_pos - pos); //Extract variable name
                        String subst = VarSource[var_name]; //Get variable value
                        Template.Remove(lmgn, rmgn - lmgn); //Remove variable name with delimiters
                        if (subst != null)
                        { //Variable has value
                            Template.Insert(lmgn, subst); //Insert variable value
                            pos = lmgn + subst.Length; //Correct position to start new search
                        }
                        else pos = lmgn; //No value for the variable found, just remove variable with delimiters (done earlier) and search in the remaining part
                    }
                }
            }
            while (ndx >= 0 && pos<Template.Length);
        }

        //Create HTML fragment to return
        String GetFragment(FormMode Mode, AccessDeniedReason Reason, String ErrorMessage)
        {
            String fragment_name = Enum.GetName(typeof(FormMode), Mode); //Get resource string name for the fragment template
            String html_template = s_Fragments[fragment_name]; //Extract fragment template from resources
            if (null == html_template) return null;
            String deny_reason = m_LocalizedStrings[Enum.GetName(typeof(AccessDeniedReason), Reason)];
            StringBuilder html_under_construction = new StringBuilder(html_template,4*html_template.Length);
            SubstituteVars(html_under_construction, "[!","]", s_Fragments); //Subtitute culture-invariant vars in the fragemnt, if any exists
            SubstituteVars(html_under_construction, "[#", "]", m_LocalizedStrings); //Subtitute localized strings in the fragemnt, if any exists
            String result = html_under_construction.ToString();
            //Show reason why the access is denied (if field for it is defined in the template)
            result = new Regex("#DenyReason#", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(result, deny_reason);
            //Insert ErrorMessage if any
            if (ErrorMessage != null)
                result=new Regex("#ErrorMessage#", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Replace(result, ErrorMessage);
            return result;
        }

        //Alternative call forms
        public String GetFragment(FormMode Mode)
        {
            return GetFragment(Mode, AccessDeniedReason.UnknownOrNotDenied, null);
        }

        public String GetFragment(FormMode Mode, String ErrorMessage)
        {
            return GetFragment(Mode, AccessDeniedReason.UnknownOrNotDenied, ErrorMessage);
        }
        public String GetFragment(FormMode Mode, AccessDeniedReason Reason)
        {
            return GetFragment(Mode, Reason, null);
        }



    }
}
