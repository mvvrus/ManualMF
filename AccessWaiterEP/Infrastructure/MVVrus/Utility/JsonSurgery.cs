using System;

namespace MVVrus.Utility
{
    static public class JsonSurgery
    {
        //Extract value of the object field FieldName as JSON string. Return JSON string with the field removed as Rest
        public static String ExtractField(String Source, String FieldName, out String Rest)
        {
            FieldName = FieldName.ToLower(); //All field names to be translated to lowercase
            //Check that we have an object representation
            if (!IsObjectJson(Source)) throw new ArgumentException("Argument is not a JSON object", "Source");
            int pos = Source.IndexOf('{') + 1; //Current looking position, begin just after object specifier
            int field_start_pos = pos;      //Possible start position for removing, the same as start looking position
            String result=null;             //Value to return, defaults to null if no required field found or JSON is malformed somehow
            Rest = null;
            Boolean is_first_field=true;    //The first field processing flag
            int name_start, name_length;
            if(pos>0) do                    //Loop through all fields until we found the required field
            {
                pos = FindFieldName(Source, pos, out name_start, out name_length);       //Get field name and skip to the position of value
                if (pos < 0 || FieldName.Length==name_length 
                    && String.CompareOrdinal(FieldName, 0, Source, name_start, name_length)==0) break;     //No more fields or the required field found
                is_first_field = false;                                 //Reset first field processing flag
                pos = SkipFieldValue(Source, pos);                      //Skip to the position after the value
                field_start_pos = pos;                                  //Remember the possible start position for removing, before delimeter
                if (pos >= 0) pos = SkipFieldDelimiter(Source, pos);    //Skip field delimiter (',') if any
            } while (pos >= 0);             //Stop if all fields scanned
            if (pos > 0)                    //The field with required name found
            {
                int start_value_pos = pos;                              //Remeber the field value value start position
                pos = SkipFieldValue(Source, pos);                      //Skip to field end position
                if (pos > 0)                                            //End of value found (otherwise do not extract the field value)
                {
                    result = Source.Substring(start_value_pos, pos - start_value_pos);  //Extract the field value
                    if (is_first_field ) pos = SkipFieldDelimiter(Source,pos);          //For the first field skip to next field delimiter (if any) to remove it
                    Rest = Source.Remove(field_start_pos, pos - field_start_pos);       //Remove complete field (with name, value, and start or stop delimeter whatever exists)
                }
            }
            return result;
        }

        //Insert field FieldName with value FieldValue into JSON Object string Source
        public static String InjectField(String Source, String FieldName, String FieldValue)
        {
            if (!IsObjectJson(Source)) throw new ArgumentException("Argument is not a JSON object", "Source");
            String to_insert = "\"" + FieldName.ToLower() + "\" : " + FieldValue;   //Create data to be inserted
            int pos = SkipWhiteSpaceBackwards(Source, Source.Length - 1);           //Find position to insert (at the end delimiter)
            int pos2 = SkipWhiteSpaceBackwards(Source, pos - 1);                    //Find previous non-whitespace character
            if (pos2 >= 0 && Source[pos2] != '{') to_insert = ", " + to_insert;     //Add value delimiter after the previous value to the data to be inserted
            return Source.Insert(pos,to_insert);                                    //Return result of insertion
        }

        //Return position after the next JSON object field delimeter (',') after Pos,
        //or return Pos if no more field delimiters
        private static int SkipFieldDelimiter(string Source, int Pos)
        {
            int result=Pos; //If Pos<0 or no more delimiters - return Pos unchanged
            if (Pos >= 0)
            {
                int pos = SkipWhiteSpace(Source, Pos);  //Skip possible whitespaces
                if (pos >= 0 && pos < Source.Length && ',' == Source[pos]) result=pos+1; //Return postion after delimiter if that present
            }
            return result;
        }

        const String ws = " \t\r\n";  //Whitespace characters to be skipped

        //Skip from Pos to the next non-whitespace character if any, return -1 if none
        private static int SkipWhiteSpace(string Source, int Pos)
        {
            int result = Pos;           //The start position
            if (result >= 0) while (result < Source.Length && ws.IndexOf(Source[result]) >= 0) result++; //Perform skipping
            if (result >= Source.Length) result = -1; //No more no-whitespace characters, return -1
            return result;
        }

        //Skip backwards from Pos to the previous non-whitespace character if any, return -1 if none
        private static int SkipWhiteSpaceBackwards(string Source, int Pos)
        {
            int result = Pos;           //The start position
            if (result >= 0) while (result >=0 && ws.IndexOf(Source[result]) >= 0) result--; //Perform skipping
            return result;
        }


        //Find next top-level field name in JSON object string starting from Pos, 
        // copy it start position and length to NameStart and NameLenth respectively. 
        // Return position after name delimiter (':') or -1 if no more field names found
        private static int FindFieldName(string Source, int Pos, out int NameStart, out int NameLength)
        {
            int result = Pos;
            NameStart = NameLength = -1;        //Initialize output parameters for unsuccessful return
            if (Pos < 0) return -1;             //Do nothing if Pos is negative
            result = Source.IndexOf('\"', Pos); //Search for the starting double quote
            if (result < 0) return -1;          //Starting double quote not found
            NameStart = result + 1;             //Set name start output parameter
            if (NameStart >= Source.Length) return -1;  //return -1 if it's not within Source string
            result = Source.IndexOf('\"', NameStart);   //Search for the ending double quote
            if (result < 0) return -1;          //Ending double quote not found 
            NameLength = result - NameStart;    //Compute name length and set output parameter
            if (NameLength <= 0) return -1;     //Return -1 if the name is empty
            result = Source.IndexOf(':',result);//Skip to delimiter between name and value
            if (result >= 0) result++;          //Advance one char to skip the delimiter
            return result<Source.Length?result:-1;
        }

        // Char arrays to use in SkipFieldValue
        static readonly char[] literal_end = { ',', '}' };          //Characters to search for to find non-string literal end (in JSON object string)
        static readonly char[] square_symbols = { '[', ']', '\"' }; //Characters to search for in object value processing
        static readonly char[] curled_symbols = { '{', '}', '\"' }; //Characters to search for in array value processing

        //Returns next index after the last character of the field value part, started at Pos (-1 if inde
        private static int SkipFieldValue(string Source, int Pos)
        {
            char[] symbols = null;
            int pos = SkipWhiteSpace(Source, Pos);      //Skip starting whitespaces
            if(pos<0) return -1;                        //Return -1 if no more than whitespaces left
            switch(Source[pos]) {                       //Determine value type
                case '\"':                          //String literal value
                    return SkipString(Source, pos);         //Skip to the end of the literal
                case '[':                           //Array literal value        
                    symbols = square_symbols;               //Prepare to skip array value (possible nested)
                    break;
                case '{':                           //Object value
                    symbols = curled_symbols;               //Prepare to skip object value (possible nested)
                    break;
                default:                            //Non-string literal value
                    return Source.IndexOfAny(literal_end, pos); //Skip to the ending character (field delimiter or an end of the entire object)
            }
            //Come here only if we prepared to skip possibly nested array or object value
            if (++pos >= Source.Length) return -1;  //Skip the opening delimiter, return -1 if no more characters left
            int depth = 1;                          //Set initial nesting depth value
            while (depth > 0)                       //Process while the outmost closing delimiter not found
            { 
                pos = Source.IndexOfAny(symbols, pos);  //Search for opening, closing, or string literal delimiter
                if (pos < 0) return -1;                 //No more delimiters found
                int i = Array.IndexOf<char>(symbols, Source[pos]);  //See what type of delimiter was found
                switch (i)
                {
                    case 0:     //Opening delimiter (nested)
                        if (++pos >= Source.Length) return -1;  //Skip the delimiter, return -1 if no more characters left
                        depth++;                                //Increment nesting depth value
                        break;
                    case 1:     //Closing delimiter 
                        if (++pos >= Source.Length) return -1;  //Skip the delimiter, return -1 if no more characters left
                        depth--;                                //Decrement nesting depth value
                        break;
                    case 2:     //String literal delimiter     
                        pos = SkipString(Source, pos);          //Skip string literal
                        break;
                    default:    //Something went wrong
                        return -1;
                }
            }
            return pos;
        }

        //Skip string literal. Return the position after the closing double quote, or -1 if none found
        private static int SkipString(string Source, int Pos)
        {
            int pos = Pos;                          //Current processing position, set it to start value
            if (pos < 0) return -1;                 //Return -1 if the position is invalid
            if (++pos >= Source.Length) return -1;  //Skip starting double quote. Return -1 if it is the last character
            Boolean not_delimiter = true;           //Flag to indicate that the last double quote found is not the ending delimiter
            while (not_delimiter)                   //Repeat while an ending delimiter is not found
            {
                pos = Source.IndexOf('\"', pos);        //Search for the next double quote
                if (pos < 0) return -1;                 //No more double quotes
                not_delimiter = '\\' == Source[pos - 1];//If double quote is escaped it is not a string delimiter
                if (++pos >= Source.Length) return -1;  //Skip the double quote found. Return -1 if it is the last character 
            }
            return pos;                             //Return the position after the closing double quote
        }

        private static Boolean IsObjectJson(String Source)
        {
            int pos = SkipWhiteSpace(Source, 0);                    //Find the first non-whitespace character
            if (pos < 0 || Source[pos] != '{') return false;        //Return false if it is not a JSON Object start delimiter ('{')
            pos = SkipWhiteSpaceBackwards(Source, Source.Length - 1);//Find the last non-whitespace character
            return (pos >= 0 && '}' == Source[pos]);                //Return true if it is a JSON Object end delimiter ('}'), otherwise return false
        }

    }
}
