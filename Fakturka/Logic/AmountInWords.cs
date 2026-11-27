namespace Fakturka.Logic;

/// <summary>
/// Částka slovy pro fakturu, např. 3240 -> „tři tisíce dvě stě čtyřicet korun českých“.
/// </summary>
public static class AmountInWords
{
    public const long MaxAmount = 999_999_999;

    // Index v poli = hodnota číslice. Na indexu 0 je prázdný text, protože nula
    // se ve složeném čísle nevyslovuje (205 = „dvě stě pět“).
    private static readonly string[] Ones = { "", "jedna", "dvě", "tři", "čtyři", "pět", "šest", "sedm", "osm", "devět" };
    private static readonly string[] Teens = { "deset", "jedenáct", "dvanáct", "třináct", "čtrnáct", "patnáct", "šestnáct", "sedmnáct", "osmnáct", "devatenáct" };
    private static readonly string[] Tens = { "", "", "dvacet", "třicet", "čtyřicet", "padesát", "šedesát", "sedmdesát", "osmdesát", "devadesát" };
    private static readonly string[] Hundreds = { "", "sto", "dvě stě", "tři sta", "čtyři sta", "pět set", "šest set", "sedm set", "osm set", "devět set" };

    /// <summary>
    /// Převede celou částku v korunách na slova.
    /// </summary>
    public static string Convert(long amount)
    {
        if (amount < 0 || amount > MaxAmount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), $"Částku slovy umím jen od 0 do {MaxAmount} Kč.");
        }

        if (amount == 0)
        {
            return "nula korun českých";
        }

        // Rozklad na trojice číslic, stejně jako rozklad na číslice ve cvičení #5, jen po tisících:
        // 1 234 567 -> miliony 1, tisíce 234, jednotky 567
        int millions = (int)(amount / 1_000_000);
        int thousands = (int)(amount / 1000 % 1000);
        int units = (int)(amount % 1000);

        string result = "";

        if (millions > 0)
        {
            // „milion“ je mužského rodu: jeden milion, dva miliony
            result = Join(result, GroupInWords(millions, false) + " " + PluralForm(millions, "milion", "miliony", "milionů"));
        }

        if (thousands == 1)
        {
            result = Join(result, "tisíc");   // říká se „tisíc dvě stě“, ne „jeden tisíc dvě stě“
        }
        else if (thousands > 1)
        {
            // „tisíc“ je také mužského rodu: dva tisíce, dvacet jeden tisíc
            result = Join(result, GroupInWords(thousands, false) + " " + PluralForm(thousands, "tisíc", "tisíce", "tisíc"));
        }

        if (units > 0)
        {
            // „koruna“ je ženského rodu: jedna koruna, dvě koruny
            result = Join(result, GroupInWords(units, true));
        }

        return result + " " + PluralForm(amount, "koruna česká", "koruny české", "korun českých");
    }

    /// <summary>
    /// Slova pro číslo 1 až 999 (jedna trojice číslic).
    /// </summary>
    /// <param name="feminine">True pro ženský rod (jedna, dvě), false pro mužský (jeden, dva).</param>
    private static string GroupInWords(int number, bool feminine)
    {
        // Stovky: „dvě stě“ platí v obou rodech, protože „sto“ je středního rodu
        string result = Hundreds[number / 100];
        int rest = number % 100;

        // 10 až 19 mají vlastní slova (jedenáct, ne „deset jedna“)
        if (rest >= 10 && rest <= 19)
        {
            return Join(result, Teens[rest - 10]);
        }

        result = Join(result, Tens[rest / 10]);

        int ones = rest % 10;
        string onesWord = Ones[ones];
        if (!feminine && ones == 1)
        {
            onesWord = "jeden";
        }
        else if (!feminine && ones == 2)
        {
            onesWord = "dva";
        }

        return Join(result, onesWord);
    }

    /// <summary>
    /// Vybere tvar slova podle počtu: 1 koruna, 2-4 koruny, 5 a víc korun.
    /// </summary>
    private static string PluralForm(long count, string one, string few, string many)
    {
        if (count == 1)
        {
            return one;
        }

        // Rozhoduje poslední číslice: dvacet dvě koruny, ale dvanáct korun (12-14 jsou výjimka)
        // a dvacet jedna korun (jednička na konci složeného čísla dává 2. pád)
        long lastDigit = count % 10;
        long lastTwoDigits = count % 100;
        if (lastDigit >= 2 && lastDigit <= 4 && (lastTwoDigits < 12 || lastTwoDigits > 14))
        {
            return few;
        }

        return many;
    }

    /// <summary>
    /// Spojí dvě části mezerou. Prázdnou část přeskočí (např. nulové stovky).
    /// </summary>
    private static string Join(string left, string right)
    {
        if (left.Length == 0)
        {
            return right;
        }

        if (right.Length == 0)
        {
            return left;
        }

        return left + " " + right;
    }
}
