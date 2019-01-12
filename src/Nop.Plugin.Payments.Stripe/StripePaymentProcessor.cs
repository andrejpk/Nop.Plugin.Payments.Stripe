using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Tasks;
using Nop.Core.Plugins;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Payments;
using Nop.Services.Tasks;
using Nop.Web.Framework.UI;
using Stripe;

namespace Nop.Plugin.Payments.Stripe
{
    /// <summary>
    /// Support for the Stripe payment processor.
    /// </summary>
    public class StripePaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IPaymentService _paymentService;
        private readonly IPageHeadBuilder _pageHeadBuilder;
        private readonly ISettingService _settingService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IWebHelper _webHelper;
        private readonly StripePaymentSettings _stripePaymentSettings;

        #endregion

        #region Ctor

        public StripePaymentProcessor(CurrencySettings currencySettings,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            ILogger logger,
            IPaymentService paymentService,
            IPageHeadBuilder pageHeadBuilder,
            ISettingService settingService,
            IScheduleTaskService scheduleTaskService,
            IWebHelper webHelper,
            StripePaymentSettings stripePaymentSettings)
        {
            this._currencySettings = currencySettings;
            this._currencyService = currencyService;
            this._customerService = customerService;
            this._genericAttributeService = genericAttributeService;
            this._localizationService = localizationService;
            this._logger = logger;
            this._paymentService = paymentService;
            this._pageHeadBuilder = pageHeadBuilder;
            this._settingService = settingService;
            this._scheduleTaskService = scheduleTaskService;
            this._webHelper = webHelper;
            this._stripePaymentSettings = stripePaymentSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Convert a NopCommere address to a Stripe API address
        /// </summary>
        /// <param name="nopAddress"></param>
        /// <returns></returns>
        private AddressOptions MapNopAddressToStripe(Core.Domain.Common.Address nopAddress)
        {
            return new AddressOptions
            {
                Line1 = nopAddress.Address1,
                City = nopAddress.City,
                State = nopAddress.StateProvince.Abbreviation,
                PostalCode = nopAddress.ZipPostalCode,
                Country = nopAddress.Country.ThreeLetterIsoCode
            };
        }

        /// <summary>
        /// Set up for a call to the Stripe API
        /// </summary>
        /// <returns></returns>
        private RequestOptions GetStripeApiRequestOptions()
        {
            return new RequestOptions
            {
                ApiKey = _stripePaymentSettings.SecretKey,
                IdempotencyKey = Guid.NewGuid().ToString()
            };
        }


        /// <summary>
        /// Perform a shallow validation of a stripe token
        /// </summary>
        /// <param name="stripeTokenObj"></param>
        /// <returns></returns>
        private bool IsStripeTokenID(string token)
        {
            return token.StartsWith("tok_");
        }
        
        private bool IsChargeID(string chargeID)
        {
            return chargeID.StartsWith("ch_");
        }

        #endregion

        #region Methods

        public override string GetConfigurationPageUrl() => $"{_webHelper.GetStoreLocation()}Admin/PaymentStripe/Configure";

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public bool CanRePostProcessPayment(Core.Domain.Orders.Order order)
        {
            throw new NotImplementedException();
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            throw new NotImplementedException();
        }


        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = _paymentService.CalculateAdditionalFee(cart,
               _stripePaymentSettings.AdditionalFee, _stripePaymentSettings.AdditionalFeePercentage);

            return result;
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentRequest = new ProcessPaymentRequest();

            if (form.TryGetValue("stripeToken", out StringValues stripeToken) && !StringValues.IsNullOrEmpty(stripeToken))
                paymentRequest.CustomValues.Add(_localizationService.GetResource("Plugins.Payments.Stripe.Fields.StripeToken.Key"), stripeToken.ToString());

            return paymentRequest;
        }

        public string GetPublicViewComponentName()
        {
            return StripePaymentDefaults.ViewComponentName;
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            throw new NotImplementedException();
        }


        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            //get customer
            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);
            if (customer == null)
                throw new NopException("Customer cannot be loaded");

            string tokenKey = _localizationService.GetResource("Plugins.Payments.Stripe.Fields.StripeToken.Key");
            if (!processPaymentRequest.CustomValues.TryGetValue(tokenKey, out object stripeTokenObj) || !(stripeTokenObj is string) || !IsStripeTokenID((string)stripeTokenObj))
            {
                throw new NopException("Card token not received");
            }
            string stripeToken = stripeTokenObj.ToString();
            var service = new ChargeService();
            var chargeOptions = new ChargeCreateOptions
            {
                Amount = (long)(processPaymentRequest.OrderTotal * 100),
                Currency = "usd",
                Description = string.Format(StripePaymentDefaults.PaymentNote, processPaymentRequest.OrderGuid),
                SourceId = stripeToken,

            };

            if (customer.ShippingAddress != null)
            {
                chargeOptions.Shipping = new ChargeShippingOptions
                {
                    Address = MapNopAddressToStripe(customer.ShippingAddress),
                    Phone = customer.ShippingAddress.PhoneNumber,
                    Name = customer.ShippingAddress.FirstName + ' ' + customer.ShippingAddress.LastName
                };
            }

            var charge = service.Create(chargeOptions, GetStripeApiRequestOptions());

            var result = new ProcessPaymentResult();
            if (charge.Status == "succeeded")
            {
                result.NewPaymentStatus = PaymentStatus.Paid;
                result.AuthorizationTransactionId = charge.Id;
                result.AuthorizationTransactionResult = $"Transaction was processed by using {charge?.Source.Object}. Status is {charge.Status}";
                return result;
            }
            else
            {
                throw new NopException($"Charge error: {charge.FailureMessage}");
            }
        }


        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Full or partial refund
        /// </summary>
        /// <param name="refundPaymentRequest"></param>
        /// <returns></returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            string chargeID = refundPaymentRequest.Order.AuthorizationTransactionId;
            var orderAmtRemaining = refundPaymentRequest.Order.OrderTotal - refundPaymentRequest.AmountToRefund;
            bool isPartialRefund = orderAmtRemaining > 0;

            if (!IsChargeID(chargeID))
            {
                throw new NopException($"Refund error: {chargeID} is not a Stripe Charge ID. Refund cancelled");
            }
            var service = new RefundService();
            var refundOptions = new RefundCreateOptions
            {
                ChargeId = chargeID,
                Amount = (long)(refundPaymentRequest.AmountToRefund * 100),
                Reason = RefundReasons.RequestedByCustomer
            };
            var refund = service.Create(refundOptions, GetStripeApiRequestOptions());

            RefundPaymentResult result = new RefundPaymentResult();

            switch (refund.Status)
            {
                case "succeeded":
                    result.NewPaymentStatus = isPartialRefund ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded;
                    break;

                case "pending":
                    result.NewPaymentStatus = PaymentStatus.Pending;
                    result.AddError($"Refund failed with status of ${ refund.Status }");
                    break;

                default:
                    throw new NopException("Refund returned a status of ${refund.Status}");
            }
            return result;
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            IList<string> errors = new List<string>();
            if (!(form.TryGetValue("stripeToken", out StringValues stripeToken) || stripeToken.Count != 1 || !IsStripeTokenID(stripeToken[0])))
            {
                errors.Add("Token was not supplied or invalid");
            }
            return errors;
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new StripePaymentSettings
            {
                AdditionalFee = 0,
                AdditionalFeePercentage = false
            });


            //locales            
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Stripe.Fields.SecretKey", "Secret key, live or test (starts with sk_)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Stripe.Fields.PublishableKey", "Publishable key, live or test (starts with pk_)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Stripe.Fields.AdditionalFee", "Additional fee");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Stripe.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Stripe.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Stripe.Fields.StripeToken.Key", "Stripe Token");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Stripe.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Stripe.Instructions", @"
                <p>
                     For plugin configuration follow these steps:<br />
                    <br />
                    1. If you haven't already, create an account on Stripe.com and sign in<br />
                    2. In the Developers menu (left), choose the API Keys option.
                    3. You will see two keys listed, a Publishable key and a Secret key. You will need both. (If you'd like, you can create and use a set of restricted keys. That topic isn't covered here.)
                    <em>Stripe supports test keys and production keys. Use whichever pair is appropraite. There's no switch between test/sandbox and proudction other than using the appropriate keys.</em>
                    4. Paste these keys into the configuration page of this plug-in. (Both keys are required.) 
                    <br />
                    <em>Note: If using production keys, the payment form will only work on sites hosted with HTTPS. (Test keys can be used on http sites.) If using test keys, 
                    use these <a href='https://stripe.com/docs/testing'>test card numbers from Stripe</a>.</em><br />
                </p>");

            base.Install();
        }


        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<StripePaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Stripe.Fields.SecretKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Stripe.Fields.PublishableKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Stripe.Fields.AdditionalFee");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Stripe.Fields.AdditionalFee.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Stripe.Fields.AdditionalFeePercentage");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Stripe.Fields.AdditionalFeePercentage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Stripe.Instructions");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Stripe.Fields.StripeToken.Key");

            base.Uninstall();
        }

        #endregion

        #region Properties

        public bool SupportCapture => false;

        public bool SupportPartiallyRefund => true;

        public bool SupportRefund => true;

        public bool SupportVoid => false;

        
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

        public bool SkipPaymentInfo => false;

        public string PaymentMethodDescription => "Stripe";

        #endregion
    }
}
