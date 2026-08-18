# ACSBounceSuppression

A reference implementation for handling **email bounce and suppression events in Azure Communication Services (ACS) Email**.

`ACSBounceSuppression` helps applications identify recipients that have hard-bounced or have been suppressed by ACS, so those addresses can be handled appropriately before additional email is sent.

## Overview

Azure Communication Services Email reports recipient-level delivery outcomes such as `Delivered`, `Bounced`, `Failed`, and `Suppressed`.

When an address produces a hard bounce, Azure Communication Services can temporarily add that recipient to its managed suppression list. Subsequent messages to the same address may then be dropped with a `Suppressed` status.

This project demonstrates a pattern for consuming those delivery results and incorporating bounce/suppression handling into an application.

```text
Application
    │
    │ Send email
    ▼
Azure Communication Services Email
    │
    │ Delivery processing
    ▼
Delivery status / Event Grid
    │
    ├── Delivered
    │
    ├── Bounced ──────► Record recipient as bounced
    │
    ├── Suppressed ───► Skip / flag future sends
    │
    └── Failed ───────► Handle according to failure reason
    │
    ▼
Application suppression logic
```

A typical integration follows these steps:

1. Send email using Azure Communication Services Email.
2. Receive or query the resulting delivery status.
3. Inspect the recipient-level status.
4. Detect `Bounced` or `Suppressed` recipients.
5. Store or flag those recipients in the application.
6. Exclude them from subsequent send operations where appropriate.

## Delivery statuses

Some of the ACS Email delivery states relevant to bounce handling include:

### `Delivered`

The message was handed to the recipient's mail transfer agent.

### `Bounced`

The recipient produced a hard bounce. Common causes include an invalid email address or invalid domain.

### `Suppressed`

The recipient previously hard-bounced and is currently present in the ACS managed suppression list. Additional email to the recipient is temporarily suppressed.

### `Failed`

Delivery failed for another reason. The associated status and SMTP response should be inspected before deciding whether the recipient should be retried or suppressed locally.

## Hard bounces vs. soft bounces

Not every delivery failure should permanently exclude a recipient.

**Hard bounces** generally indicate a permanent problem, such as:

```text
Recipient does not exist
Invalid domain
Mailbox/account disabled
Permanent recipient rejection
```

**Soft bounces** generally indicate a temporary problem, such as:

```text
Mailbox full
Temporary mail-server failure
Rate limiting
Temporary infrastructure issue
```

Applications should distinguish between the two before deciding whether future delivery attempts should be blocked.

## Integration with Event Grid

Azure Communication Services can publish email delivery events through **Azure Event Grid**.

A bounce-suppression implementation can subscribe to those events and react when delivery status changes:

```text
ACS Email
   │
   ▼
Azure Event Grid
   │
   ▼
Event Handler
   │
   ├── Delivered  → no suppression action
   ├── Bounced    → add/update bounce record
   ├── Suppressed → mark recipient as suppressed
   └── Failed     → inspect failure information
```

This approach avoids relying only on the initial send operation, since successful message submission does not necessarily mean that the message was ultimately delivered.

## Example suppression logic

The exact implementation depends on your application, but the decision flow can be represented as:

```pseudo
onDeliveryEvent(event):
    recipient = event.recipient
    status = event.deliveryStatus

    if status == "Bounced":
        markRecipientAsBounced(recipient)

    else if status == "Suppressed":
        markRecipientAsSuppressed(recipient)

    else if status == "Delivered":
        markRecipientAsDeliverable(recipient)

    else:
        recordDeliveryFailure(event)
```

Before sending another message:

```pseudo
if recipientIsSuppressed(recipient):
    skipSend()
else:
    sendEmail()
```
