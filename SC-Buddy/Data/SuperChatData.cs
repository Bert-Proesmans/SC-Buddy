using SC_Buddy.Model;
using System.Linq;
using System.Windows.Media;

namespace SC_Buddy.Data
{
    static class SuperChatData
    {
        public readonly static SuperChat RED;
        public readonly static SuperChat VANITY;
        public readonly static SuperChat AMBER;
        // Editor's note; Yes? This is yellow..
        public readonly static SuperChat SAFFRON;
        public readonly static SuperChat CARRIBEAN;
        public readonly static SuperChat CYAN;
        public readonly static SuperChat DENIM;
        public readonly static SuperChat UNKNOWN;

        public readonly static ISOCurrency KNOWN_CURRENCY;
        public readonly static SuperChat[] OrderedSuperchats;

        static SuperChatData()
        {
            var euroCurrency = ISOCurrencies.AllCurrencies.First(x => x.Symbol == new CurrencySymbol("€"));
            KNOWN_CURRENCY = euroCurrency;

            var redValuta = new Valuta(euroCurrency, 100);
            RED = new(Color.FromRgb(208, 0, 0), Color.FromRgb(230, 33, 23), Color.FromRgb(255, 255, 255), redValuta);

            var vanityValuta = new Valuta(euroCurrency, 50);
            VANITY = new(Color.FromRgb(194, 24, 91), Color.FromRgb(233, 30, 99), Color.FromRgb(255, 255, 255), vanityValuta);

            var amberValuta = new Valuta(euroCurrency, 20);
            AMBER = new(Color.FromRgb(230, 81, 0), Color.FromRgb(245, 124, 0), Color.FromRgb(255, 255, 255), amberValuta);

            var saffronValuta = new Valuta(euroCurrency, 10);
            SAFFRON = new(Color.FromRgb(255, 179, 0), Color.FromRgb(255, 202, 40), Color.FromRgb(0, 0, 0), saffronValuta);

            var carribbeanValuta = new Valuta(euroCurrency, 5);
            CARRIBEAN = new(Color.FromRgb(0, 191, 165), Color.FromRgb(29, 233, 182), Color.FromRgb(0, 0, 0), carribbeanValuta);

            var cyanValuta = new Valuta(euroCurrency, 2);
            CYAN = new(Color.FromRgb(0, 184, 212), Color.FromRgb(0, 229, 255), Color.FromRgb(0, 0, 0), cyanValuta);

            var denimValuta = new Valuta(euroCurrency, 1);
            DENIM = new(Color.FromRgb(21, 101, 192), default, Color.FromRgb(255, 255, 255), denimValuta);

            UNKNOWN = new(Color.FromRgb(81, 81, 81), Color.FromRgb(107, 107, 107), Color.FromRgb(255, 255, 255), new(euroCurrency, 0));

            OrderedSuperchats = new[] { RED, VANITY, AMBER, SAFFRON, CARRIBEAN, CYAN, DENIM };
        }

    }
}
