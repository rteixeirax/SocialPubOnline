using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SocialPubOnline.Models
{
    public class C_GrupoAmigos
    {
        public Utilizador DadosUtilizador { get; set; }
        public List<Utilizador> ListaAmigos { get; set; }

        public C_GrupoAmigos()
        {
            ListaAmigos = new List<Utilizador>();
        }

    }
}