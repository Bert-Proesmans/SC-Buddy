using SC_Buddy.Helpers;
using SC_Buddy.Model;
using System;
using System.Linq;
using Xunit;

namespace SC_Buddy.Tests
{
    public class ExchangeRate
    {
        [Fact]
        public void CalculateRates()
        {
            var euroCurrency = ISOCurrencies.AllCurrencies.First(x => x.ISOName == new CurrencyName_ISO4217("EUR"));

            var philippineCurrency = ISOCurrencies.AllCurrencies.First(x => x.ISOName == new CurrencyName_ISO4217("PHP"));
            Assert.InRange(
                Currencies.ConvertCurrency(euroCurrency, philippineCurrency, 10).Value,
                0.172M,
                0.173M);

            var usaCurrency = ISOCurrencies.AllCurrencies.First(x => x.ISOName == new CurrencyName_ISO4217("USD"));
            Assert.InRange(
                Currencies.ConvertCurrency(euroCurrency, usaCurrency, 10).Value,
                8.393M,
                8.394M);

            var singaporeCurrency = ISOCurrencies.AllCurrencies.First(x => x.ISOName == new CurrencyName_ISO4217("SGD"));
            Assert.InRange(
                Currencies.ConvertCurrency(euroCurrency, singaporeCurrency, 10).Value,
                6.262M,
                6.263M);
        }
    }
}
