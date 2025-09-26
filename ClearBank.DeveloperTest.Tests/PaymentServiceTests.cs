using ClearBank.DeveloperTest.Abstractions;
using ClearBank.DeveloperTest.Services;
using ClearBank.DeveloperTest.Types;
using Moq;
using Xunit;

namespace ClearBank.DeveloperTest.Tests
{
    public class PaymentServiceTests
    {
        public static IEnumerable<object[]> SchemeCases =>
            [
                [PaymentScheme.Bacs, AllowedPaymentSchemes.Bacs, true],
                [PaymentScheme.FasterPayments, AllowedPaymentSchemes.FasterPayments,  true],
                [PaymentScheme.Chaps, AllowedPaymentSchemes.Chaps, true],
                [PaymentScheme.Bacs, AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps, true],
                [PaymentScheme.Bacs, AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.FasterPayments | AllowedPaymentSchemes.Chaps , true],
                [PaymentScheme.Bacs, AllowedPaymentSchemes.None, false],
                [PaymentScheme.FasterPayments, AllowedPaymentSchemes.None, false],
                [PaymentScheme.Chaps, AllowedPaymentSchemes.None, false],
                [PaymentScheme.Bacs, AllowedPaymentSchemes.FasterPayments, false],
                [PaymentScheme.Bacs, AllowedPaymentSchemes.Chaps, false]
            ];

        [Theory]
        [MemberData(nameof(SchemeCases))]
        public void MakePayment_SchemeCheck_SucceedsWhenAllowed(PaymentScheme scheme, AllowedPaymentSchemes allowedPaymentSchemes, bool expectedResult)
        {
            var mockAccountStore = new Mock<IAccountDataStore>();

            var account = new Account()
            {
                Balance = 500m,
                Status = AccountStatus.Live,
                AllowedPaymentSchemes = allowedPaymentSchemes
            };

            mockAccountStore.Setup(s => s.GetAccount(It.IsAny<string>())).Returns(account);

            var paymentService = new PaymentService(mockAccountStore.Object);
            var paymentRequest= new MakePaymentRequest { Amount = 100m, PaymentScheme = scheme };

            var result = paymentService.MakePayment(paymentRequest);

            Assert.Equal(expectedResult, result.Success);
        }

        [Theory]
        [InlineData(100, 50, true)]
        [InlineData(100, 100, true)]
        [InlineData(100, 150, false)]
        public void MakePayment_FasterPayments_SucceedsDependingOnBalance(decimal balance, decimal debitAmount, bool expectedResult)
        {
            var mockAccountStore = new Mock<IAccountDataStore>();

            var account = new Account
            {
                Balance = balance,
                Status = AccountStatus.Live,
                AllowedPaymentSchemes = AllowedPaymentSchemes.FasterPayments
            };

            mockAccountStore.Setup(s => s.GetAccount(It.IsAny<string>())).Returns(account);

            var paymentService = new PaymentService(mockAccountStore.Object);
            var paymentRequest= new MakePaymentRequest { Amount = debitAmount, PaymentScheme = PaymentScheme.FasterPayments };

            var result = paymentService.MakePayment(paymentRequest);

            Assert.Equal(expectedResult, result.Success);
        }

        [Theory]
        [InlineData(AccountStatus.Live, true)]
        [InlineData(AccountStatus.Disabled, false)]
        [InlineData(AccountStatus.InboundPaymentsOnly, false)]
        public void MakePayment_ChapsPayments_SucceedsDependingOnAccountStatus(AccountStatus accountStatus, bool expectedResult)
        {
            var mockAccountStore = new Mock<IAccountDataStore>();

            var account = new Account
            {
                Balance = 500m,
                Status = accountStatus,
                AllowedPaymentSchemes = AllowedPaymentSchemes.Chaps
            };

            mockAccountStore.Setup(s => s.GetAccount(It.IsAny<string>())).Returns(account);

            var paymentService = new PaymentService(mockAccountStore.Object);
            var paymentRequest= new MakePaymentRequest { Amount = 100m, PaymentScheme = PaymentScheme.Chaps };

            var result = paymentService.MakePayment(paymentRequest);

            Assert.Equal(expectedResult, result.Success);
        }

        [Fact]
        public void MakePayment_Succeeds_UpdatesAccountWithCorrectBalance()
        {
            var mockAccountStore = new Mock<IAccountDataStore>();

            var account = new Account
            {
                Balance = 500m,
                Status = AccountStatus.Live,
                AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs
            };

            mockAccountStore.Setup(s => s.GetAccount(It.IsAny<string>())).Returns(account);

            var paymentService = new PaymentService(mockAccountStore.Object);
            var paymentRequest = new MakePaymentRequest { Amount = 100m, PaymentScheme = PaymentScheme.Bacs };

            var result = paymentService.MakePayment(paymentRequest);

            Assert.True(result.Success);
            mockAccountStore.Verify(s => s.UpdateAccount(It.Is<Account>(a => a.Balance == 400m)), Times.Once);
        }

        [Fact]
        public void MakePayment_Fails_UpdatesAccountWithCorrectBalance()
        {
            var mockAccountStore = new Mock<IAccountDataStore>();

            var account = new Account
            {
                Balance = 500m,
                Status = AccountStatus.Live,
                AllowedPaymentSchemes = AllowedPaymentSchemes.None
            };

            mockAccountStore.Setup(s => s.GetAccount(It.IsAny<string>())).Returns(account);

            var paymentService = new PaymentService(mockAccountStore.Object);
            var paymentRequest = new MakePaymentRequest { Amount = 100m, PaymentScheme = PaymentScheme.Bacs };

            var result = paymentService.MakePayment(paymentRequest);

            Assert.False(result.Success);
            mockAccountStore.Verify(s => s.UpdateAccount(It.IsAny<Account>()), Times.Never);
        }

        //Below are tests passing but should maybe fail?
        [Theory]
        [InlineData(AccountStatus.Disabled, PaymentScheme.Bacs)]
        [InlineData(AccountStatus.InboundPaymentsOnly, PaymentScheme.Bacs)]
        [InlineData(AccountStatus.Disabled, PaymentScheme.FasterPayments)]
        [InlineData(AccountStatus.InboundPaymentsOnly, PaymentScheme.FasterPayments)]
        public void MakePayment_BacsOrFasterPayments_AccountStatusNotLive_Succeeds(AccountStatus accountStatus, PaymentScheme paymentScheme)
        {
            var mockAccountStore = new Mock<IAccountDataStore>();

            var account = new Account
            {
                Balance = 500m,
                Status = accountStatus,
                AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.FasterPayments
            };

            mockAccountStore.Setup(s => s.GetAccount(It.IsAny<string>())).Returns(account);

            var paymentService = new PaymentService(mockAccountStore.Object);
            var paymentRequest= new MakePaymentRequest { Amount = 100m, PaymentScheme = paymentScheme };

            var result = paymentService.MakePayment(paymentRequest);

            Assert.True(result.Success);
        }

        [Theory]
        [InlineData(PaymentScheme.Bacs)]
        [InlineData(PaymentScheme.Chaps)]
        public void MakePayment_BacsOrChaps_PaymentBalanceLessThanAmount_Succeeds(PaymentScheme paymentScheme)
        {
            var mockAccountStore = new Mock<IAccountDataStore>();

            var account = new Account
            {
                Balance = 500m,
                Status = AccountStatus.Live,
                AllowedPaymentSchemes = AllowedPaymentSchemes.Bacs | AllowedPaymentSchemes.Chaps
            };

            mockAccountStore.Setup(s => s.GetAccount(It.IsAny<string>())).Returns(account);

            var paymentService = new PaymentService(mockAccountStore.Object);
            var paymentRequest= new MakePaymentRequest { Amount = 1000m, PaymentScheme = paymentScheme };

            var result = paymentService.MakePayment(paymentRequest);

            Assert.True(result.Success);
        }
    }
}
