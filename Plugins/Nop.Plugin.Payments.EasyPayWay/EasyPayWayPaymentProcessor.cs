using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.EasyPayWay.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Routing;
using Nop.Services.Localization;
using Nop.Core.Domain.Shipping;

namespace Nop.Plugin.Payments.EasyPayWay
{
    /// <summary>
    /// EasyPayWay payment processor
    /// </summary>
    public class EasyPayWayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly EasyPayWayPaymentSettings _easyPayWayPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IOrderService _orderService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;

        #endregion

        #region Ctor

        public EasyPayWayPaymentProcessor(EasyPayWayPaymentSettings easyPayWayPaymentSettings,
            ISettingService settingService,
            IWebHelper webHelper,
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            IOrderService orderService,
            IOrderTotalCalculationService orderTotalCalculationService)
        {
            this._easyPayWayPaymentSettings = easyPayWayPaymentSettings;
            this._settingService = settingService;
            this._webHelper = webHelper;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._orderService = orderService;
            this._orderTotalCalculationService = orderTotalCalculationService;
        }

        #endregion

        #region Utilities
        
        /// <summary>
        /// Gets EasyPayWay URL
        /// </summary>
        /// <returns></returns>
        private string GetEasyPayWayUrl()
        {
            return _easyPayWayPaymentSettings.UseSandbox ? "http://www.easypayway.com.bd/secure_pay/paynow.php" : "https://www.easypayway.com/secure_pay/paynow.php";
        }

        /// <summary>
        /// Gets EasyPayWay Store ID (Merchant ID)
        /// </summary>
        /// <returns></returns>
        private string GetStoreId()
        {
            return _easyPayWayPaymentSettings.UseSandbox ? _easyPayWayPaymentSettings.TestStoreId : _easyPayWayPaymentSettings.LiveStoreId;
        }

        private bool IsNaturalNumber(decimal amount)
        {
            decimal diff = Math.Abs(Math.Truncate(amount) - amount);
            return (diff < Convert.ToDecimal(0.0000001)) || (diff > Convert.ToDecimal(0.9999999));
        }
        
        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;            
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var post = new RemotePost();

            var order = postProcessPaymentRequest.Order;
            string transactionId = order.Id.ToString();
            string amount = IsNaturalNumber(order.OrderTotal) ? order.OrderTotal.ToString("0", CultureInfo.InvariantCulture) : order.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture);
            var storeLocation = _webHelper.GetStoreLocation(false);
            var customer = order.Customer;

            string successUrl = storeLocation + "Plugins/PaymentEasyPayWay/PaymentResult";
            string cancelUrl = storeLocation + "Plugins/PaymentEasyPayWay/CancelOrder";
            string failureUrl = storeLocation + "Plugins/PaymentEasyPayWay/PaymentResult";

            post.FormName = "EasyPayWayForm";
            post.Url = GetEasyPayWayUrl();
            post.Method = "POST";
            
            post.Add("store_id", GetStoreId());
            post.Add("tran_id", transactionId);
            post.Add("success_url", successUrl);
            post.Add("fail_url", failureUrl);
            post.Add("cancel_url", cancelUrl);
            post.Add("opt_a", "");
            post.Add("opt_b", "");
            post.Add("opt_c", "");
            post.Add("opt_d", "");
            post.Add("amount", amount);
            post.Add("payment_type", "");
            post.Add("currency", order.CustomerCurrencyCode);
            post.Add("desc", GetStoreId());
            post.Add("cus_name", string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName));
            post.Add("cus_email", customer.Email);
            post.Add("cus_add1", order.BillingAddress.Address1);
            post.Add("cus_add2", order.BillingAddress.Address2);
            post.Add("cus_city", order.BillingAddress.City);
            post.Add("cus_state", order.BillingAddress.StateProvince != null ? order.BillingAddress.StateProvince.Name : "");
            post.Add("cus_postcode", order.BillingAddress.ZipPostalCode);
            post.Add("cus_country", order.BillingAddress.Country != null ? order.BillingAddress.Country.Name : "");
            post.Add("cus_phone", order.BillingAddress.PhoneNumber);
            post.Add("cus_fax", order.BillingAddress.FaxNumber);

            if (postProcessPaymentRequest.Order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                post.Add("ship_name", string.Format("{0} {1}", order.ShippingAddress.FirstName, order.ShippingAddress.LastName));
                post.Add("ship_add1", order.ShippingAddress.Address1);
                post.Add("ship_add2", order.ShippingAddress.Address2);
                post.Add("ship_city", order.ShippingAddress.City);
                post.Add("ship_state", order.ShippingAddress.StateProvince != null ? order.ShippingAddress.StateProvince.Name : "");
                post.Add("ship_postcode", order.ShippingAddress.ZipPostalCode);
                post.Add("ship_country", order.ShippingAddress.Country != null ? order.ShippingAddress.Country.Name : "");
            }

            post.Post();
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _easyPayWayPaymentSettings.AdditionalFee, _easyPayWayPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if (order.OrderStatus == OrderStatus.Pending)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentEasyPayWay";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.EasyPayWay.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentEasyPayWay";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.EasyPayWay.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentEasyPayWayController);
        }

        public override void Install()
        {
            //settings
            var settings = new EasyPayWayPaymentSettings()
            {
                UseSandbox = true,
                TestStoreId = "test",
                LiveStoreId = "Your live store id",
                AdditionalFee = 0,                
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.RedirectionTip", "You will be redirected to EasyPayWay site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.UseSandbox", "Use Test Mode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.UseSandbox.Hint", "Mark if you want to test the gateway");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.TestStoreId", "Test Store ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.TestStoreId.Hint", "Enter Test Store ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.LiveStoreId", "Live Store ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.LiveStoreId.Hint", "Enter Live Store ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.AdditionalFee.Hint", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage", "Return to order details page");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage.Hint", "Enable if a customer should be redirected to the order details page when he clicks \"Cancel Transaction\" link on EasyPayWay site WITHOUT completing a payment");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPayWay.PaymentErrorMessage", "There was a problem with your payment. Please click details below and then try to complete the payment again.");

            base.Install();
        }

        public override void Uninstall()
        {
            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.TestStoreId");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.TestStoreId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.LiveStoreId");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.LiveStoreId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.AdditionalFeePercentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.Fields.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPayWay.PaymentErrorMessage");

            base.Uninstall();
        }
        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get
            {
                return false;
            }
        }

        #endregion
    }
}
