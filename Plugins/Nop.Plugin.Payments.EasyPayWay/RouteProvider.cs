using Nop.Web.Framework.Mvc.Routes;
using System.Web.Routing;
using System.Web.Mvc;

namespace Nop.Plugin.Payments.EasyPayWay
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //Success and failure URL
            routes.MapRoute("Plugin.Payments.EasyPayWay.PaymentResult",
                 "Plugins/PaymentEasyPayWay/PaymentResult",
                 new { controller = "PaymentEasyPayWay", action = "PaymentResult" },
                 new[] { "Nop.Plugin.Payments.EasyPayWay.Controllers" }
            );
            //cancel URL
            routes.MapRoute("Plugin.Payments.EasyPayWay.CancelOrder",
                 "Plugins/PaymentEasyPayWay/CancelOrder",
                 new { controller = "PaymentEasyPayWay", action = "CancelOrder" },
                 new[] { "Nop.Plugin.Payments.EasyPayWay.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
