using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.EasyPayWay.Models;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Nop.Plugin.Payments.EasyPayWay.Controllers
{
    public class PaymentEasyPayWayController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly PaymentSettings _paymentSettings;
        private readonly EasyPayWayPaymentSettings _easyPayWayPaymentSettings;

        public PaymentEasyPayWayController(
            IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            IPaymentService paymentService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IStoreContext storeContext,
            ILogger logger,
            IWebHelper webHelper,
            PaymentSettings paymentSettings,
            EasyPayWayPaymentSettings easyPayWayPaymentSettings
            
            )
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._storeContext = storeContext;
            this._logger = logger;
            this._webHelper = webHelper;
            this._paymentSettings = paymentSettings;
            this._easyPayWayPaymentSettings = easyPayWayPaymentSettings;
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var easyPayWayPaymentSettings = _settingService.LoadSetting<EasyPayWayPaymentSettings>(storeScope);

            var model = new ConfigurationModel();

            model.UseSandbox = easyPayWayPaymentSettings.UseSandbox;
            model.TestStoreId = easyPayWayPaymentSettings.TestStoreId;
            model.LiveStoreId = easyPayWayPaymentSettings.LiveStoreId;
            model.AdditionalFee = easyPayWayPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = easyPayWayPaymentSettings.AdditionalFeePercentage;
            model.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage = easyPayWayPaymentSettings.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(easyPayWayPaymentSettings, x => x.UseSandbox, storeScope);
                model.TestStoreId_OverrideForStore = _settingService.SettingExists(easyPayWayPaymentSettings, x => x.TestStoreId, storeScope);
                model.LiveStoreId_OverrideForStore = _settingService.SettingExists(easyPayWayPaymentSettings, x => x.LiveStoreId, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(easyPayWayPaymentSettings, x => x.AdditionalFee,
                    storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(easyPayWayPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore = _settingService.SettingExists(easyPayWayPaymentSettings, x => x.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage, storeScope);
            }

            return View("~/Plugins/Payments.EasyPayWay/Views/PaymentEasyPayWay/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var easyPayWayPaymentSettings = _settingService.LoadSetting<EasyPayWayPaymentSettings>(storeScope);

            //save settings
            easyPayWayPaymentSettings.UseSandbox = model.UseSandbox;
            easyPayWayPaymentSettings.TestStoreId = model.TestStoreId;
            easyPayWayPaymentSettings.LiveStoreId = model.LiveStoreId;
            easyPayWayPaymentSettings.AdditionalFee = model.AdditionalFee;
            easyPayWayPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            easyPayWayPaymentSettings.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage = model.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.UseSandbox_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(easyPayWayPaymentSettings, x => x.UseSandbox, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(easyPayWayPaymentSettings, x => x.UseSandbox, storeScope);

            if (model.TestStoreId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(easyPayWayPaymentSettings, x => x.TestStoreId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(easyPayWayPaymentSettings, x => x.TestStoreId, storeScope);

            if (model.LiveStoreId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(easyPayWayPaymentSettings, x => x.LiveStoreId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(easyPayWayPaymentSettings, x => x.LiveStoreId, storeScope);

            if (model.AdditionalFee_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(easyPayWayPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(easyPayWayPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(easyPayWayPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(easyPayWayPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            if (model.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(easyPayWayPaymentSettings, x => x.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(easyPayWayPaymentSettings, x => x.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();
            return View("~/Plugins/Payments.EasyPayWay/Views/PaymentEasyPayWay/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        [ValidateInput(false)]
        public ActionResult PaymentResult(FormCollection form)
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.EasyPayWay") as EasyPayWayPaymentProcessor;
            if (processor == null || !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("EasyPayWay module cannot be loaded");


            var paymentStatus = CommonHelper.EnsureNotNull(form["pay_status"]);
            var transactionId = CommonHelper.EnsureNotNull(form["epw_txnid"]);
            var amount = CommonHelper.EnsureNotNull(form["amount"]);
            var merchantTranId = CommonHelper.EnsureNotNull(form["mer_txnid"]);
            var storeId = CommonHelper.EnsureNotNull(form["store_id"]);
            var currency = CommonHelper.EnsureNotNull(form["currency"]);
            var merchentCurrency = CommonHelper.EnsureNotNull(form["currency_merchant"]);
            var conversionRate = CommonHelper.EnsureNotNull(form["conversion_rate"]);
            var storeAmount = CommonHelper.EnsureNotNull(form["store_amount"]);
            var payTime = CommonHelper.EnsureNotNull(form["pay_time"]);
            var bankTranId = CommonHelper.EnsureNotNull(form["bank_txn"]);
            var cardNo = CommonHelper.EnsureNotNull(form["card_number"]);
            var cardHolder = CommonHelper.EnsureNotNull(form["card_holder"]);
            var cardType = CommonHelper.EnsureNotNull(form["card_type"]);
            var ip = CommonHelper.EnsureNotNull(form["ip_address"]);
            var reason = CommonHelper.EnsureNotNull(form["reason"]);
            var otherCurrency = CommonHelper.EnsureNotNull(form["other_currency"]);
            var successUrl = CommonHelper.EnsureNotNull(form["success_url"]);
            var failUrl = CommonHelper.EnsureNotNull(form["fail_url"]);
            var serviceChargeTk = CommonHelper.EnsureNotNull(form["epw_service_charge_bdt"]);
            var serviceChargeUsd = CommonHelper.EnsureNotNull(form["epw_service_charge_usd"]);

            var order = _orderService.GetOrderById(Convert.ToInt32(merchantTranId));
            if (order == null)
                throw new NopException(String.Format("The order ID {0} doesn't exists in the system", merchantTranId));

            var sb = new StringBuilder();
            sb.AppendLine("Returned data by EasyPayWay:");
            sb.AppendLine("Transaction status: " + paymentStatus);
            sb.AppendLine("EasyPayWay Unique Transaction ID: " + transactionId);
            sb.AppendLine("Amount in Bangladeshi Taka: " + amount);
            sb.AppendLine("Merchant Transaction ID: " + merchantTranId);
            sb.AppendLine("Merchant ID: " + storeId);
            sb.AppendLine("Currency: " + currency);
            sb.AppendLine("Merchant Currency: " + merchentCurrency);
            sb.AppendLine("Current Conversion Rate: " + conversionRate);
            sb.AppendLine("Merchant Receivable Amount in BDT after deducting Service Charge: " + storeAmount);
            sb.AppendLine("Payment Process Date and Time (YYYY-MM-DD, HH:MM:SS): " + payTime);
            sb.AppendLine("Bank Transaction ID: " + bankTranId);
            sb.AppendLine("Card number: " + cardNo);
            sb.AppendLine("Card Holder's Name: " + cardHolder);
            sb.AppendLine("Payment Process Card Type: " + cardType);
            sb.AppendLine("IP Address: " + ip);
            sb.AppendLine("Reason: " + reason);
            sb.AppendLine("Merchant Provided Currency Amount: " + otherCurrency);
            sb.AppendLine("EasyPayWay Service Charge amount in BDT: " + serviceChargeTk);
            sb.AppendLine("EasyPayWay Service Charge Amount in Merchant Currency: " + serviceChargeUsd);

            order.OrderNotes.Add(new OrderNote()
                {
                    Note = sb.ToString(),
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
            _orderService.UpdateOrder(order);

            if (paymentStatus == "success" && Convert.ToDecimal(amount) == order.OrderTotal && _orderProcessingService.CanMarkOrderAsPaid(order))
            {
                _orderProcessingService.MarkOrderAsPaid(order);
                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }
            else
            {
                return RedirectToAction("Index", "Home", new { area = "" });
                //_logger.InsertLog(Core.Domain.Logging.LogLevel.Information, "Payment Result", msg + " response: " + result);
                //var model = new PaymentResultModel();
                //model.OrderID = merchantTranId.ToString();
                //model.Message = "Payment Failed";

                //return View("~/Plugins/Payments.EasyPayWay/Views/PaymentEasyPayWay/PaymentResult.cshtml", model);
            }
        }

        public ActionResult CancelOrder(FormCollection form)
        {
            if (_easyPayWayPaymentSettings.ReturnFromEasyPayWayWithoutPaymentRedirectsToOrderDetailsPage)
            {
                var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                    customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                    .FirstOrDefault();
                if (order != null)
                {
                    return RedirectToRoute("OrderDetails", new { orderId = order.Id });
                }
            }

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
