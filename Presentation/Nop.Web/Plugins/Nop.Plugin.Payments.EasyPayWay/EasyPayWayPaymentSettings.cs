using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.EasyPayWay
{
    public class EasyPayWayPaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }
        public string TestStoreId { get; set; }
        public string LiveStoreId { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        public decimal AdditionalFee { get; set; }
        /// <summary>
        /// Enable if a customer should be redirected to the order details page
        /// when he clicks "cancel transaction" link on EasyPayWay site
        /// WITHOUT completing a payment
        /// </summary>
        public bool ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage { get; set; }
    }
}
