using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Stripe.Models
{
    public class PaymentInfoModel : BaseNopModel
    {
        #region Ctor

        public PaymentInfoModel()
        {
            //StoredCards = new List<SelectListItem>();
        }

        #endregion

        #region Properties

        public bool IsGuest { get; set; }
        public string Errors { get; set; }
        public string StripeToken { get; set; }

        //public string CardNonce { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Stripe.Fields.PostalCode")]
        public string PostalCode { get; set; }

        //[NopResourceDisplayName("Plugins.Payments.Stripe.Fields.SaveCard")]
        //public bool SaveCard { get; set; }

        //[NopResourceDisplayName("Plugins.Payments.Stripe.Fields.StoredCard")]
        //public string StoredCardId { get; set; }
        //public IList<SelectListItem> StoredCards { get; set; }

        #endregion
    }
}