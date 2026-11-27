namespace Fakturka.Logic;

/// <summary>
/// Výpočet mezinárodního čísla účtu (IBAN) z českého čísla účtu.
/// </summary>
public static class Iban
{
    /// <summary>
    /// Sestaví IBAN: CZ + 2 kontrolní číslice + kód banky (4) + předčíslí (6) + číslo účtu (10).
    /// </summary>
    public static string FromCzechAccount(int bankCode, long prefix, long number)
    {
        // Základ (BBAN): všechny části doplněné úvodními nulami na pevnou délku, celkem 20 číslic
        string bban = bankCode.ToString("D4") + prefix.ToString("D6") + number.ToString("D10");

        // Kontrolní číslice: kód země se převede na čísla (C = 12, Z = 35), připojí se
        // na konec spolu s 00 a z celého čísla se spočítá zbytek po dělení 97
        int remainder = Mod97(bban + "1235" + "00");
        int check = 98 - remainder;

        return "CZ" + check.ToString("D2") + bban;
    }

    /// <summary>
    /// Zapíše IBAN po skupinách 4 znaků, jak se uvádí na fakturách: CZ65 0800 0000 ...
    /// </summary>
    public static string Format(string iban)
    {
        string result = "";
        for (int i = 0; i < iban.Length; i++)
        {
            // Před každým čtvrtým znakem (kromě prvního) vložíme mezeru
            if (i > 0 && i % 4 == 0)
            {
                result += " ";
            }

            result += iban[i];
        }

        return result;
    }

    /// <summary>
    /// Zbytek po dělení 97 pro číslo zapsané jako text libovolné délky.
    /// </summary>
    private static int Mod97(string digits)
    {
        // Číslo má 26 číslic a do long (nejvýš 19 číslic) se nevejde.
        // Počítáme proto jako při písemném dělení: bereme číslici po číslici
        // a pamatujeme si jen zbytek, který nikdy nepřesáhne 96.
        int remainder = 0;
        foreach (char c in digits)
        {
            // Znak '7' má v tabulce znaků kód o 7 větší než '0', rozdíl je tedy hodnota číslice
            int digit = c - '0';
            remainder = (remainder * 10 + digit) % 97;
        }

        return remainder;
    }
}
