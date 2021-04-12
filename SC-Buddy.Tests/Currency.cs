using SC_Buddy.Helpers;
using SC_Buddy.Model;
using System.Linq;
using Xunit;

namespace SC_Buddy.Tests
{
    public class Currency
    {
        [Fact]
        public void CurrencyRecognition()
        {
            Assert.Equal(
                (ISOCurrencies.AllCurrencies.First(x => x.ISOName == new CurrencyName_ISO4217("PHP")), 10), 
                Currencies.ExtractCurrency("PHP ₱10.00"));

            Assert.Equal(
                (ISOCurrencies.AllCurrencies.First(x => x.ISOName == new CurrencyName_ISO4217("USD")), 10),
                Currencies.ExtractCurrency("USD $10.00"));

            Assert.Equal(
                (ISOCurrencies.AllCurrencies.First(x => x.ISOName == new CurrencyName_ISO4217("SGD")), 10),
                Currencies.ExtractCurrency("SGD $10.00"));
        }
    }
}
