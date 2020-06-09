using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SocialPubOnline.Models
{
    public class C_Grupo
    {
        public Grupo Dados_Grupo { get; set; }
        public List<Utilizador> lista_utilizadores { get; set; }
        public List<C_Publicacao> lista_publicacoes { get; set; }

        public C_Grupo()
        {
            lista_utilizadores = new List<Utilizador>();
            lista_publicacoes = new List<C_Publicacao>();
        }
    }
}