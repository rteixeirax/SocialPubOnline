using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SocialPubOnline.Models
{
    public class C_Notificao
    {        
        public int IDgrupo { get; set; }
        public string NomeGrupo { get; set; }       
        public int IDutilizador { get; set; }
        public string UtilizadorNome { get; set; }
        public int IDutilizadorConvidado { get; set; }   
    }
}