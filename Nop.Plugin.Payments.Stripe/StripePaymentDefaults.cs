
namespace Nop.Plugin.Payments.Stripe
{
    /// <summary>
    /// Represents constants of the Stripe payment plugin
    /// </summary>
    public static class StripePaymentDefaults
    {
        /// <summary>
        /// Stripe payment method system name
        /// </summary>
        public static string SystemName => "Payments.Stripe";

        /// <summary>
        /// Name of the view component to display plugin in public store
        /// </summary>
        public const string ViewComponentName = "PaymentStripe";

        /// <summary>
        /// User agent used for requesting Stripe services
        /// </summary>
        public static string UserAgent => "Stripe-connect-nopCommerce-0.1";
        
        /// <summary>
        /// Path to the Stripe payment form js script
        /// </summary>
        public static string PaymentFormScriptPath => "https://js.stripe.com/v3/";


        /// <summary>
        /// Note passed for each payment transaction
        /// </summary>
        /// <remarks>
        /// {0} : Order Guid
        /// </remarks>
        public static string PaymentNote => "nopCommerce: {0}";
    }
}