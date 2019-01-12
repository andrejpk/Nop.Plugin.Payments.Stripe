using Nop.Core.Configuration;
//using Nop.Plugin.Payments.Stripe.Domain;

namespace Nop.Plugin.Payments.Stripe
{
    /// <summary>
    /// Represents a Stripe payment settings
    /// </summary>
    public class StripePaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets OAuth2 application identifier
        /// </summary>
        public string SecretKey { get; set; }
        public string PublishableKey { get; set; }
        
        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
    }
}