using SC_Buddy.Data;
using SC_Buddy.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SC_Buddy.Helpers
{
    static class Currencies
    {
        private readonly static List<CurrencyName_ISO4217> _currencyNames;
        private readonly static IList<ISOCurrency> _currencies;

        static Currencies()
        {
            _currencies = ISOCurrencies.AllCurrencies.OrderBy(x => x.ISOName.Value).ToList();
            _currencyNames = _currencies.Select(x => x.ISOName).ToList();
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057:Use range operator", Justification = "That's ugly AF")]
        public static (ISOCurrency? Currency, decimal? Amount) ExtractCurrency(string content)
        {
            var idxCurrency = _currencyNames.FindIndex(currency => content.StartsWith(currency.Value));
            if (idxCurrency == -1) return (null, null);
            var currencyMatch = _currencies[idxCurrency];

            var currencySymbolIdx = content.IndexOf(currencyMatch.Symbol.Value, StringComparison.InvariantCulture);
            if (currencySymbolIdx == -1) return (currencyMatch, null);

            decimal? amountParsed = null;
            var amount = content.Substring(currencySymbolIdx).TrimEnd();
            var parsingOptions = NumberStyles.Currency | NumberStyles.AllowDecimalPoint | NumberStyles.AllowCurrencySymbol;
            if (decimal.TryParse(amount, parsingOptions, currencyMatch.Formatting, out decimal parseResult))
            {
                amountParsed = parseResult;
            }

            return (currencyMatch, amountParsed);
        }

        public static Valuta? ConvertCurrency(ISOCurrency target, ISOCurrency currency, decimal amount)
        {
            if (target == currency) return new(target, amount);

            var rate = ExchangeRates.ALL_RATES.FirstOrDefault(x => x.Target == currency);
            if (target == SuperChatData.KNOWN_CURRENCY)
                return rate?.ConvertBack(amount);

            if (currency == SuperChatData.KNOWN_CURRENCY)
                return rate?.Convert(amount);

            // NOTE; Use the KNOWN_CURRENCY as a jump rate
            var rate1 = ExchangeRates.ALL_RATES.FirstOrDefault(x => x.Target == currency);
            var rate2 = ExchangeRates.ALL_RATES.FirstOrDefault(x => x.Target == target);
            if (rate1 is not null && rate2 is not null)
                return rate2.Convert(rate1.ConvertBack(amount).Value);

            return null;
        }
    }
}
