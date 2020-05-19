# PageSniffer
Periodically check a web page for a specific value and send a [Pushover](https://pushover.net/) notification on state change.

## Configuration
You'll find all of these settings in the `appsettings.json` file.

```
"configuration": {
    "periodInSeconds": 900,
    "periodVariationPercentage": 20
}
```
* `periodInSeconds`: How often you want to check the page. 900 seconds here is every 15 minutes.
* `periodVariationPercentage`: A whole number between 1 and 100 (0 to disable) representing a percentage of the period to randomly offset. In this case, 20% of 900 seconds (15 minutes) is 180 seconds (3 minutes). A random value between -180 and 180 seconds will be added to the 900 second period. This can give your page refresh the appearence that it is being done by a human and not by code every 15 minutes exactly.

```
"pushover": {
    "appKey": "",
    "userKey": ""
}
```
* These values need to be created at https://pushover.net. You will need to create a user account to obtain a `userKey` and create an application to obtain an `appKey`. You will also want to download once of their [device clients](https://pushover.net/clients) to recieve notifications. The service is free for a short term and the [pricing](https://pushover.net/pricing) is reasonable. I've found the service to be useful in a number of other projects.

```
"webpages": [{
        "enabled": true,
        "name": "Red/Blue Nintendo Switch",
        "url": "https://www.bestbuy.com/site/nintendo-switch-32gb-console-neon-red-neon-blue-joy-con/6364255.p?skuId=6364255",
        "nodePath": "//button",
        "nodeFilter": "add-to-cart-button",
        "alertTrigger": "Add to Cart",
        "alertActive": false
    },
    {
        "enabled": false,
        "name": "Zelda Breath of the Wild",
        "url": "https://www.bestbuy.com/site/the-legend-of-zelda-breath-of-the-wild-nintendo-switch/5721500.p?skuId=5721500",
        "nodePath": "//button",
        "nodeFilter": "add-to-cart-button",
        "alertTrigger": "Add to Cart",
        "alertActive": false
    }
]
```
* `webpages`: An array of webpages to check.
* `enabled`: If this page should be checked.
* `name`: Short name describing the page.
* `url`: Url of the page to check.
* `nodePath`: XPath to the HTML node/element containing the value to check. `//button` will load in all `<button>` elements. These can be filtered futher with the `nodeFilter` setting.
* `nodeFilter`: Unique string to search for within the `nodePath`. In this case `add-to-cart-button` is a class applied only to the `Add to Cart` button on this webpage.
* `alertTrigger`: Value to search for within the filtered node. If this value is found, and the state has changed from the previous check, a Pushover notification will be sent.
* `alertActive`: Sets the initial state at run. Mostly used for debugging, but can help avoid initial notifications if the trigger value is already present.

## Attribution
PageSniffer icon made by [Smashicons](https://www.flaticon.com/authors/smashicons) from www.flaticon.com
