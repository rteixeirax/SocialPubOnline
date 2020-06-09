using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SocialPubOnline.Models
{
    public class C_Perfil
    {
        public Utilizador DadosUtilizador { get; set; }
        public List<C_Publicacao> lista_publicacoes { get; set; }
        public List<Utilizador> lista_amigos { get; set; }

        public C_Perfil()
        {
            lista_publicacoes = new List<C_Publicacao>();
            lista_amigos = new List<Utilizador>();
        }
    }
}