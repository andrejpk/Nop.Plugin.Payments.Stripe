using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Payments.Stripe.Models;
using Nop.Services.Common;
using Nop.Services.Localization;

namespace Nop.Plugin.Payments.Stripe.Components
{
    [ViewComponent(Name = StripePaymentDefaults.ViewComponentName)]
    public class PaymentStripeViewComponent : ViewComponent
    {
        #region Fields

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        //private readonly StripePaymentManager _stripePaymentManager;

        #endregion

        #region Ctor

        public PaymentStripeViewComponent(IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IWorkContext workContext)
        {
            this._genericAttributeService = genericAttributeService;
            this._localizationService = localizationService;
            this._workContext = workContext;
        }

        #endregion

        #region Methods

        public IViewComponentResult Invoke()
        {
            PaymentInfoModel model = new PaymentInfoModel
            {
                //whether current customer is guest
                IsGuest = _workContext.CurrentCustomer.IsGuest(),
                PostalCode = _workContext.CurrentCustomer.BillingAddress?.ZipPostalCode ?? _workContext.CurrentCustomer.ShippingAddress?.ZipPostalCode,
            };

            return View("~/Plugins/Payments.Stripe/Views/PaymentInfo.cshtml", model);
        }

        #endregion
    }
}