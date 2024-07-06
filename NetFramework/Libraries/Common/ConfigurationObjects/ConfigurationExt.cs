using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PissedEngineer.Primitives
{
    internal static class ConfigurationExt
    {
        public static bool InitFromString( this IConfigurationBase o,  
                                           string str, 
                                           out string errorMessage )
        {
            if ( string.IsNullOrEmpty( str ) ) {

                errorMessage = "Can't deserialize empty or null string.";
                return false;
            }

            try {

                JToken jToken= JToken.Parse(str);
                object res = jToken.ToObject(o.GetType());

                if ( res == null ) {

                    errorMessage = "Deserialisation failed.";
                    return false;
                }

                errorMessage = null;
                return  o.CopyFrom(res) ;
            } 
            catch (Exception ex ){ 

                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
