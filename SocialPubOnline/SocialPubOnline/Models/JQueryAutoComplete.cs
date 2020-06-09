using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SocialPubOnline.Models
{
    public class JQueryAutoComplete
    {
        public int id { get; set; }
        public string value { get; set; }

        public JQueryAutoComplete(int IDutilizador, string letra)
        {
            value = letra;
            id = IDutilizador;
        }       
    }
}