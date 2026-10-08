# Merchant-only sale entry follow-up — 2026-10-08

QA found a bulk-sale button in the mobile pending-loot page. The audit also found bulk sale and common/rare auto-sale settings on the desktop pending-loot page. Remove those entry points from both pages and move them into each repository's existing merchant service. Merchant content scroll height and equipment row offsets now include three additional controls. Individual sale still uses the existing protected-item checks, and bulk sale keeps preset-impact confirmation and service protections.

Claim-all, single pending/recovery claim, first-clear rewards and exchange remain unchanged. Existing auto-sale preferences and pickup behavior remain compatible; only the location of their settings changes. Inventory, wear details, collection preview and workshop have no sale or auto-sale setter calls. Shared sale/confirmation helpers remain callable from the merchant service.

Validation: source-entry audit plus reward UI production fixture and whole runtime compilation against Unity references. These are not Unity GUI/device tests. Actual merchant scrolling/clicks and pending-loot claims still require Mac/device QA. No main merge until that QA passes.
