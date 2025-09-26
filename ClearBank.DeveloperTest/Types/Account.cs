namespace ClearBank.DeveloperTest.Types
{
    public class Account
    {
        public string AccountNumber { get; init; }
        public decimal Balance { get; set; }
        public AccountStatus Status { get; init; }
        public AllowedPaymentSchemes AllowedPaymentSchemes { get; init; }

        public Account() { }

        public bool Debit(decimal amount)
        {
            Balance -= amount;
            return true;
        }
    }
}
