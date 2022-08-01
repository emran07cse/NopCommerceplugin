using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.EasyPayWay.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.EasyPayWay.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.EasyPayWay.Fields.TestStoreId")]
        public string TestStoreId { get; set; }
        public bool TestStoreId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.EasyPayWay.Fields.LiveStoreId")]
        public string LiveStoreId { get; set; }
        public bool LiveStoreId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.EasyPayWay.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.EasyPayWay.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.EasyPayWay.Fields.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage")]
        public bool ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage { get; set; }
        public bool ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore { get; set; }
    }
}
