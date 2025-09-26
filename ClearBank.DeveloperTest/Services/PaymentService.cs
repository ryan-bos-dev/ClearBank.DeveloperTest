using ClearBank.DeveloperTest.Abstractions;
using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.Services
{
    public class PaymentService(IAccountDataStore accountDataStore) : IPaymentService
    {
        public MakePaymentResult MakePayment(MakePaymentRequest request)
        {
            var account = accountDataStore.GetAccount(request.DebtorAccountNumber);

            var result = TryMakePayment(account, request);

            if (result.Success)
            {
                account.Debit(request.Amount);
                accountDataStore.UpdateAccount(account);
            }

            return result;
        }

        private static MakePaymentResult TryMakePayment(Account account, MakePaymentRequest request)
        {
            if (account == null)
            {
                return new MakePaymentResult(false);
            }

            switch (request.PaymentScheme)
            {
                case PaymentScheme.Bacs:

                    if (!account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.Bacs))
                    {
                        return new MakePaymentResult(false);
                    }
                    break;

                case PaymentScheme.FasterPayments:
                    if (!account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.FasterPayments))
                    {
                        return new MakePaymentResult(false);
                    }
                    else if (account.Balance < request.Amount)
                    {
                        return new MakePaymentResult(false);
                    }
                    break;

                case PaymentScheme.Chaps:
                    if (!account.AllowedPaymentSchemes.HasFlag(AllowedPaymentSchemes.Chaps))
                    {
                        return new MakePaymentResult(false);
                    }
                    else if (account.Status != AccountStatus.Live)
                    {
                        return new MakePaymentResult(false);
                    }
                    break;
            }

            return new MakePaymentResult(true);
        }
    }
}
