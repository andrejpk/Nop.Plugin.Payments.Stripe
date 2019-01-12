# Stripe payment provide for NopCommerce

This Payment provider supports Stripe's credit card payment gatway. 

## Features

 - Uses Stripe's native client-side secure payment forms (Stripe.js + Elements) so no credit card numbers pass through the server
 - Support for full and partial refunds
 - Postal code data automatically passes through to the Stripe forms so the user doesn't have to reenter it on the Stripe form
 - Postal code and CVC verification are implemented
 
## Limitations

Since this is an open source project, anyone is welcome to send pull requests that address these limits, where possible.

- US Dollars is the only supported currency
- Only credit card payments are supported (e.g. no e-checks)
- The Payment workflow is the only supported workflow (Capture/Void are not supported)
- No ability to save cards for a customer
- Recurring payments are not supported
- Only tested on the single-page checkout format

## License

This plug-in is covered by the nopCommerce Public License version 3.0 ("NPL")