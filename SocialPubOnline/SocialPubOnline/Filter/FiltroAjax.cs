using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SocialPubOnline.Filter
{
    public class FiltroAjax : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {

            //Só executa os pedidos se os mesmos forem feitos com Ajax, caso contrario retorna null.

            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {

                base.OnActionExecuting(filterContext);
            }
            else
            {
                filterContext.Result = null;

            }
        }
    }
}