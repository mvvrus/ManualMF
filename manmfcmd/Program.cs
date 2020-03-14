using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Resources;
using System.Configuration;
using System.Data;
using System.DirectoryServices;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ManualMF
{
    class Program
    {
        const String UPN_PROPNAME = "userPrincipalName";
        const String DISPLAYNAME_PROPNAME = "displayName";
        static ResourceSet LocStrings;
        static TimeSpan DefValidityPeriod;
        struct OutCol
        {
            public String OutName;
            public String TableColName;
            public Int32 Width;
            public OutCol(String _TableColName, String _OutName) { OutName = _OutName; TableColName = _TableColName; Width = OutName.Length; }
        }

        static void Main(string[] args)
        {
            /* Command format
              manmfcmd list [-a]
              manmfcmd allow <upn> [-s] [-t:<TimeSpan>]
              manmfcmd deny <upn> [-t:<TimeSpan>]
              manmfcmd clear <upn>
            */
            //Perform some initialization
            // Load resource set with strings localized to current user culture;
            System.Reflection.Assembly assembly = typeof(Program).Assembly;
            ResourceManager res_mgr = new ResourceManager(assembly.GetName().Name + ".constrings", assembly);
            LocStrings = res_mgr.GetResourceSet(System.Threading.Thread.CurrentThread.CurrentCulture, true, true);
            // Read validity period from application config file
            DefValidityPeriod=new TimeSpan(2, 0, 0); //Hard-coded default if no/bad config data
            String vp_str = ConfigurationManager.AppSettings["ValidityPeriod"];
            if(vp_str!=null) try
            {
                DefValidityPeriod = TimeSpan.Parse(vp_str);
            }
            catch { /*Ignore any syntax errors in the parameter value, leaving hard-coded default*/};

            if (args.Length > 0)
            {
                String[] other_args; //Array of unprocessed arguments
                SqlConnection conn = null;
                NameValueCollection switch_values; //Holds values of switches from command line after parsing via SwitchValues method
                try
                {
                    conn = new SqlConnection(ConfigurationManager.AppSettings["DBConnString"] ?? @"Server=.\SQLExpress;Integrated Security=true;Database=ManualMF"); 
                    if ("list" == args[0])
                    { //Process "list" command 
                        //Skip the first argument
                        other_args = new String[args.Length - 1]; 
                        Array.Copy(args, 1, other_args, 0, other_args.Length );
                        //Check for -a switch (and overall syntax)
                        switch_values = SwitchValues(other_args,new String[]{"a"});
                        Boolean show_all = switch_values["a"] != null;
                        //Form query string to fill a table in the dataset to be created
                        String query_str = "SELECT UPN, REQUEST_STATE, VALID_UNTIL, FROM_IP FROM dbo.PERMISSIONS WHERE VALID_UNTIL>SYSDATETIME()";
                        if (!show_all) query_str += " AND REQUEST_STATE=0";
                        using(DataSet ds=new DataSet()){
                            //Fill a table from the database
                            SqlDataAdapter adapter = new SqlDataAdapter(query_str, conn);
                            adapter.Fill(ds, "PERMISSIONS");
                            DataTable list_table = ds.Tables[0];
                            if (list_table.Rows.Count > 0)
                            { //Have some results - print them
                                //Add column for FullName from Active Directory...
                                list_table.Columns.Add("FULLNAME", typeof(String));
                                // ...IP address the request came from an from which the access is granted...
                                list_table.Columns.Add("IPADDRESS", typeof(String));
                                // ...request validity time...
                                list_table.Columns.Add("VALID_UNTIL_STR", typeof(String));
                                // ... and string representation of request state if -a switch is used
                                if (show_all) list_table.Columns.Add("STATE", typeof(String));
                                //Resolve full names of users via AD connection
                                DirectorySearcher gc_searcher = null;
                                try
                                {
                                    // Get root domain DN
                                    DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE");
                                    String forest_root = (String)rootDSE.Properties["rootDomainNamingContext"][0];
                                    DirectoryEntry gc_root = new DirectoryEntry("GC://" + forest_root);
                                    gc_searcher = new DirectorySearcher();
                                    // Create DirectorySearcher to search for UPN in GC (i.e. in entire forest)
                                    gc_searcher.SearchRoot = gc_root;
                                    // Prepare to search for UPN 
                                    gc_searcher.PropertiesToLoad.Add(DISPLAYNAME_PROPNAME);
                                }
                                catch (Exception ex)
                                {
                                    //Print warnings about user names unavailability
                                    Console.WriteLine(LocStrings.GetString("ADWarning"));
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(LocStrings.GetString("ADWarning2"));
                                    Console.WriteLine();
                                }
                                String upn_filter = "(" + UPN_PROPNAME + "={0})"; //Format string to pass UPN to the DirectorySearcher filter
                                // Enumerate all records to display and fill user's displayName to each record
                                foreach (DataRow row in list_table.Rows)
                                {
                                    //Try to get full name from Active directory
                                    row["FULLNAME"] = "??????";
                                    if (gc_searcher != null)
                                    {
                                        gc_searcher.Filter = (String.Format(upn_filter, row["UPN"]));
                                        try
                                        {
                                            SearchResult s_res = gc_searcher.FindOne();
                                            if (s_res != null) row["FULLNAME"] = s_res.Properties[DISPLAYNAME_PROPNAME][0];
                                        }
                                        catch { /*Eat any exceptions*/}
                                    }
                                    // Convert IP address to string form
                                    row["IPADDRESS"] = "";
                                    if (!row.IsNull("FROM_IP")) 
                                        //Put text IP address representation into IPADDRESS field
                                        row["IPADDRESS"] = (new IPAddress((byte[])row["FROM_IP"])).ToString();
                                    //Fill VALID_UNTIL_STR
                                    row["VALID_UNTIL_STR"] = row["VALID_UNTIL"].ToString();
                                    if (show_all)
                                    {
                                        //Translate REQUEST_STATE fileld numeric value into a localized string value and put it into STATE filed
                                        String[] state_names = { "Pending", "Allowed", "Denied" }; //Names of resource strings for each state
                                        try
                                        {
                                            row["STATE"] = LocStrings.GetString(state_names[(int)(byte)(row["REQUEST_STATE"])]);
                                        }
                                        catch (IndexOutOfRangeException)
                                        {
                                            //Bad data in the database. Don't fail, just exhibit that smth strange occured
                                            row["STATE"] = "???";
                                        }
                                    }
                                    list_table.AcceptChanges();
                                }
                                //Print list of records with all required information
                                // Define columns for output
                                OutCol[] out_cols;
                                if (show_all) { out_cols = new OutCol[] { new OutCol("FULLNAME", LocStrings.GetString("Name")), new OutCol("UPN", "UPN"), new OutCol("VALID_UNTIL_STR", LocStrings.GetString("Expiration")), new OutCol("IPADDRESS", LocStrings.GetString("IPAddress")), new OutCol("STATE", LocStrings.GetString("State")) }; }
                                else out_cols = new OutCol[] { new OutCol("FULLNAME", LocStrings.GetString("Name")), new OutCol("UPN", "UPN"), new OutCol("VALID_UNTIL_STR", LocStrings.GetString("Expiration")), new OutCol("IPADDRESS", LocStrings.GetString("IPAddress")) };

                                // Determine widths of the columns for output
                                //  Accumulate widths of columns.
                                foreach (DataRow row in list_table.Rows)
                                    for (int i = 0; i < out_cols.Length; i++)
                                        out_cols[i].Width = Math.Max(out_cols[i].Width, ((String)row[out_cols[i].TableColName]).Length);
                                //  Compute space left in console string
                                int delta = Console.BufferWidth - out_cols.Length - out_cols.Select(out_col => out_col.Width).Sum();
                                if (delta < 0) //output string length exceeds console string length
                                { //Sacrifice Name column length
                                    //...but save space for the header
                                    out_cols[0].Width = Math.Max(out_cols[0].Width + delta, out_cols[0].OutName.Length);
                                }
                                //Print column headers
                                foreach (OutCol out_col in out_cols)
                                    Console.Write(out_col.OutName.PadRight(out_col.Width+1));
                                Console.WriteLine();
                                //Print record contents
                                foreach (DataRow row in list_table.Rows)
                                {
                                    foreach (OutCol out_col in out_cols)
                                    {
                                        String val = (String)row[out_col.TableColName];
                                        if (val.Length > out_col.Width) Console.Write(val.Substring(0, out_col.Width - 3) + "... ");
                                        else Console.Write(val.PadRight(out_col.Width + 1));
                                    }
                                    Console.WriteLine();
                                }
                            }
                            else Console.WriteLine(LocStrings.GetString("NoData"));
                        }
                    }
                    else 
                    { //Process all other commands
                        Boolean result = false; //Variable to show was a command actually performed
                        if (args.Length < 2) throw new Exception(LocStrings.GetString("MissingUpn")); 
                        String upn; //User principal name - the first argument of any AccessController method
                        upn = args[1]; //Extract UPN from the 2-nd command line argument 
                        if(!Regex.IsMatch(upn,@"\A[^@]+@[A-Z,a-z,0-9][A-Z,a-z,0-9,\-]*(\.[A-Z,a-z,0-9][A-Z,a-z,0-9,\-]*)*\Z")) //Check UPN format
                            throw new ArgumentException(LocStrings.GetString("BadUpn")+": <user>@<domain.name>"); //Throw error if it is invalid
                        //Skip first 2 arguments (processed)
                        other_args = new String[args.Length - 2];
                        Array.Copy(args, 2, other_args, 0, other_args.Length);
                        //Open connection to use with an AccessController
                        conn.Open();
                        //Create an AccessController object to handle commands
                        using (AccessController ac = new AccessController(conn))
                        {
                            //Switch according to command to call appropriate AccessController method
                            switch (args[0])
                            {
                                case "allow": //Process "allow" command
                                    //Check for -s and -t switches (and overall syntax)
                                    switch_values = SwitchValues(other_args, new String[] { "s", "t" });
                                    //Call AccessController.Allow method to perform the command
                                    result = ac.Allow(upn, ComputeValidUntil(switch_values), switch_values["s"] != null);
                                    break;
                                case "deny": //Process "deny" command
                                    //Check for -t switch (and overall syntax)
                                    switch_values = SwitchValues(other_args, new String[] { "t" });
                                    //Call AccessController.Deny method to perform the command
                                    result = ac.Deny(upn, ComputeValidUntil(switch_values));
                                    break;
                                case "clear": //Process "clear" command
                                    //Check for excessive parameters in the command line
                                    switch_values = SwitchValues(other_args, new String[] { "t" });
                                    //Call AccessController.Deny method to perform the command
                                    result = ac.Clear(upn);
                                    break;
                                default: //Unknown command
                                    throw new ArgumentException(LocStrings.GetString("InvalidParameter") + ": ", args[0]);
                            }
                        }
                        //Report result of the command performed
                        Console.WriteLine(LocStrings.GetString(result ? "Done" : "NothingDone")); 
                    }
                }
                catch (Exception ex)
                {//Somthing went wrong
                    //Print exception message
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    if (conn != null) conn.Dispose();
                }
                return;
            }
            //Write usage information
            Console.WriteLine("Usage:");
            Console.WriteLine(" manmfcmd list [-a]");
            Console.WriteLine(" manmfcmd allow <upn> [-s] [-t:<TimeSpan>]");
            Console.WriteLine(" manmfcmd deny <upn> [-t:<TimeSpan>]");
            Console.WriteLine(" manmfcmd clear <upn>");

        }

        //Compute ValidUntil parameter for AccessControl methods from command line or use default values
        static DateTime ComputeValidUntil(NameValueCollection SwitchValues)
        {
            TimeSpan validity_period = DefValidityPeriod; //First of all try use the default time interval
            String vp_str = SwitchValues["t"]; //See if user specifies a time interval in the command line
            if (vp_str != null) validity_period = TimeSpan.Parse(vp_str); //If so - try to use it
            return DateTime.Now + validity_period; //Return current time incremented by the time interval
        }

        const String SWITCH_CHARS = "-/";

        static NameValueCollection SwitchValues(String[] Args, String[] ValidSwitches)
        {
            NameValueCollection result = new NameValueCollection(); //The resulting collection (initially empty)
            foreach (String arg in Args)
            { //Enum each argument in Args array
                if (SWITCH_CHARS.IndexOf(arg[0]) >= 0)
                { //It's a switch
                    string arg_name, arg_value=""; //switch name & value (defaults to an empty string) to extract
                    string[] arg_namevalue = arg.Substring(1).Split(new char[]{':'}, 2); //extract - split argument by the first ':' symbol if present
                    arg_name = arg_namevalue[0]; //switch name
                    if (arg_namevalue.Length > 1) arg_value = arg_namevalue[1]; //switch value (if exists)
                    //Check for the switch to be a vakid one
                    if (Array.IndexOf(ValidSwitches, arg_name) < 0) throw new ArgumentException("InvalidSwitch" + ": ", arg);
                    //Add a name and value pair for the switch found
                    result.Add(arg_name, arg_value);
                }
                else throw new ArgumentException(LocStrings.GetString("InvalidParameter")+": ",arg); //It's not a switch - raise error
            }
            return result;
        }
    }

}
