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
    public enum FormMode { NormalForm, WaitMoreForm, AlreadyAuthForm, DeniedForm, ErrorForm, FinalCloseForm };

    //This class is used as a source of localized HTML fragments, returned by ManualMFPresentation instances
    //For each locale a single instance of this class is created
    static public class PublicStrings
    {
        public static String CancelButtonName { get { return HtmlFragmentSupplier.CancelButtonName; } }
        public static String FinalCloseFragment(int Lcid) {
            HtmlFragmentSupplier temp_supplier = HtmlFragmentSupplier.GetFragmentSupplier(Lcid,"");
            return temp_supplier.GetFragment(FormMode.FinalCloseForm);
        }
    }

    class VarSubstitute
    {
        static internal void SubstituteVars(StringBuilder Template, String LeftDelimiter, String RightDelimiter, NameValueCollection VarSource)
        {
            SubstituteVars(Template, LeftDelimiter, RightDelimiter, VarSource, new HashSet<String>());
        }

        static void SubstituteVars(StringBuilder Template, String LeftDelimiter, String RightDelimiter, NameValueCollection VarSource, ISet<String> ExpandedVars)
        {
            int ndx; //Position of left delimiter found
            int pos = 0; //Position from which start search
            do
            {
                ndx = Template.ToString().IndexOf(LeftDelimiter, pos); //Search for left delimiter denoting variable to substite
                if (ndx >= 0)
                {  //Left delimiter found
                    int lmgn = ndx; //Start of substring to be replaced (or removed if no value for the variable will be found)
                    pos = ndx + LeftDelimiter.Length; //Skip RightDelimiter found - to avoid endless loop in the case where no RightDelmiter exists
                    int right_pos = Template.ToString().IndexOf(RightDelimiter, pos); //Search for the right delimiter
                    if (right_pos >= 0)
                    { //Right delimiter found
                        int rmgn = right_pos + RightDelimiter.Length; //End of substring to replace
                        String var_name = Template.ToString(pos, right_pos - pos); //Extract variable name
                        if (ExpandedVars.Contains(var_name)) throw new ArgumentException("Circular reference to variable: " + var_name);
                        String subst = VarSource[var_name]; //Get variable value
                        Template.Remove(lmgn, rmgn - lmgn); //Remove variable name with delimiters
                        if (subst != null)
                        { //Variable has value
                            ExpandedVars.Add(var_name);
                            StringBuilder subst_builder = new StringBuilder(subst);
                            SubstituteVars(subst_builder, LeftDelimiter, RightDelimiter, VarSource, ExpandedVars);
                            subst = subst_builder.ToString();
                            ExpandedVars.Remove(var_name);
                            Template.Insert(lmgn, subst); //Insert variable value
                            pos = lmgn + subst.Length; //Correct position to start new search
                        }
                        else pos = lmgn; //No value for the variable found, just remove variable with delimiters (done earlier) and search in the remaining part
                    }
                }
            }
            while (ndx >= 0 && pos < Template.Length);
        }

    }

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
            s_LocalizedStrings = new ResourceManager(assembly_name + ".LocalizedStrings", typeof(HtmlFragmentSupplier).Assembly);
            s_Fragments = GetStringsForCulture(html_fragments_resource,new CultureInfo("en"));
        }

        //Name of Cancel button (from resources)
        static internal String CancelButtonName { get { return s_Fragments[CANCELBUTTON]; } }

        NameValueCollection m_LocalizedStrings;
        String m_Upn;

        private HtmlFragmentSupplier(int lcid, string Upn)
        {
            m_LocalizedStrings = GetStringsForCulture(s_LocalizedStrings, new CultureInfo(lcid));
            m_Upn = Upn;
        }

        public static HtmlFragmentSupplier GetFragmentSupplier(int lcid, string Upn)
        {
            HtmlFragmentSupplier result;
            if (!s_suppliers.TryGetValue(lcid, out result)) {
                //No instance for specified locale exists yet. Create it/
                result = new HtmlFragmentSupplier(lcid,Upn);
                s_suppliers.Add(lcid, result);
            }
            return result;
        }

        //Create HTML fragment to return
        String GetFragment(FormMode Mode, AccessDeniedReason Reason, String ErrorMessage, int? Token=null)
        {
            String fragment_name = Enum.GetName(typeof(FormMode), Mode); //Get resource string name for the fragment template
            String html_template = s_Fragments[fragment_name]; //Extract fragment template from resources
            if (null == html_template) return null;
            //Extract localized deny reason
            String deny_reason = m_LocalizedStrings[Enum.GetName(typeof(AccessDeniedReason), Reason)];
            //Make buffer to process the template
            StringBuilder html_under_construction = new StringBuilder(html_template,4*html_template.Length);
            VarSubstitute.SubstituteVars(html_under_construction, "[!","]", s_Fragments); //Subtitute culture-invariant vars in the fragemnt, if any exists
            VarSubstitute.SubstituteVars(html_under_construction, "[#", "]", m_LocalizedStrings); //Subtitute localized strings in the fragemnt, if any exists
            //Create collection of the program variables
            NameValueCollection vars = new NameValueCollection();
            if (deny_reason != null) vars.Add("DenyReason", deny_reason); //Add reason why the access is denied 
            if (ErrorMessage != null) vars.Add("ErrorMessage", ErrorMessage); //Add ErrorMessage if any
            if (Token != null) vars.Add("EPAccessToken", Token.Value.ToString());  //Add EndpointAccessToken token if any
            vars.Add("Upn", m_Upn);
            VarSubstitute.SubstituteVars(html_under_construction, "#", "#", vars); //Subtituprogram variables in the fragemnt, if any exists
            String result = html_under_construction.ToString();
            return result;
        }

        //Alternative call forms
        public String GetFragment(FormMode Mode)
        {
            return GetFragment(Mode, AccessDeniedReason.UnknownOrNotDenied, null);
        }

        public String GetFragment(FormMode Mode,int? Token)
        {
            return GetFragment(Mode, AccessDeniedReason.UnknownOrNotDenied, null,Token);
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
