using SC_Buddy.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SC_Buddy.Model
{
    public record ExchangeRate(ISOCurrency BaseRateCurrency, ISOCurrency Target, decimal Rate)
    {
        public Valuta Convert(decimal sourceAmount) => new(Target, sourceAmount * Rate);
        public Valuta ConvertBack(decimal sourceAmount) => new(BaseRateCurrency, sourceAmount / Rate);
    }

    public record ExchangeRateResponse(bool Success, long UnixTimestamp, string Base, Dictionary<string, decimal> Rates);

    class ExchangeRates
    {
        private const string EXCHANGE_DATA_PATH = "./Data/exchange-rates.json";

        static ExchangeRates()
        {
            (KNOWN_CURRENCY, ALL_RATES) = InitExchangeRates(new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
        }

        public static ISOCurrency KNOWN_CURRENCY { get; }
        public static ExchangeRate[] ALL_RATES { get; }

        private static ExchangeRateResponse? LoadExchangeData(JsonSerializerOptions serializerOptions) =>
            JsonSerializer.Deserialize<ExchangeRateResponse>(File.ReadAllText(EXCHANGE_DATA_PATH), serializerOptions);

        private static (ISOCurrency, ExchangeRate[]) InitExchangeRates(JsonSerializerOptions serializerOptions)
        {
            var currencies = ISOCurrencies.AllCurrencies.ToDictionary(x => x.ISOName);
            var exchangeData = LoadExchangeData(serializerOptions) ?? throw new InvalidDataException($"Couldn't load exchange rate data");

            if (!currencies.TryGetValue(new(exchangeData.Base), out ISOCurrency? baseCurrency))
                throw new InvalidDataException($"Base currency identifier '{exchangeData.Base}' wasn't recognized");

            var rates = currencies
                .Select(kv => exchangeData.Rates.TryGetValue(kv.Key.Value, out decimal rate) ?
                    (Currency: kv.Value, Rate: (decimal?)rate) :
                        (Currency: kv.Value, Rate: null))
                // NOTE; Silently ignore unknown exchange rates..
                .Where(x => x.Currency != null && x.Rate != null)
                .Select(tup => new ExchangeRate(baseCurrency, tup.Currency, tup.Rate!.Value))
                .ToArray();

            return (baseCurrency, rates);
        }
    }
}
