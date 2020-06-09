using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SocialPubOnline.Models
{
    public class C_Publicacao
    {
        public bool Egrupo { get; set; } // para diferenciar se a publicacção é ém um grupo
        public string nomeGrupo { get; set; } // Para guardar o nome do grupo
        public int codigoGrupo { get; set; } // Para guardar o codigo de grupo
        public Publicacao publicacao { get; set; }
        public Arquivo arquivo { get; set; } //uma publicação apenas tem um ficheiro anexado.
        public List<Comentar> lista_comentarios { get; set; }

        public C_Publicacao()
        {
            lista_comentarios = new List<Comentar>();
            Egrupo = false;
        }
    }
}