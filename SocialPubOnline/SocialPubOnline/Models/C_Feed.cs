using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SocialPubOnline.Models
{
    public class C_Feed
    {
        public Utilizador dados_utilizador { get; set; }
        public List<Grupo> lista_grupos { get; set; }
        public List<C_Publicacao> lista_publicacoes { get; set; }        

        public C_Feed()
        {
            lista_grupos = new List<Grupo>();
            lista_publicacoes = new List<C_Publicacao>();            
        }
    }
}