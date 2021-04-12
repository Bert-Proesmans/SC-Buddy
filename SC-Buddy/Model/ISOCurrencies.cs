using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace SC_Buddy.Model
{
    // NOTE; User friendly name of the currency
    public record CurrencyName_English(string Value);
    // NOTE; Name of the currency as used within communications
    // REF; ISO 4217
    public record CurrencyName_ISO4217(string Value);
    // NOTE; Symbol representing the currency
    public record CurrencySymbol(string Value);

    public record ISOCurrency(IFormatProvider Formatting, CurrencyName_English Name, CurrencyName_ISO4217 ISOName, CurrencySymbol Symbol);

    public record Valuta(ISOCurrency Currency, decimal Value);

    class EqualISOCurrencyNameComparer : IEqualityComparer<ISOCurrency>
    {
        public bool Equals(ISOCurrency? x, ISOCurrency? y) =>
            EqualityComparer<CurrencyName_ISO4217>.Default.Equals(x?.ISOName, y?.ISOName);

        public int GetHashCode([DisallowNull] ISOCurrency obj) => obj.ISOName.GetHashCode();
    }

    class ISOCurrencies
    {
        static ISOCurrencies()
        {
            AllCurrencies = InitCurrencies();
        }

        public static ISOCurrency[] AllCurrencies { get; }

        private static ISOCurrency[] InitCurrencies() =>
            CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures)
                .Select(culture => (culture, new RegionInfo(culture.LCID)))
                .Select(x => new ISOCurrency(
                        x.culture,
                        new(x.Item2.CurrencyEnglishName),
                        new(x.Item2.ISOCurrencySymbol),
                        new(x.Item2.CurrencySymbol)))
                // WARN; Filter out all duplicate currency info because of translated RegionInfo objects.
                .Distinct(new EqualISOCurrencyNameComparer())
                .ToArray();

    }
}
