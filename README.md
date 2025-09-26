## Refactoring notes: 
1) Since AccountDataStore and BackupAccountDataStore share method signatures we can abstract these to implement a generic IAccountDataStore.
2) dataStoreType is an environment-wide setting rather than per user so we can easily move this to the Dependency Injection and inject the appropriate AccountDataStore/BackupAccountDataStore. This will separate the Account logic from the Payment logic and improve code readability and testability.
3) Add DataStoreType as an Enum to avoid usage of magic strings ("Backup").
4) In Account: AccountNumber, Status and AllowedPaymentSchemes should be readonly so changed from "set" to "init"
5) Add None as default AllowedPaymentSchemed Enum.
6) Move logic for Determining Success of Payment to private method. In the future if this were to become more complex and call external services it would make sense to move this logic to a separate service (i.e PaymentValidator) with it's own unit tests separate to the PaymentService.

## Unit Testing
### PaymentScheme Validation
 - request PaymentScheme matches AllowedPaymentScheme (cases for 1, 2, or 3 AllowedPaymentSchemes) -> Succeess = true
 - request PaymentScheme is not contained in AllowedPaymentSchemes -> Success = false 
### Assuming the PaymentScheme is allowed: 
 - For FasterPayments Payments, if Account Balance > Request Amount -> Success = true
 - For FasterPayments Payments, if Account Balance = Request Amount -> Success = true
 - For FasterPayments Payments, if Account Balance < Request Amount -> Success = false
 - For Chaps Payments, if AccountStatus = Live -> Success = true
 - For Chaps Payments, if AccountStatus = Disabled -> Success = false
 - For Chaps Payments, if AccountStatus = InboundPaymentsOnly -> Success = false
### Persistence Validation
When Success = true, UpdateAccount is called once with the updated balance (Balance - Amount)
When Success = false, UpdateAccount is never called.

### Unit tests questioning business logic
- For Bacs and FasterPayments, even if AccountStatus is not Live -> Success = true
- For Bacs or Chaps, even if Balance < Amount -> Success = true

### DependencyInjection tests
- Given Config DataStoreType = Backup/Primary (case-insensitive) appropriate AccountDataStore is registered
- Given other Configs or null -> throws InvalidOperationException.

## Out of Scope/Future Improvements:

- Include Failure Reason in MakePaymentResult and return as appropriate InsufficientFunds/AccountNotLive/SchemeNotAllowed.
- Guard against double-spend.
- Logging errors
- If the DataStoreType can vary per user, introduce a Factory.
- Assuming in a real-world scenario we'd call an external service that would return MakePaymentResult we could add a retry policy like Polly. 
- Validation for accounts (ensure valid account number, ensure balance is > 0


