namespace Fakturka.Logic;

/// <summary>
/// Kontroly údajů pomocí kontrolních číslic. Zachytí většinu překlepů dřív,
/// než se dostanou na fakturu (špatná číslice na faktuře = nezaplacená faktura).
/// </summary>
public static class Validators
{
    public const int MaxIco = 99_999_999;                 // IČO má nejvýš 8 číslic
    public const long MaxEan = 9_999_999_999_999;         // EAN má 13 číslic
    public const long MaxAccountPrefix = 999_999;         // předčíslí má nejvýš 6 číslic
    public const long MaxAccountNumber = 9_999_999_999;   // číslo účtu má nejvýš 10 číslic

    // Váhy pro jednotlivé pozice číslic. Pro čísla účtů je určuje vyhláška ČNB č. 169/2011 Sb.
    private static readonly int[] IcoWeights = { 8, 7, 6, 5, 4, 3, 2 };
    private static readonly int[] AccountPrefixWeights = { 10, 5, 8, 4, 2, 1 };
    private static readonly int[] AccountNumberWeights = { 6, 3, 7, 9, 10, 5, 8, 4, 2, 1 };

    /// <summary>
    /// Kontrola IČO: prvních 7 číslic se vynásobí vahami 8 až 2 a sečte.
    /// Z jejich součtu se dopočítá kontrolní číslice, která musí sedět s osmou číslicí.
    /// </summary>
    public static bool IsValidIco(int ico)
    {
        // Kratší číslo bereme jako IČO s úvodními nulami (6947 = 00006947)
        if (ico < 1 || ico > MaxIco)
        {
            return false;
        }

        int[] digits = ToDigits(ico, 8);
        int remainder = WeightedSum(digits, IcoWeights) % 11;

        // Zbytek 0 dává kontrolní číslici 1, zbytek 1 dává 0, jinak 11 - zbytek.
        // Výraz (11 - zbytek) % 10 pokryje všechny tři případy najednou.
        int check = (11 - remainder) % 10;
        return digits[7] == check;
    }

    /// <summary>
    /// Kontrola EAN-13: prvních 12 číslic střídavě s vahami 1 a 3,
    /// kontrolní číslice doplní součet na nejbližší násobek 10.
    /// </summary>
    public static bool IsValidEan13(long ean)
    {
        if (ean < 1 || ean > MaxEan)
        {
            return false;
        }

        int[] digits = ToDigits(ean, 13);
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            // 1., 3., 5. ... číslice má váhu 1, 2., 4., 6. ... váhu 3.
            // Index pole začíná nulou, takže 1. číslice = index 0 (sudý).
            int weight = i % 2 == 0 ? 1 : 3;
            sum += digits[i] * weight;
        }

        int check = (10 - sum % 10) % 10;
        return digits[12] == check;
    }

    /// <summary>
    /// Kontrola čísla účtu: předčíslí i samotné číslo musí mít vážený součet číslic dělitelný 11.
    /// </summary>
    public static bool IsValidAccount(long prefix, long number)
    {
        if (prefix < 0 || prefix > MaxAccountPrefix || number < 1 || number > MaxAccountNumber)
        {
            return false;
        }

        // Předčíslí 0 projde vždy, protože jeho vážený součet je 0
        bool prefixOk = WeightedSum(ToDigits(prefix, 6), AccountPrefixWeights) % 11 == 0;
        bool numberOk = WeightedSum(ToDigits(number, 10), AccountNumberWeights) % 11 == 0;
        return prefixOk && numberOk;
    }

    /// <summary>
    /// Kontrola formátu českého DIČ: CZ a 8 až 10 číslic
    /// (firmy CZ + IČO, fyzické osoby většinou CZ + rodné číslo).
    /// </summary>
    public static bool IsValidDic(string dic)
    {
        if (dic.Length < 10 || dic.Length > 12 || !dic.StartsWith("CZ"))
        {
            return false;
        }

        return AllDigits(dic.Substring(2));
    }

    /// <summary>
    /// Rozloží zápis účtu „19-2000145399/0800“ nebo „2000145399/0800“ na předčíslí, číslo a kód banky.
    /// Kontrolní součet neověřuje, jen tvar zápisu.
    /// </summary>
    public static bool TryParseAccount(string text, out long prefix, out long number, out int bankCode)
    {
        prefix = 0;
        number = 0;
        bankCode = 0;

        // Za lomítkem je kód banky, vždy přesně 4 číslice (0800, 0100, 2010...)
        string[] parts = text.Replace(" ", "").Split('/');
        if (parts.Length != 2 || parts[1].Length != 4 || !AllDigits(parts[1]))
        {
            return false;
        }

        bankCode = int.Parse(parts[1]);   // bezpečné: výše ověřeno, že jsou to 4 číslice

        // Před lomítkem je buď jen číslo účtu, nebo předčíslí-číslo
        string[] accountParts = parts[0].Split('-');
        string prefixText = accountParts.Length == 2 ? accountParts[0] : "0";
        string numberText = accountParts[accountParts.Length - 1];

        // AllDigits odmítne i prázdný text, takže „-2000145399“ nebo „19-“ neprojde
        if (accountParts.Length > 2
            || prefixText.Length > 6 || !AllDigits(prefixText)
            || numberText.Length > 10 || !AllDigits(numberText))
        {
            return false;
        }

        prefix = long.Parse(prefixText);
        number = long.Parse(numberText);
        return true;
    }

    /// <summary>
    /// Rozloží číslo na číslice do pole dané délky, chybějící úvodní číslice doplní nulami.
    /// </summary>
    private static int[] ToDigits(long number, int count)
    {
        // Stejný postup jako u ciferného součtu ze cvičení #5:
        // % 10 dá poslední číslici, / 10 ji z čísla odřízne.
        // Pole plníme odzadu, takže co na začátku zbude, zůstane 0.
        int[] digits = new int[count];
        for (int i = count - 1; i >= 0; i--)
        {
            digits[i] = (int)(number % 10);
            number /= 10;
        }

        return digits;
    }

    /// <summary>
    /// Součet součinů číslice x váha. Když je vah méně než číslic (IČO: 7 vah, 8 číslic),
    /// poslední číslice se nepočítá - je to ta kontrolní.
    /// </summary>
    private static int WeightedSum(int[] digits, int[] weights)
    {
        int sum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            sum += digits[i] * weights[i];
        }

        return sum;
    }

    /// <summary>
    /// True, když text není prázdný a obsahuje jen číslice 0-9.
    /// </summary>
    private static bool AllDigits(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (char c in text)
        {
            if (c < '0' || c > '9')
            {
                return false;
            }
        }

        return true;
    }
}
