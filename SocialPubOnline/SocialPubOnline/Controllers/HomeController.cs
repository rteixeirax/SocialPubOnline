using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SocialPubOnline.Models;
using System.IO;
using System.Drawing;
using System.Globalization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Net.Mail;
using System.Web.Hosting;
using System.Text;

namespace SocialPubOnline.Controllers
{
    public class HomeController : Controller
    {
        DataClassesDataContext db = new DataClassesDataContext();

        public ActionResult Index()
        {
            return RedirectToAction("Login");
        }

        public ActionResult Registo()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Registo(FormCollection dados)
        {
            if(dados["password"] != dados["RepeatPassword"])
            {
                ViewBag.resgistoMessage = "*As passwords não são iguais";
                return View();
            }

            if (db.Utilizadors.Where(x => x.email == dados["email"]).Count() == 1)
            {
                ViewBag.resgistoMessage = "*email já registado";
                return View();
            }
           
            Utilizador novo = new Utilizador();
            novo.nome = dados["nome"];

            if (dados["genero"] == "Masculino")
                novo.genero = 'M';
            else if (dados["genero"] == "Feminino")
                novo.genero = 'F';

            novo.data_nasc = DateTime.Parse(dados["datanascimento"]);
            novo.endereco_pais = dados["endereco_pais"];
            novo.endereco_localidade = dados["endereco_localidade"];
            novo.contacto = dados["contacto"];
            novo.email = dados["email"];
            novo.palavra_pass = dados["password"];
            novo.foto = "/Content/UserFotos/Default.jpg";              
            db.Utilizadors.InsertOnSubmit(novo);
            db.SubmitChanges();         
               
            EnviarEmailDeConfirmacao(novo.codigo_utilizador);

            TempData["MensagemSucesso"] = "E-mail enviado. Verifique a sua conta de e-mail.";

            return RedirectToAction("Login");
          
        }   

        public ActionResult ConfirmarRegisto(int id)
        {
            if(db.Utilizadors.Where(x=> x.codigo_utilizador == id).Count() == 1)
            {
                Utilizador user = db.Utilizadors.Single(x => x.codigo_utilizador == id);
                user.estado = 1; // Coloca o o utilizador ativo.
                db.SubmitChanges();

                //Depois da conta ativada é criado o grupo de AMIGOS do utilizador.
                Grupo grupo = new Grupo();
                grupo.nome = "Amigos";
                grupo.codigo_utilizador = user.codigo_utilizador;
                grupo.data_criacao = DateTime.Now;
                grupo.imagem = "/Content/GrupoImagens/Default.png";
                db.Grupos.InsertOnSubmit(grupo);
                db.SubmitChanges();

                Pertencer pertence = new Pertencer();
                pertence.codigo_utilizador = user.codigo_utilizador;
                pertence.codigo_grupo = grupo.codigo_grupo;
                db.Pertencers.InsertOnSubmit(pertence);
                db.SubmitChanges();

                TempData["MensagemSucesso"] = "Conta ativada!";
                return RedirectToAction("Login");
            }

            return null;
        }

        public ActionResult RecuperarPassword()
        {
            int id_sessao = Verficar_login();

            if (id_sessao > 0)  //Se o utilizador já estiver logado e tentar aceder à pagina Recuperar Password atraves do Url é reencaminhado para o Feed
                return RedirectToAction("Feed");

            return View();
        }

        [HttpPost]
        public ActionResult RecuperarPassword(FormCollection dados)
        {
            if (db.Utilizadors.Where(x => x.email == dados["Email"]).Count() == 1)
            {
                EmailRecuperarPassword(db.Utilizadors.Single(x => x.email == dados["Email"]).codigo_utilizador);
                return RedirectToAction("Login");
            }
            else
                ViewBag.erro = "*Não existe nenhum utilizador registado na SOCIAL PUB Online com o e-mail que inseriu.";


            return View();
        }

      
        public ActionResult ConfirmarPassword(int id)
        {
            if (db.Utilizadors.Where(x => x.codigo_utilizador == id).Count() == 1)
            {
                return View(db.Utilizadors.Single(x => x.codigo_utilizador == id));
            }
                       
            return RedirectToAction("Login");
        }
        [HttpPost]
        public ActionResult ConfirmarPassword(FormCollection dados, int id)
        {
            if (dados["NewPassword"] != dados["RepeatPassword"])
            {
                ViewBag.erro = "*As passwords não são iguais";
                return View(db.Utilizadors.Single(x => x.codigo_utilizador == id));
            }

            if (db.Utilizadors.Where(x => x.codigo_utilizador == id).Count() == 1)
            {
                Utilizador user = db.Utilizadors.Single(x => x.codigo_utilizador == id);
                user.palavra_pass = dados["NewPassword"];
                db.SubmitChanges();
                TempData["MensagemSucesso"] = "Password alterada!";
                return RedirectToAction("Login");
            }

            return null;
        }


        public ActionResult Login()
        {
            int id_sessao = Verficar_login(); 
           
            if (id_sessao > 0)  //Se o utilizador já estiver logado e tentar aceder à pagina login atraves do Url é reencaminhado para o Feed
                return RedirectToAction("Feed");

            C_Feed dadosFeedPublico = CarregarPublicacoesPublicas();
            return View(dadosFeedPublico.lista_publicacoes);
        } 

        [HttpPost]
        public ActionResult Login(FormCollection dados)
        {
            if (db.Utilizadors.Where(x => x.email == dados["email"]).Count() == 1)
            {
                if (db.Utilizadors.Single(x => x.email == dados["email"]).palavra_pass == dados["password"])
                {
                    int ID_utilizador = db.Utilizadors.Single(x => x.email == dados["email"]).codigo_utilizador;

                    if(db.Utilizadors.Single(x=> x.codigo_utilizador == ID_utilizador).estado == 2)
                    {
                        //Caso o utlizador tenha suspendido a conta, ao voltar a fazer login a conta é restaurada.
                        Utilizador user = db.Utilizadors.Single(x => x.codigo_utilizador == ID_utilizador);
                        user.estado = 1;
                        ModificarEstadoGrupos(ID_utilizador, "Ativar");
                        db.SubmitChanges();
                        TempData["MensagemRestauro"] = "Bem vindo(a) de volta! A sua conta foi completamente restaurada.";
                    }

                    //Cria um cookie de sessao para o utilizador em questão
                    Session.Add("ID", ID_utilizador);
                    Session.Timeout = 86400; // Dura 1 dia.
                    return RedirectToAction("Feed");
                }
                else
                    ViewBag.loginMessage = "*Password errada";
            }
            else
                ViewBag.loginMessage = "*Não existe utilizador com este email";

            return View();
        }

        public ActionResult Logout()
        {
            Session.Abandon();
            return RedirectToAction("Login");
        }


        public ActionResult Feed()
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0) //Se o utilizador não estiver logado e tentar aceder à pagina Feed atraves do Url é reencaminhado para o Login
                return RedirectToAction("Login");
                        
            // Carrega os dados para apresentar no Feed e se seguida apresenta a view.
            C_Feed dadosFeed = CarregarDadosFeed(id_sessao);

            //Cria uma viewBag para no htlm criar um dropdownList com as privacidades. Não passa a privacidade "Grupo", pois pertence apenas aos grupos.
            ViewBag.Privacidade = new SelectList(db.Privacidades.Where(x=> x.descricao != "Grupo").OrderBy(x => x.codigo_privacidade), "codigo_privacidade", "descricao");

            return View(dadosFeed);            
        }


        public ActionResult DownloadAnexo(int id)
        {
            //int id_sessao = Verficar_login();

            //if (id_sessao < 0 )
            //    return RedirectToAction("Login");

            //Não verifica se está com o login efetuado para permitir os Guest fazer download dos anexos das publicacões publicas.

            string path = Server.MapPath(db.Arquivos.Single(x => x.codigo_arquivo == id).caminho);
            string formato = db.Arquivos.Single(x => x.codigo_arquivo == id).formato_arquivo;
            string nomeFile = db.Arquivos.Single(x => x.codigo_arquivo == id).nome;

            if (System.IO.File.Exists(path))
                return File(path, MimeMapping.GetMimeMapping(nomeFile), nomeFile); // MimeMaping, para apresentar o tipo de arquivo na janela download.

            return null;
        }

        [HttpPost]
        public ActionResult AdicionarPublicacao(FormCollection dados, int id)
        {           
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            //Dá o caminho da pagina de onde foi feito o pedido.
            var origemDoPedido = Request.UrlReferrer.AbsolutePath;
            string[] pagina = origemDoPedido.Split('/');            
 
            if (pagina[2] == "Grupo")
            {
                Publicacao pubGrupo = new Publicacao();
                pubGrupo.codigo_utilizador = (int)Session["id"];
                pubGrupo.conteudo = dados["conteudo_publicacao"];
                pubGrupo.data_public = DateTime.Now;
                pubGrupo.codigo_privacidade = db.Privacidades.Single(x => x.descricao == "Grupo").codigo_privacidade; // A privacidade das publicações de grupo são sempre "Grupo", ou seja visiveis apenas para membros.
                db.Publicacaos.InsertOnSubmit(pubGrupo);
                db.SubmitChanges();

                //Para efeturar upload de ficheiros grandes alterei propriedades na pagina "Web.config"
                if (Request.Files.Count == 1)  //Se existir ficheiro anexado é efetuado a inserção do mesmo na BD
                {
                    var file = Request.Files[0]; //obtem o primeiro ficheiro que foi submetido
                    var fileExtension = Path.GetExtension(file.FileName).ToLower(); //Obtem a extensao do ficheiro

                    if (file != null && file.ContentLength > 0)
                    {
                        file.SaveAs(Path.Combine(Server.MapPath("/Content/Anexos"), pubGrupo.codigo_publicacao.ToString() + fileExtension)); //Guarda ficheiro na pasta "Anexos"

                        Arquivo novoArquivo = new Arquivo();
                        novoArquivo.nome = file.FileName;
                        novoArquivo.caminho = "/Content/Anexos/" + pubGrupo.codigo_publicacao.ToString() + fileExtension;
                        novoArquivo.formato_arquivo = fileExtension;
                        db.Arquivos.InsertOnSubmit(novoArquivo);
                        db.SubmitChanges();

                        Anexar novoAnexar = new Anexar();
                        novoAnexar.codigo_arquivo = novoArquivo.codigo_arquivo;
                        novoAnexar.codigo_publicacao = pubGrupo.codigo_publicacao;
                        db.Anexars.InsertOnSubmit(novoAnexar);
                        db.SubmitChanges();
                    }
                }

                Conter novo = new Conter();
                novo.codigo_grupo = id;
                novo.codigo_publicacao = pubGrupo.codigo_publicacao;
                novo.data_public = pubGrupo.data_public;
                db.Conters.InsertOnSubmit(novo);
                db.SubmitChanges();                
                return RedirectToAction("Grupo", new { id = id });
            }

            Publicacao pub = new Publicacao();
            pub.codigo_utilizador = id;
            pub.conteudo = dados["conteudo_publicacao"];
            pub.data_public = DateTime.Now;
            pub.codigo_privacidade = Convert.ToInt16(dados["codigo_privacidade"]);
            db.Publicacaos.InsertOnSubmit(pub);
            db.SubmitChanges();

            //Para efeturar upload de ficheiros grandes alterei propriedades na pagina "Web.config"
            if (Request.Files.Count == 1) //Se existir ficheiro anexado é efetuado a inserção do mesmo na BD
            {
                var file = Request.Files[0]; //obtem o primeiro ficheiro que foi submetido
                var fileExtension = Path.GetExtension(file.FileName).ToLower(); //Obtem a extensao do ficheiro

                if (file != null && file.ContentLength > 0)
                {
                    file.SaveAs(Path.Combine(Server.MapPath("/Content/Anexos"), pub.codigo_publicacao.ToString() + fileExtension)); //Guarda ficheiro na pasta "Anexos"

                    Arquivo novoArquivo = new Arquivo();
                    novoArquivo.nome = file.FileName;
                    novoArquivo.caminho = "/Content/Anexos/" + pub.codigo_publicacao.ToString() + fileExtension;
                    novoArquivo.formato_arquivo = fileExtension;
                    db.Arquivos.InsertOnSubmit(novoArquivo);
                    db.SubmitChanges();

                    Anexar novoAnexar = new Anexar();
                    novoAnexar.codigo_arquivo = novoArquivo.codigo_arquivo;
                    novoAnexar.codigo_publicacao = pub.codigo_publicacao;
                    db.Anexars.InsertOnSubmit(novoAnexar);
                    db.SubmitChanges();
                }
            }

            if (pub.codigo_privacidade == db.Privacidades.Single(x => x.descricao == "Amigos").codigo_privacidade) // Caso a privacidade seja "Amigos" adicionamos à tabela conter.
            {
                Conter novo = new Conter();
                novo.codigo_grupo = db.Grupos.Single(x => x.codigo_utilizador == id && x.nome == "Amigos").codigo_grupo;
                novo.codigo_publicacao = pub.codigo_publicacao;
                novo.data_public = pub.data_public;

                db.Conters.InsertOnSubmit(novo);
                db.SubmitChanges();
            }

            if (pagina[2] == "Feed")
                return RedirectToAction("Feed", new { id = id });

            if (pagina[2] == "Perfil")               
                return RedirectToAction("Perfil", new { id = id }); 

            return null;
        }        

        [HttpPost]
        [Filter.FiltroAjax]
        public PartialViewResult InserirComentarios(FormCollection dados, int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            Comentar novoComentario = new Comentar();

            novoComentario.conteudo = dados["comentario"];
            novoComentario.codigo_publicacao = int.Parse(dados["ID_publicacao"]);
            novoComentario.codigo_utilizador = id;
            novoComentario.data_comentario = DateTime.Now;
            db.Comentars.InsertOnSubmit(novoComentario);
            db.SubmitChanges();

            //Dá o caminho da pagina de onde foi feito o pedido.
            var origemDoPedido = Request.UrlReferrer.AbsolutePath;
            string[] pagina = origemDoPedido.Split('/');

            if (pagina[2] == "Grupo")
            {
                C_Grupo novosDadosGrupo = new C_Grupo();
                novosDadosGrupo = CarregarDadosGrupo(int.Parse(pagina[3]));
                ViewBag.adminGrupo = db.Grupos.Single(x => x.codigo_grupo == int.Parse(pagina[3])).codigo_utilizador; // guarda o admin do grupo para usar no html. 
                return PartialView("PartialViewPublicacoesGrupo", novosDadosGrupo.lista_publicacoes);
            }

            if (pagina[2] == "Feed")
            {
                C_Feed NovosDadosFeed = new C_Feed();
                NovosDadosFeed = CarregarDadosFeed(id);
                return PartialView("PartialViewPublicacoes", NovosDadosFeed.lista_publicacoes);
            }

            if (pagina[2] == "Perfil")
            {
                C_Perfil NovosDadosPerfil = new C_Perfil();
                NovosDadosPerfil = CarregarDadosPerfil(id, "Utilizador");
                return PartialView("PartialViewPublicacoes", NovosDadosPerfil.lista_publicacoes);
            }         
      
            return null;
        }

        public ActionResult Perfil(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.
          
            if (id_sessao < 0)
                return RedirectToAction("PerfilGuest", new { id = id });

            C_Perfil dadosPerfil = new C_Perfil();
              
            if (id != (int)Session["id"]) // Verifica se o utilizador que está a consultar o perfil é o próprio ou outro.
            {        
                // Verifica se o utilizador que está a consultar o perfil faz parte do grupo "Amigos".
                if (db.Pertencers.Where(x => x.codigo_grupo == db.Grupos.Single(y => y.codigo_utilizador == id && y.nome == "Amigos").codigo_grupo && x.codigo_utilizador == (int)Session["id"]).Count() == 1)
                {
                    ViewBag.verPerfil = "Utilizador Amigo"; // Caso seja amigo, preenche a viewbag.perfil com a informação "Utilizador Amigo".
                    dadosPerfil = CarregarDadosPerfil(id, "Utilizador Amigo"); // Carrega os dados para apresentar no Feed e se seguida apresenta a view.
                    return View(dadosPerfil);
                }
                else
                {
                    ViewBag.verPerfil = "Utilizador Registado";   // Caso nao seja amigo, preenche a viewbag.perfil com a informação "Utilizador Registado".
                    dadosPerfil = CarregarDadosPerfil(id, "Utilizador Registado"); // Carrega os dados para apresentar no Feed e se seguida apresenta a view.
                    return View(dadosPerfil);
                }
            }               
                
            dadosPerfil = CarregarDadosPerfil(id,"Utilizador"); // Carrega os dados para apresentar no Perfil e se seguida apresenta a view.
            //Cria uma viewBag para no htlm criar um dropdownList com as privacidades. Não passa a privacidade "Grupo", pois pertence apenas aos grupos.
            ViewBag.Privacidade = new SelectList(db.Privacidades.Where(x => x.descricao != "Grupo").OrderBy(x => x.codigo_privacidade), "codigo_privacidade", "descricao");
            return View(dadosPerfil);          
        }

        public ActionResult PerfilGuest(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao > 0) //Se o utilizador já estiver logado e tentar aceder à Perfil Guest atraves do Url é reencaminhado para o Feed
                return RedirectToAction("Feed", new { id = id_sessao });

            C_Perfil dadosPerfilGuest = new C_Perfil();
            dadosPerfilGuest = CarregarPerfilGuest(id); // Carrega os dados para apresentar no perfil    

            return View(dadosPerfilGuest);
        }

        public ActionResult EditarPerfil(int id)
        {
            int id_sesssao = Verficar_login();

            if(id_sesssao < 0)
                return RedirectToAction("Login");
            if(id_sesssao != id)
                return RedirectToAction("Login");

            return View(db.Utilizadors.Single(x => x.codigo_utilizador == id));          
        }

        [HttpPost]
        public ActionResult EditarPerfil(FormCollection dados, int id)
        {
            Utilizador user = db.Utilizadors.Single(x => x.codigo_utilizador == id);

            if (String.IsNullOrEmpty(dados["nome"]))
                ModelState.AddModelError("nome", "*Campo obrigatório");

            if (String.IsNullOrEmpty(dados["email"]))
                ModelState.AddModelError("email", "*Campo obrigatório");

            if (String.IsNullOrEmpty(dados["genero"]))
                ModelState.AddModelError("genero", "*Campo obrigatório");

            if (String.IsNullOrEmpty(dados["datanascimento"]))
                ModelState.AddModelError("datanascimento", "*Campo obrigatório");

            if (String.IsNullOrEmpty(dados["endereco_pais"]))
                ModelState.AddModelError("endereco_pais", "*Campo obrigatório");

            if (String.IsNullOrEmpty(dados["endereco_localidade"]))
                ModelState.AddModelError("endereco_localidade", "*Campo obrigatório");

            // Caso o utilizador mude o email, antes de guardar as alterações verifica se já existe utilizadores registados com esse email,
            // caso haja, então aparece msg de erro, caso contrario, guarda as alterações efetuadas.
            if (dados["email"] != user.email)
            {
                if (db.Utilizadors.Where(x => x.email == dados["email"]).Count() == 1)
                    ModelState.AddModelError(dados["email"], "*Email já registado");
            }

            if (ModelState.IsValid)
            {
                user.nome = dados["nome"];

                if (dados["genero"] == "Masculino")
                    user.genero = 'M';
                else if (dados["genero"] == "Feminino")
                    user.genero = 'F';               

                user.data_nasc = DateTime.Parse(dados["datanascimento"]);
                user.endereco_pais = dados["endereco_pais"];
                user.endereco_localidade = dados["endereco_localidade"];
                user.contacto = dados["contacto"];
                user.email = dados["email"];
                db.SubmitChanges();                

                return RedirectToAction("Perfil", new { id = user.codigo_utilizador });
            }

            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.
            return View(db.Utilizadors.Single(x => x.codigo_utilizador == id));
        }

        public ActionResult AlterarFoto(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0)
                return RedirectToAction("Login");
            if(id_sessao != id)
                return RedirectToAction("Login");

            ViewBag.IDutilizador = id;
            ViewBag.fotoUtilizador = db.Utilizadors.Single(x => x.codigo_utilizador == id).foto;
            return View();  
        }

        [HttpPost]
        public ActionResult AlterarFoto(FormCollection dados, int id)
        {      
            if (Request.Files.Count == 1)
            {
                var file = Request.Files[0]; //obtem o primeiro ficheiro que foi submetido
                var fileExtension = Path.GetExtension(file.FileName).ToLower(); //Obtem a extensao do ficheiro

                if (file != null && file.ContentLength > 0 && (fileExtension == ".png" || fileExtension == ".jpg" || fileExtension == ".gif"))
                {
                    Bitmap bmp = new Bitmap(file.InputStream);

                    // Verifica o tamanho
                    if (bmp.Width >= 130 && bmp.Height >= 130 && bmp.Width <= 200 && bmp.Height <= 200)
                    {
                        
                       string fotoAtual = db.Utilizadors.Single(x => x.codigo_utilizador == id).foto;

                        if(fotoAtual != null)
                        {
                            string caminho = Server.MapPath(fotoAtual);

                            //Apagar qualquer foto antiga do mesmo user!
                            if (System.IO.File.Exists(caminho))
                                System.IO.File.Delete(caminho);
                        }

                        //Guarda a foto na pasta "USerFotos"
                        file.SaveAs(Path.Combine(Server.MapPath("/Content/UserFotos"), id.ToString() + fileExtension));

                        //Atualiza o path da foto do utilizador na BD.
                        Utilizador user = db.Utilizadors.Single(x => x.codigo_utilizador == id);
                        user.foto = "/Content/UserFotos/" + id.ToString() + fileExtension;
                        db.SubmitChanges();

                        return RedirectToAction("Perfil", new { id = id });
                    }
                    else
                    {
                      ViewBag.erro = "*Tamanho inválido!" + bmp.Width.ToString() + "*" + bmp.Height.ToString() + "!";
                    }
                }
                else
                    ViewBag.erro = "*Formato inválido. Apenas PNG, JPG e GIF são aceites.";
            }
            else
                ViewBag.erro = "*Não existe ficheiro submetido!";

            ViewBag.IDutilizador = id;
            ViewBag.fotoUtilizador = db.Utilizadors.Single(x => x.codigo_utilizador == id).foto;
            return View();
        }

        public ActionResult AlterarPassword(int id)
        {
           int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0 )
                return RedirectToAction("Login");
            if (id_sessao != id)
                return RedirectToAction("Login");

            return View(db.Utilizadors.Single(x=> x.codigo_utilizador == id));
        }
        [HttpPost]
        public ActionResult AlterarPassword(FormCollection dados, int id)
        {
            Utilizador user = db.Utilizadors.Single(x => x.codigo_utilizador == id);

            if (user.palavra_pass != dados["Password"])
            {
                ViewBag.erro = "*Password Atual errada";
                int id_sesssao = Verficar_login(); // Para manter os dados na "barra de navegação"
                return View(user);
            }

            if (dados["NewPassword"] != dados["RepeatPassword"])
            {
                ViewBag.erro = "*As passwords não são iguais";
                int id_sesssao = Verficar_login(); // Para manter os dados na "barra de navegação"
                return View(user);
            }

            user.palavra_pass = dados["NewPassword"];
            db.SubmitChanges();

            return RedirectToAction("Perfil", new { id = user.codigo_utilizador });
        }

        public ActionResult SuspenderConta(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0)
                return RedirectToAction("Login");
            if (id_sessao != id)
                return RedirectToAction("Login");
          
            Utilizador user = db.Utilizadors.Single(x => x.codigo_utilizador == id);
            user.estado = 2; //Estado 2, Utilizador Inativo
            ModificarEstadoGrupos(id, "Suspender");           
            db.SubmitChanges();

            TempData["MensagemSucesso"] = "Conta suspensa!";
            return RedirectToAction("Login");
        }
      

        public ActionResult CriarGrupo()
        {
            int id_sesssao = Verficar_login();

            if (id_sesssao < 0)
                return RedirectToAction("Login");

            return View();         
        }

        [HttpPost]
        public ActionResult CriarGrupo(FormCollection dados)
        {           
            if(dados["NomeGrupo"] == "Amigos")
            {
                ViewBag.erro = "*Não é possivel criar grupo com Nome 'Amigos'. Nome reservado.";
                return View();
            }

            Grupo novo = new Grupo();
            novo.data_criacao = DateTime.Now;
            novo.codigo_utilizador = (int)Session["id"];
            novo.nome = dados["NomeGrupo"];
            novo.descricao = dados["DescricaoGrupo"];
            novo.imagem = "/Content/GrupoFotos/Default.png";
            db.Grupos.InsertOnSubmit(novo);
            db.SubmitChanges();

            Pertencer novopertence = new Pertencer();
            novopertence.codigo_grupo = novo.codigo_grupo;
            novopertence.codigo_utilizador = (int)Session["id"];
            db.Pertencers.InsertOnSubmit(novopertence);
            db.SubmitChanges();

            return RedirectToAction("Grupo", new { id = novo.codigo_grupo });
        }

        public ActionResult Grupo(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0 )
             return RedirectToAction("Login");
            if(VerificarSePertenceAOGrupo(id_sessao, id) == false)
                return RedirectToAction("Login");
            if(db.Grupos.Single(x=> x.codigo_grupo == id).nome == "Amigos")
                return RedirectToAction("Login");

            ViewBag.adminGrupo = db.Grupos.Single(x => x.codigo_grupo == id).codigo_utilizador; // guarda o admin do grupo para usar no html.                
            C_Grupo dadosGrupo = CarregarDadosGrupo(id);
            return View(dadosGrupo);
        }

        public ActionResult GrupoAmigos(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0) 
                return RedirectToAction("Login");
            if(ViewBag.GrupoAmigos != id)// verifica se o utilizador que está a aceder ao grupo é o proprietario do grupo.
                return RedirectToAction("Login");

            C_GrupoAmigos dadosGrupo = CarregarDadosGrupoAmigos(id_sessao, id);
            return View(dadosGrupo);
        }

        public ActionResult AlterarFotoGrupo(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0)
                return RedirectToAction("Login");
            if (db.Grupos.Single(x=> x.codigo_grupo == id).codigo_utilizador != id_sessao) // Se não é o admin nao pode aceder
                return RedirectToAction("Login");

            ViewBag.IDgrupo = id;
            ViewBag.fotoGrupo = db.Grupos.Single(x => x.codigo_grupo == id).imagem;
            return View();
        }

        [HttpPost]
        public ActionResult AlterarFotoGrupo(FormCollection dados, int id)
        {
            if (Request.Files.Count == 1)
            {
                var file = Request.Files[0]; //obtem o primeiro ficheiro que foi submetido
                var fileExtension = Path.GetExtension(file.FileName).ToLower(); //Obtem a extensao do ficheiro

                if (file != null && file.ContentLength > 0 && (fileExtension == ".png" || fileExtension == ".jpg" || fileExtension == ".gif"))
                {
                    Bitmap bmp = new Bitmap(file.InputStream);

                    // Verifica o tamanho
                    if (bmp.Width >= 130 && bmp.Height >= 130 && bmp.Width <= 200 && bmp.Height <= 200)
                    {

                        string fotoAtual = db.Utilizadors.Single(x => x.codigo_utilizador == id).foto;

                        if (fotoAtual != null)
                        {
                            string caminho = Server.MapPath(fotoAtual);

                            //Apagar qualquer foto antiga do mesmo grupo!
                            if (System.IO.File.Exists(caminho))
                                System.IO.File.Delete(caminho);
                        }

                        //Guarda a foto na pasta "USerFotos"
                        file.SaveAs(Path.Combine(Server.MapPath("/Content/GrupoFotos"), id.ToString() + fileExtension));

                        //Atualiza o path da foto do Grupo na BD.
                        Grupo grupo = db.Grupos.Single(x => x.codigo_grupo == id);
                        grupo.imagem = "/Content/GrupoFotos/" + id.ToString() + fileExtension;
                        db.SubmitChanges();

                        return RedirectToAction("Grupo", new { id = id });
                    }
                    else
                    {
                        ViewBag.erro = "*Tamanho inválido!" + bmp.Width.ToString() + "*" + bmp.Height.ToString() + "!";
                    }
                }
                else
                    ViewBag.erro = "*Formato inválido. Apenas PNG, JPG e GIF são aceites.";
            }
            else
                ViewBag.erro = "*Não existe ficheiro submetido!";

            ViewBag.IDgrupo = id;
            ViewBag.fotoGrupo = db.Grupos.Single(x => x.codigo_grupo == id).imagem;
            return View();
        }

        public ActionResult EditarGrupo(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0 )
                return RedirectToAction("Login");
            if (db.Grupos.Single(x => x.codigo_grupo == id).codigo_utilizador != id_sessao) // Se não é o admin nao pode aceder
                return RedirectToAction("Login");

            return View(db.Grupos.Single(x => x.codigo_grupo == id));
        }
        [HttpPost]
        public ActionResult EditarGrupo(FormCollection dados, int id)
        {
            if (String.IsNullOrEmpty(dados["nome"]))
                ModelState.AddModelError("nome", "*Campo obrigatório");

            if (ModelState.IsValid)
            {
                Grupo grupo = db.Grupos.Single(x => x.codigo_grupo == id);
                grupo.nome = dados["nome"];
                grupo.descricao = dados["descricao"];
                db.SubmitChanges();

                return RedirectToAction("Grupo", new { id = id });
            }

            return View(db.Grupos.Single(x => x.codigo_grupo == id));
        }

        public ActionResult EliminarGrupo(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0 )
                return RedirectToAction("Login");
            if(db.Grupos.Single(x => x.codigo_grupo == id).codigo_utilizador != id_sessao) // Se não é o admin nao pode aceder
                return RedirectToAction("Login");

            //Caso seja mesmo para eliminar o grupo da BD...........................................................................................

            //foreach (var pub in db.Conters.Where(x => x.codigo_grupo == id))
            //{
            //    foreach (var comentario in db.Comentars.Where(x=> x.codigo_publicacao == pub.codigo_publicacao))
            //    {
            //        db.Comentars.DeleteOnSubmit(comentario); //Na tabela Comentar apago todos os comentarios que pertencam a publicacoes do grupo
            //    }
            //}           

            //foreach (var ligacao in db.Conters.Where(x=> x.codigo_grupo == id))
            //{
            //    db.Conters.DeleteOnSubmit(ligacao); // Apago as ligações na tabela Conter

            //    foreach (var publicacao in db.Publicacaos.Where(x=> x.codigo_publicacao == ligacao.codigo_publicacao))
            //    {
            //        db.Publicacaos.DeleteOnSubmit(publicacao); // Por fim apago as publicacoes do grupo
            //    }
            //}

            //foreach (var pertence in db.Pertencers.Where(x=> x.codigo_grupo == id)) 
            //{
            //    db.Pertencers.DeleteOnSubmit(pertence); // Na tabela pertencer elimino as ligações existentes com o grupo
            //}

            //db.Grupos.DeleteOnSubmit(db.Grupos.Single(x => x.codigo_grupo == id));
            //db.SubmitChanges();
            //........................................................................................................................................

            //Coloca o grupo e suas publicacoes em estado Inativo.
            Grupo grupo = db.Grupos.Single(x => x.codigo_grupo == id);
            grupo.estado = true; //Estado true, Inativo
            SuspenderPublicacoesGrupo(id);
            db.SubmitChanges();             
            return RedirectToAction("Feed");
        }       

        public ActionResult MostrarElementos(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0 ) 
                return RedirectToAction("Login");
            if (db.Grupos.Single(x => x.codigo_grupo == id).codigo_utilizador != id_sessao) // Se não é o admin nao pode aceder
                return RedirectToAction("Login");

            C_Grupo dadosGrupo = CarregarDadosGrupo(id);
            return View(dadosGrupo);
        }

        [HttpPost]
        public ActionResult RemoverElemento(FormCollection dados, int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0) 
                return RedirectToAction("Login");
            if (db.Grupos.Single(x => x.codigo_grupo == id).codigo_utilizador != id_sessao) // Se não é o admin nao pode aceder
                return RedirectToAction("Login");

            db.Pertencers.DeleteOnSubmit(db.Pertencers.Single(x => x.codigo_grupo == id && x.codigo_utilizador == int.Parse(dados["IDutilizador"])));
            db.SubmitChanges();
            return RedirectToAction("Grupo", new { id = id });
        }

        [HttpPost]
        public ActionResult ConvidarUtilizador(FormCollection dados, int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0 )
                return RedirectToAction("Login");
            if(VerificarSePertenceAOGrupo(id_sessao, id) == false) //So pode convidar quem for membro do grupo
                return RedirectToAction("Login");

            if (db.Convidars.Where(x => x.codigo_utilizador == int.Parse(dados["Utilizadores"]) && x.codigo_grupo == id && x.estado == 2).Count() == 1)
            {
                Convidar convite = db.Convidars.Single(x => x.codigo_utilizador == int.Parse(dados["Utilizadores"]) && x.codigo_grupo == id);
                convite.estado = 0;
                db.SubmitChanges();
                return RedirectToAction("Grupo", new { id = id });
            }

            if(db.Convidars.Where(x => x.codigo_utilizador == int.Parse(dados["Utilizadores"]) && x.codigo_grupo == id && x.estado == 0).Count() == 1)
            {
                TempData["Mensagem"] = "já foi enviado um convite para esse utilizador";
                return RedirectToAction("Grupo", new { id = id });
            }

            Convidar novoElemento = new Convidar();
            novoElemento.codigo_grupo = id;
            novoElemento.codigo_utilizador = int.Parse(dados["Utilizadores"]);
            novoElemento.data_convite = DateTime.Now;
            novoElemento.estado = 0; //Em análise
            db.Convidars.InsertOnSubmit(novoElemento);
            db.SubmitChanges();
            return RedirectToAction("Grupo", new { id = id });
        }      


        public ActionResult AbandonarGrupo(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if(id_sessao < 0 )
                return RedirectToAction("Login");
            if(VerificarSePertenceAOGrupo(id_sessao, id) == false)
                return RedirectToAction("Login");

            db.Pertencers.DeleteOnSubmit(db.Pertencers.Single(x => x.codigo_grupo == id && x.codigo_utilizador == id_sessao));
            db.SubmitChanges();            
            return RedirectToAction("Feed");
        }

        public ActionResult PedirAmizade(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0 )
                return RedirectToAction("Login");
            if(VerificarSePertenceAOGrupo(id_sessao, db.Grupos.Single(x => x.codigo_utilizador == id && x.nome == "Amigos").codigo_grupo) == true)
                return RedirectToAction("Login");

            if (db.Convidars.Where(x => x.codigo_utilizador == id && x.codigo_grupo == db.Grupos.Single(y => y.codigo_utilizador == id_sessao && y.nome == "Amigos").codigo_grupo && x.estado == 2).Count() == 1)
            {
                Convidar convite = db.Convidars.Single(x => x.codigo_utilizador == id && x.codigo_grupo == db.Grupos.Single(y => y.codigo_utilizador == id_sessao && y.nome == "Amigos").codigo_grupo && x.estado == 2);
                convite.estado = 0;
                db.SubmitChanges();
                return RedirectToAction("Perfil", new { id = id_sessao });
            }

            if (db.Convidars.Where(x => x.codigo_utilizador == id && x.codigo_grupo == db.Grupos.Single(y => y.codigo_utilizador == id_sessao && y.nome == "Amigos").codigo_grupo && x.estado == 0).Count() == 1)
            {
                TempData["Mensagem"] = "já foi enviado um pedido de amizade para esse utilizador";
                return RedirectToAction("Perfil", new { id = id });
            }


            Convidar pedidoAmizade = new Convidar();
            pedidoAmizade.codigo_grupo = db.Grupos.Single(x => x.codigo_utilizador == id_sessao && x.nome == "Amigos").codigo_grupo;
            pedidoAmizade.codigo_utilizador = id;
            pedidoAmizade.data_convite = DateTime.Now;
            pedidoAmizade.estado = 0; //Em análise
            db.Convidars.InsertOnSubmit(pedidoAmizade);
            db.SubmitChanges();
            return RedirectToAction("Perfil", new { id = id_sessao });
        }

        public ActionResult RemoverAmizade(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0 )
                return RedirectToAction("Login");
            if(VerificarSePertenceAOGrupo(id_sessao, db.Grupos.Single(x => x.codigo_utilizador == id && x.nome == "Amigos").codigo_grupo) == false)
                return RedirectToAction("Login");

            db.Pertencers.DeleteOnSubmit(db.Pertencers.Single(x => x.codigo_utilizador == id_sessao && x.codigo_grupo == db.Grupos.Single(y => y.codigo_utilizador == id && y.nome == "Amigos").codigo_grupo));
            db.Pertencers.DeleteOnSubmit(db.Pertencers.Single(x => x.codigo_utilizador == id && x.codigo_grupo == db.Grupos.Single(y => y.codigo_utilizador == id_sessao && y.nome == "Amigos").codigo_grupo));
            db.SubmitChanges();
            return RedirectToAction("Perfil", new { id = id_sessao });
        }

        [HttpPost]
        public ActionResult AceitarRecusarConvites(FormCollection dados)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados.

            var origemDoPedido = Request.UrlReferrer.AbsolutePath;
            string[] pagina = origemDoPedido.Split('/');

            if (dados["decisao"] == "Aceitar" && db.Grupos.Single(x=>x.codigo_grupo == int.Parse(dados["IDgrupo"])).nome == "Amigos") // Pedido amizade.
            {
                //adiciona ao grupo de amigos do utilizador que convidou
                Pertencer novo = new Pertencer();
                novo.codigo_grupo = int.Parse(dados["IDgrupo"]);
                novo.codigo_utilizador = id_sessao;
                db.Pertencers.InsertOnSubmit(novo);
                db.SubmitChanges();

                //adiciona ao grupo de amigos do utilizador que foi convidado
                Pertencer novo2 = new Pertencer();
                novo2.codigo_grupo = db.Grupos.Single(x => x.codigo_utilizador == id_sessao && x.nome == "Amigos").codigo_grupo;
                novo2.codigo_utilizador = int.Parse(dados["IDutilizador"]);
                db.Pertencers.InsertOnSubmit(novo2);
                db.SubmitChanges();

                Convidar convite = db.Convidars.Single(x => x.codigo_grupo == int.Parse(dados["IDgrupo"]) && x.codigo_utilizador == id_sessao && x.estado == 0);
                db.Convidars.DeleteOnSubmit(convite);
                db.SubmitChanges();


                if (pagina[2] == "Feed")            
                    return RedirectToAction("Feed");

                if (pagina[2] == "Perfil")
                    return RedirectToAction("Perfil", new { id = id_sessao });
            }

            if (dados["decisao"] == "Aceitar" && db.Grupos.Single(x => x.codigo_grupo == int.Parse(dados["IDgrupo"])).nome != "Amigos")
            {
                Pertencer novo = new Pertencer();
                novo.codigo_grupo = int.Parse(dados["IDgrupo"]);
                novo.codigo_utilizador = id_sessao;
                db.Pertencers.InsertOnSubmit(novo);
                db.SubmitChanges();

                Convidar convite = db.Convidars.Single(x => x.codigo_grupo == int.Parse(dados["IDgrupo"]) && x.codigo_utilizador == id_sessao && x.estado == 0);
                db.Convidars.DeleteOnSubmit(convite);
                db.SubmitChanges();

                if (pagina[2] == "Feed")
                    return RedirectToAction("Feed");

                if (pagina[2] == "Perfil")
                    return RedirectToAction("Perfil", new { id = id_sessao });
            }

            if(dados["decisao"] == "Recusar")
            {
                Convidar convite = db.Convidars.Single(x => x.codigo_grupo == int.Parse(dados["IDgrupo"]) && x.codigo_utilizador == id_sessao && x.estado == 0);
                convite.estado = 2;
                db.SubmitChanges();

                if (pagina[2] == "Feed")
                    return RedirectToAction("Feed");

                if (pagina[2] == "Perfil")
                    return RedirectToAction("Perfil", new { id = id_sessao });
            }

            return null;
        }

        [Filter.FiltroAjax]
        public PartialViewResult ApagarPublicacao(FormCollection dados, int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            //Caso seja mesmo para eliminar a publicação da BD...........................................................................................

            //foreach (var item in db.Comentars.Where(x => x.codigo_publicacao == id)) //Apagas as ligações na tabela Comentar.
            //{
            //    db.Comentars.DeleteOnSubmit(item);
            //}

            //foreach (var item in db.Conters.Where(x=> x.codigo_publicacao == id)) //Apagas as ligações na tabela Conter.
            //{
            //    db.Conters.DeleteOnSubmit(item);
            //}

            //foreach (var item in db.Anexars.Where(x => x.codigo_publicacao == id)) //Apagas as ligações na tabela Anexar.
            //{
            //    db.Anexars.DeleteOnSubmit(item);
            //    db.Arquivos.DeleteOnSubmit(item.Arquivo);
            //}
            //--------------------------------------------------------------------------------------------------------------------------------------------

            Publicacao pub = db.Publicacaos.Single(x => x.codigo_publicacao == id);
            pub.estado = true; // Coloca a publicacao inativa
            db.Publicacaos.DeleteOnSubmit(db.Publicacaos.Single(x => x.codigo_publicacao == id)); //Por fim elimina a publicacao da tabela Publicacoes.
            db.SubmitChanges();

            //Dá o caminho da pagina de onde foi feito o pedido.
            var origemDoPedido = Request.UrlReferrer.AbsolutePath;
            string [] pagina = origemDoPedido.Split('/');

            if (pagina[2] == "Grupo")
            {
                C_Grupo novosDadosGrupo = new C_Grupo();
                novosDadosGrupo = CarregarDadosGrupo(int.Parse(pagina[3]));
                ViewBag.adminGrupo = db.Grupos.Single(x => x.codigo_grupo == int.Parse(pagina[3])).codigo_utilizador; // guarda o admin do grupo para usar no html. 
                return PartialView("PartialViewPublicacoesGrupo", novosDadosGrupo.lista_publicacoes);
            }

            if (pagina[2] == "Feed")
            {
                C_Feed dadosFeed = CarregarDadosFeed(int.Parse(dados["IDutilizador"]));
                return PartialView("PartialViewPublicacoes", dadosFeed.lista_publicacoes);
            }

            if(pagina[2] == "Perfil")
            {
                C_Perfil dadosPerfil = CarregarDadosPerfil(int.Parse(dados["IDutilizador"]),"Utilizador");
                return PartialView("PartialViewPublicacoes", dadosPerfil.lista_publicacoes);
            }            

            return null;           
        }

        [Filter.FiltroAjax]
        public PartialViewResult ApagarComentario(FormCollection dados, int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.           
            
            db.Comentars.DeleteOnSubmit(db.Comentars.Single(x => x.codigo_publicacao == id && x.data_comentario == DateTime.Parse(dados["HoraComentario"]) && x.codigo_utilizador == int.Parse(dados["IDutilizador"])));
            db.SubmitChanges();

            var origemDoPedido = Request.UrlReferrer.AbsolutePath;
            string[] pagina = origemDoPedido.Split('/');

            if (pagina[2] == "Grupo")
            {
                C_Grupo novosDadosGrupo = new C_Grupo();
                novosDadosGrupo = CarregarDadosGrupo(int.Parse(pagina[3]));
                ViewBag.adminGrupo = db.Grupos.Single(x => x.codigo_grupo == int.Parse(pagina[3])).codigo_utilizador; // guarda o admin do grupo para usar no html. 
                return PartialView("PartialViewPublicacoesGrupo", novosDadosGrupo.lista_publicacoes);
            }

            if (pagina[2] == "Feed")
            {
                C_Feed dadosFeed = CarregarDadosFeed(int.Parse(dados["IDutilizador"]));
                return PartialView("PartialViewPublicacoes", dadosFeed.lista_publicacoes);
            }

            if (pagina[2] == "Perfil")
            {
                C_Perfil dadosPerfil = CarregarDadosPerfil(int.Parse(dados["IDutilizador"]), "Utilizador");
                return PartialView("PartialViewPublicacoes", dadosPerfil.lista_publicacoes);
            }

            return null;
        }

        public ActionResult DenunciarUtilizador(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0)
                return RedirectToAction("Login");
            if(db.Utilizadors.Where(x=> x.codigo_utilizador == id).Count() == 0)
                return RedirectToAction("Login");

            return View(db.Utilizadors.Single(x=> x.codigo_utilizador == id));
        }
        [HttpPost]
        public ActionResult DenunciarUtilizador(FormCollection dados, int id)
        {
            //TODO: codigo para efetuar a denuncia de utilizador.

            string aux = dados["Denuncia"];

            return null;
        }


        public ActionResult DenunciarPublicacao(int id)
        {
            int id_sessao = Verficar_login(); // Para manter os dados no "Layout" atualizados, assim como o viewBag.login.

            if (id_sessao < 0)
                return RedirectToAction("Login");
            if (db.Publicacaos.Where(x => x.codigo_publicacao == id).Count() == 0)
                return RedirectToAction("Login");

            return View(db.Publicacaos.Single(x => x.codigo_publicacao == id));
        }
        [HttpPost]
        public ActionResult DenunciarPublicacao(FormCollection dados, int id)
        {
            //TODO: Escrever o codigo para denunciar publicacao

            string aux = dados["Denuncia"];
                        
            return null;
        }    


        // ------- FUNÇÕES DE AUXILIO ---------------------------------------------------------------------------------------------------------------------

        //Função para verificar se o utilizador se encontra com login efetuado.
        public int Verficar_login()
        {
            if( Session["id"] != null && db.Utilizadors.Single(x=> x.codigo_utilizador == (int)Session["id"]).estado == 1) //Estado 1, Utilizador Ativo
            {
                //Cria uma viewbag para fazer umas verificações na view "Layout"
                ViewBag.login = (int)Session["id"];

                //Cria uma viewbag para na view "Layout" aparecer o nome do utilizador na "barra de navegação"
                ViewBag.NomeUtilizador = db.Utilizadors.Single(x => x.codigo_utilizador == (int)Session["id"]).nome;

                //Cria uma viewbag para na view "Layout" aparecer o id do grupo de amigos na "barra de navegação"
                ViewBag.GrupoAmigos = db.Grupos.Single(x => x.codigo_utilizador == (int)Session["id"] && x.nome == "Amigos").codigo_grupo;
              
                //Cria uma viewbag para guardar o caminho da foto do utilizador.
                ViewBag.foto = db.Utilizadors.Single(x => x.codigo_utilizador == (int)Session["id"]).foto;

                //Cria uma viewBag com notificações.
                ViewBag.Notificacoes = new List<C_Notificao>();
                foreach (var item in db.Convidars.Where(x => x.codigo_utilizador == (int)Session["id"] && x.estado == 0)) //Estado 0, em análise.
                {
                    C_Notificao notificacao = new C_Notificao();                    
                    notificacao.IDgrupo = item.codigo_grupo;
                    notificacao.NomeGrupo = item.Grupo.nome;
                    notificacao.IDutilizador = db.Grupos.Single(x => x.codigo_grupo == item.codigo_grupo).codigo_utilizador;
                    notificacao.UtilizadorNome = db.Utilizadors.Single(x => x.codigo_utilizador == notificacao.IDutilizador).nome;
                    notificacao.IDutilizadorConvidado = item.Utilizador.codigo_utilizador;
                    ((List<C_Notificao>)ViewBag.Notificacoes).Add(notificacao);
                }

                return (int)Session["id"];
            }

            //Retorna -1 caso seja guest
            return -1;
        }


        //Função para apresentar os utilizadores na search bar a medida que se pesquisa pelo nome.
        public JsonResult ProcurarUtilizador(string term)
        {
            var user = db.Utilizadors.Where(x => x.nome.ToLower().Contains(term.ToLower()));

            List<JQueryAutoComplete> lista = new List<JQueryAutoComplete>();

            foreach (var item in user)
            {
                lista.Add(new JQueryAutoComplete(item.codigo_utilizador, item.nome));
            }

            return Json(lista, JsonRequestBehavior.AllowGet);
        }
        

        //Função para criar o email de ativação de conta
        public void EnviarEmailDeConfirmacao(int id)
        {
            string conteudoEmail = "Clique aqui http://localhost:6273/Home/ConfirmarRegisto/" + id.ToString() + " para ativar a sua conta.";
            string email = db.Utilizadors.Single(x => x.codigo_utilizador == id).email;

            EnviarEmail(email, "SOCIAL Pub online - Ativar conta", conteudoEmail);
        }


        //Função para criar o email de recuperação de password
        public void EmailRecuperarPassword(int id)
        {
            string conteudoEmail = "Clique aqui http://localhost:6273/Home/ConfirmarPassword/" + id.ToString() + " para recuperar password.";
            string email = db.Utilizadors.Single(x => x.codigo_utilizador == id).email;

            EnviarEmail(email, "SOCIAL Pub online - Recuperar password", conteudoEmail);
        }

        //Função para enviar o email de confirmação
        public void EnviarEmail(string destinatario, string assunto, string conteudoEmail)
        {
            SmtpClient client = new SmtpClient();
            client.Port = 587;
            client.Host = "smtp.Gmail.com";
            client.EnableSsl = true;
            client.Timeout = 10000;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            string Password = "socialpubonlinepassword";
            client.Credentials = new System.Net.NetworkCredential("SocialPubOnline@Gmail.com", Password);

            MailMessage MensagemEmail = new MailMessage("SocialPubOnline@Gmail.com", destinatario, assunto, conteudoEmail);
            MensagemEmail.BodyEncoding = UTF8Encoding.UTF8;
            MensagemEmail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

            client.Send(MensagemEmail);
        }

        //Função para carregar os dados que serão apresentados no Feed do utilizador
        public C_Feed CarregarDadosFeed(int IDutilizador)
        {
            C_Feed dadosFeed = new C_Feed();
            dadosFeed.dados_utilizador = db.Utilizadors.Single(x => x.codigo_utilizador == IDutilizador);

            // Carrega os grupos 
            foreach (var item in db.Pertencers.Where(x => x.codigo_utilizador == IDutilizador && x.Grupo.nome != "Amigos" && x.Grupo.estado == false)) //Estado false, Grupo Ativo
            {
                dadosFeed.lista_grupos.Add(item.Grupo);
            }

            //Carrega Publicações Públicas através da função "CarregarPublicacoesPublicas".
            dadosFeed.lista_publicacoes = CarregarPublicacoesPublicas().lista_publicacoes;

            //Carrega Publicações dos grupos a que pertence.
            foreach (var pertence in db.Pertencers.Where(x => x.codigo_utilizador == IDutilizador && x.Grupo.estado == false)) // Verifica na tabela Pertencer quais os grupos que o utilizador faz parte e que estejam ativos.
            {
                foreach (var contem in db.Conters.Where(x => x.codigo_grupo == pertence.codigo_grupo)) //Na tabela Conter, verifica as publicações existentes do grupo em questão.
                {
                    foreach (var pub in db.Publicacaos.Where(x => x.codigo_publicacao == contem.codigo_publicacao && x.estado == false)) // Na tabela Publicacao, Carrega todas as publicacões e seus comentários referentes ao grupo.
                    {                                                                                                                   //Estado false, Publicacao Ativa
                        C_Publicacao pubGrupos = new C_Publicacao();
                        pubGrupos.publicacao = pub;

                        if (db.Anexars.Where(x => x.codigo_publicacao == pub.codigo_publicacao).Count() == 1) //Se existir anexo, carregar o arquivo da publicação.
                            pubGrupos.arquivo = db.Arquivos.Single(x => x.codigo_arquivo == db.Anexars.Single(y => y.codigo_publicacao == pub.codigo_publicacao).codigo_arquivo);

                        if (pub.Privacidade.descricao == "Grupo")
                        {
                            pubGrupos.Egrupo = true;
                            pubGrupos.codigoGrupo = contem.codigo_grupo;
                            pubGrupos.nomeGrupo = contem.Grupo.nome;
                        }

                        foreach (var comentario in db.Comentars.Where(x => x.codigo_publicacao == pub.codigo_publicacao))
                        {
                            pubGrupos.lista_comentarios.Add(comentario);
                        }

                        dadosFeed.lista_publicacoes.Add(pubGrupos);
                    }
                }
            }

            //Ordena as publicações descendentemente por Data.           
            dadosFeed.lista_publicacoes = dadosFeed.lista_publicacoes.OrderByDescending(x => x.publicacao.data_public).ToList();

            return dadosFeed;
        }


        //Função para apresentar os dados no Perfil do utilizador. Apenas aparecem Publicações efetuadas pelo utilizador.
        public C_Perfil CarregarDadosPerfil(int IDutilizador, string tipoUtilizador)
        {
            C_Perfil dadosPerfil = new C_Perfil();
            dadosPerfil.DadosUtilizador = db.Utilizadors.Single(x => x.codigo_utilizador == IDutilizador);

            if(tipoUtilizador == "Utilizador") // Apresenta todas as publicações feitas pelo utilizador.
            {
                foreach (var amigo in db.Pertencers.Where(x=> x.codigo_grupo == db.Grupos.Single(y=> y.codigo_utilizador == IDutilizador && y.nome == "Amigos").codigo_grupo && x.codigo_utilizador != IDutilizador && x.Utilizador.estado == 1)) //Estado 1, Utilizador Ativo
                {
                    dadosPerfil.lista_amigos.Add(amigo.Utilizador);
                }
                // Ordena a lista de amigos por ordem alfabetica.
                dadosPerfil.lista_amigos = dadosPerfil.lista_amigos.OrderBy(x => x.nome).ToList();

                //Carrega Publicações Públicas através da função "CarregarPublicacoesPublicas".
                dadosPerfil.lista_publicacoes = CarregarPublicacoesPublicas().lista_publicacoes;

                //Carrega Publicações dos grupos a que pertence.
                foreach (var pertence in db.Pertencers.Where(x => x.codigo_utilizador == IDutilizador && x.Grupo.estado == false)) // Verifica na tabela Pertencer quais os grupos que o utilizador faz parte.
                {                                                                                                                 //Estado false, Grupo Ativo
                    foreach (var contem in db.Conters.Where(x => x.codigo_grupo == pertence.codigo_grupo)) //Na tabela Conter, verifica as publicações existentes do grupo em questão.
                    {
                        foreach (var pub in db.Publicacaos.Where(x => x.codigo_publicacao == contem.codigo_publicacao && x.codigo_utilizador == IDutilizador && x.estado == false)) // Na tabela Publicacao, Carrega todas as publicacões e seus comentários referentes ao grupo.
                        {                                                                                                                                                          //Estado false, Publicacao Ativa
                            C_Publicacao pubPerfil = new C_Publicacao();
                            pubPerfil.publicacao = pub;

                            if (db.Anexars.Where(x => x.codigo_publicacao == pub.codigo_publicacao).Count() == 1) //Se existir anexo, carregar o arquivo da publicação.
                                pubPerfil.arquivo = db.Arquivos.Single(x => x.codigo_arquivo == db.Anexars.Single(y => y.codigo_publicacao == pub.codigo_publicacao).codigo_arquivo);

                            if (pub.Privacidade.descricao == "Grupo")
                            {
                                pubPerfil.Egrupo = true;
                                pubPerfil.codigoGrupo = contem.codigo_grupo;
                                pubPerfil.nomeGrupo = contem.Grupo.nome;
                            }

                            foreach (var comentario in db.Comentars.Where(x => x.codigo_publicacao == pub.codigo_publicacao))
                            {
                                pubPerfil.lista_comentarios.Add(comentario);
                            }

                            dadosPerfil.lista_publicacoes.Add(pubPerfil);
                        }
                    }
                }
            }
            else if(tipoUtilizador == "Utilizador Registado") // Apresenta todas as publicações de conteúdo público feitas pelo utilizador.
            {
                foreach (var amigo in db.Pertencers.Where(x => x.codigo_grupo == db.Grupos.Single(y => y.codigo_utilizador == IDutilizador && y.nome == "Amigos").codigo_grupo && x.codigo_utilizador != IDutilizador && x.Utilizador.estado == 1)) //Estado 1, Utilizador Ativo
                {
                    dadosPerfil.lista_amigos.Add(amigo.Utilizador);
                }
                // Ordena a lista de amigos por ordem alfabetica.
                dadosPerfil.lista_amigos = dadosPerfil.lista_amigos.OrderBy(x => x.nome).ToList();                

                foreach (var pub in db.Publicacaos.Where(x => x.codigo_utilizador == IDutilizador && x.codigo_privacidade == db.Privacidades.Single(y=> y.descricao == "Público").codigo_privacidade && x.estado == false)) //Estado false, Publicacao Ativa
                {
                    C_Publicacao pubPerfil = new C_Publicacao();
                    pubPerfil.publicacao = pub;

                    if (db.Anexars.Where(x => x.codigo_publicacao == pub.codigo_publicacao).Count() == 1) //Se existir anexo, carregar o arquivo da publicação.
                        pubPerfil.arquivo = db.Arquivos.Single(x => x.codigo_arquivo == db.Anexars.Single(y => y.codigo_publicacao == pub.codigo_publicacao).codigo_arquivo);

                    if (db.Conters.Where(x => x.codigo_publicacao == pub.codigo_publicacao && x.Grupo.nome != "Amigos").Count() == 1)
                    {
                        pubPerfil.Egrupo = true;
                        pubPerfil.nomeGrupo = db.Conters.Single(x => x.codigo_publicacao == pub.codigo_publicacao).Grupo.nome;
                        pubPerfil.codigoGrupo = db.Conters.Single(x => x.codigo_publicacao == pub.codigo_publicacao).codigo_grupo;
                    }

                    foreach (var comentario in db.Comentars.Where(x => x.codigo_publicacao == pub.codigo_publicacao))
                    {
                        pubPerfil.lista_comentarios.Add(comentario);
                    }

                    dadosPerfil.lista_publicacoes.Add(pubPerfil);
                }
            }
            else if (tipoUtilizador == "Utilizador Amigo") // Apresenta todas as publicações, exepto as de conteúdo grupo, feitas pelo utilizador.
            {
                foreach (var amigo in db.Pertencers.Where(x => x.codigo_grupo == db.Grupos.Single(y => y.codigo_utilizador == IDutilizador && y.nome == "Amigos").codigo_grupo && x.codigo_utilizador != IDutilizador && x.Utilizador.estado == 1)) //Estado 1, Utilizador Ativo
                {
                    dadosPerfil.lista_amigos.Add(amigo.Utilizador);
                }
                // Ordena a lista de amigos por ordem alfabetica.
                dadosPerfil.lista_amigos = dadosPerfil.lista_amigos.OrderBy(x => x.nome).ToList();           

                foreach (var pub in db.Publicacaos.Where(x => x.codigo_utilizador == IDutilizador && x.codigo_privacidade != db.Privacidades.Single(y => y.descricao == "Grupo").codigo_privacidade && x.estado == false)) //Estado false, Publicacao Ativa
                {
                    C_Publicacao pubPerfil = new C_Publicacao();
                    pubPerfil.publicacao = pub;

                    if (db.Anexars.Where(x => x.codigo_publicacao == pub.codigo_publicacao).Count() == 1) //Se existir anexo, carregar o arquivo da publicação.
                        pubPerfil.arquivo = db.Arquivos.Single(x => x.codigo_arquivo == db.Anexars.Single(y => y.codigo_publicacao == pub.codigo_publicacao).codigo_arquivo);

                    if (db.Conters.Where(x => x.codigo_publicacao == pub.codigo_publicacao && x.Grupo.nome != "Amigos").Count() == 1)
                    {
                        pubPerfil.Egrupo = true;
                        pubPerfil.nomeGrupo = db.Conters.Single(x => x.codigo_publicacao == pub.codigo_publicacao).Grupo.nome;
                        pubPerfil.codigoGrupo = db.Conters.Single(x => x.codigo_publicacao == pub.codigo_publicacao).codigo_grupo;
                    }

                    foreach (var comentario in db.Comentars.Where(x => x.codigo_publicacao == pub.codigo_publicacao))
                    {
                        pubPerfil.lista_comentarios.Add(comentario);
                    }

                    dadosPerfil.lista_publicacoes.Add(pubPerfil);
                }
            }

            //Ordena as publicações descendentemente por Data.           
            dadosPerfil.lista_publicacoes = dadosPerfil.lista_publicacoes.OrderByDescending(x => x.publicacao.data_public).ToList();

            return dadosPerfil;
        }


        //Função para carregar os dados de perfil do utilizador para serem apresentados ao Guest
        public C_Perfil CarregarPerfilGuest(int IDutilizador)
        {
            C_Perfil dadosPerfilGuest = new C_Perfil();
            dadosPerfilGuest.DadosUtilizador = db.Utilizadors.Single(x => x.codigo_utilizador == IDutilizador && x.estado == 1); //Estado 1, Utilizador Ativo

            foreach (var pub in db.Publicacaos.Where(x => x.codigo_utilizador == IDutilizador && x.codigo_privacidade == db.Privacidades.Single(y => y.descricao == "Público").codigo_privacidade && x.estado == false)) //Estado false, Publicacao Ativa
            {
                C_Publicacao pubPerfil = new C_Publicacao();
                pubPerfil.publicacao = pub;

                if (db.Anexars.Where(x => x.codigo_publicacao == pub.codigo_publicacao).Count() == 1) //Se existir anexo, carregar o arquivo da publicação.
                    pubPerfil.arquivo = db.Arquivos.Single(x => x.codigo_arquivo == db.Anexars.Single(y => y.codigo_publicacao == pub.codigo_publicacao).codigo_arquivo);

                foreach (var comentario in db.Comentars.Where(x => x.codigo_publicacao == pub.codigo_publicacao))
                {
                    pubPerfil.lista_comentarios.Add(comentario);
                }

                dadosPerfilGuest.lista_publicacoes.Add(pubPerfil);
            }

            //Ordena as publicações descendentemente por Data.           
            dadosPerfilGuest.lista_publicacoes = dadosPerfilGuest.lista_publicacoes.OrderByDescending(x => x.publicacao.data_public).ToList();

            return dadosPerfilGuest;
        }


        //Função para apresentar as publicações publicas na pagina de login.
        public C_Feed CarregarPublicacoesPublicas()
        {
            C_Feed dadosPublicacao = new C_Feed();
           
            foreach (var item in db.Publicacaos.Where(x => x.codigo_privacidade == db.Privacidades.Single(y => y.descricao == "Público").codigo_privacidade && x.estado == false)) //Estado false, publicacao Ativa
            {
                C_Publicacao pubPublicas = new C_Publicacao();
                pubPublicas.publicacao = item;

                if (db.Anexars.Where(x => x.codigo_publicacao == item.codigo_publicacao).Count() == 1) //Se existir anexo, carregar o arquivo da publicação.
                    pubPublicas.arquivo = db.Arquivos.Single(x => x.codigo_arquivo == db.Anexars.Single(y => y.codigo_publicacao == item.codigo_publicacao).codigo_arquivo);

                foreach (var comentario in db.Comentars.Where(x => x.codigo_publicacao == item.codigo_publicacao))
                {
                    pubPublicas.lista_comentarios.Add(comentario);
                }

                dadosPublicacao.lista_publicacoes.Add(pubPublicas);
            }

            //Ordena as publicações descendentemente por Data.           
            dadosPublicacao.lista_publicacoes = dadosPublicacao.lista_publicacoes.OrderByDescending(x => x.publicacao.data_public).ToList();

            return dadosPublicacao;
        }


        //Função para carregar dados que serão apresentados na view de Grupo.
        public C_Grupo CarregarDadosGrupo(int IDgrupo)
        {
            C_Grupo dadosGrupo = new C_Grupo();

            dadosGrupo.Dados_Grupo = db.Grupos.Single(x => x.codigo_grupo == IDgrupo);

            foreach (var utilizador in db.Pertencers.Where(x=> x.codigo_grupo == IDgrupo && x.Utilizador.estado == 1))//Estado 1, Utilizador Ativo
            {
                dadosGrupo.lista_utilizadores.Add(utilizador.Utilizador);
            }
            // Ordena a lista de utilizadores por ordem alfabetica.
            dadosGrupo.lista_utilizadores = dadosGrupo.lista_utilizadores.OrderBy(x => x.nome).ToList();
            
            //Preenche a ViewBag com os utilizadores que fazem parte da rede social, mas que nao pertencam ao grupo.
            List<Utilizador> listaUtilizadoresDaRede = new List<Utilizador>();
            foreach (var utilizador in db.Utilizadors.Where(x=> x.estado == 1))//Estado 1, Utilizador Ativo
            {
                if(dadosGrupo.lista_utilizadores.Where(x=> x.codigo_utilizador == utilizador.codigo_utilizador).Count() == 0)
                    listaUtilizadoresDaRede.Add(utilizador);                
            }
            ViewBag.Utilizadores = new SelectList(listaUtilizadoresDaRede.OrderBy(x => x.nome).ToList(), "codigo_utilizador", "nome");


            foreach (var publicacao in db.Conters.Where(x=> x.codigo_grupo == IDgrupo && x.Publicacao.estado == false)) //Estado false, publicacao Ativa
            {
                C_Publicacao pub = new C_Publicacao();
                pub.Egrupo = true;
                pub.publicacao = publicacao.Publicacao;
                pub.nomeGrupo = dadosGrupo.Dados_Grupo.nome;
                pub.codigoGrupo = IDgrupo;

                if (db.Anexars.Where(x => x.codigo_publicacao == publicacao.codigo_publicacao).Count() == 1) //Se existir anexo, carregar o arquivo da publicação.
                    pub.arquivo = db.Arquivos.Single(x => x.codigo_arquivo == db.Anexars.Single(y => y.codigo_publicacao == publicacao.codigo_publicacao).codigo_arquivo);

                foreach (var comentario in db.Comentars.Where(x=> x.codigo_publicacao == pub.publicacao.codigo_publicacao))
                {
                    pub.lista_comentarios.Add(comentario);
                }

                dadosGrupo.lista_publicacoes.Add(pub);
            }

            //Ordena as publicações descendentemente por Data.           
            dadosGrupo.lista_publicacoes = dadosGrupo.lista_publicacoes.OrderByDescending(x => x.publicacao.data_public).ToList();

            return dadosGrupo;
        }


        //Função para carregar dados que serão apresentados na view de GrupoAmigos
        public C_GrupoAmigos CarregarDadosGrupoAmigos(int IDutilizador, int IDgrupo)
        {
            C_GrupoAmigos dadosGrupo = new C_GrupoAmigos();

            dadosGrupo.DadosUtilizador = db.Utilizadors.Single(x => x.codigo_utilizador == IDutilizador);

            foreach (var amigo in db.Pertencers.Where(x=> x.codigo_grupo == IDgrupo && x.codigo_utilizador != IDutilizador && x.Utilizador.estado == 1))//Estado 1, Utilizador Ativo
            {
                dadosGrupo.ListaAmigos.Add(amigo.Utilizador);
            }

            return dadosGrupo;
        }


        //Função para verificar se o utilizador pertence ao Grupo.
        public bool VerificarSePertenceAOGrupo(int IDutilizador, int IDgrupo)
        {
            if (db.Pertencers.Where(x => x.codigo_grupo == IDgrupo && x.codigo_utilizador == IDutilizador).Count() == 1)
                return true;

            return false;
        }


        //Função para Modificar o estado das publicações e dos Grupos.
        public void ModificarEstadoGrupos(int IDutilizador, string Accao)
        {
            if(Accao == "Suspender")
            {
                foreach (var grupo in db.Grupos.Where(x=> x.codigo_utilizador == IDutilizador))
                {
                    grupo.estado = true; //Estado true, Grupo Inativo
                }

            }
            else if(Accao == "Ativar")
            {
                foreach (var grupo in db.Grupos.Where(x => x.codigo_utilizador == IDutilizador))
                {
                    grupo.estado = false;
                }
            }

            db.SubmitChanges();
        }


        //Função para Suspender Publicacões do Grupo.
        public void SuspenderPublicacoesGrupo(int IDgrupo)
        {
            foreach (var item in db.Conters.Where(x=> x.codigo_grupo == IDgrupo))
            {
                item.Publicacao.estado = true; //Estado true, Publicacao Inativa
            }

            db.SubmitChanges();
        }

    }  
}