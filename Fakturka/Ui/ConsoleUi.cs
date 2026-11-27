using Fakturka.Logic;

namespace Fakturka.Ui;

/// <summary>
/// Práce s konzolí: hlavička programu, hlášky a bezpečné načítání vstupů.
/// Všechny metody Read... opakují dotaz, dokud uživatel nezadá platnou hodnotu,
/// takže program na chybném vstupu nikdy nespadne.
/// </summary>
/// <remarks>
/// Barvy jsou vybrané tak, aby byly čitelné na tmavém i světlém pozadí terminálu.
/// Bílá by na světlém pozadí (výchozí Terminál na Macu) úplně zmizela.
/// </remarks>
public static class ConsoleUi
{
    private const int BannerWidth = 44;

    // ------------------------------------------------------------
    // Hlavička a nadpisy
    // ------------------------------------------------------------

    /// <summary>
    /// Smaže obrazovku a vypíše hlavičku programu v rámečku.
    /// </summary>
    public static void PrintBanner()
    {
        Clear();
        string line = new string('═', BannerWidth);

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"╔{line}╗");
        PrintBannerLine("", ConsoleColor.DarkGray);
        PrintBannerLine("F A K T U R K A", ConsoleColor.DarkYellow);
        PrintBannerLine("fakturace pro malý e-shop", ConsoleColor.DarkGray);
        PrintBannerLine("", ConsoleColor.DarkGray);
        PrintBannerLine("Daniel Čeřovský, IS 2026", ConsoleColor.DarkGray);
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"╚{line}╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    /// Vypíše jeden řádek rámečku s textem uprostřed.
    /// </summary>
    private static void PrintBannerLine(string text, ConsoleColor textColor)
    {
        // Centrování: vlevo polovina volného místa, zbytek doplní PadRight vpravo
        int left = (BannerWidth - text.Length) / 2;
        string centered = (new string(' ', left) + text).PadRight(BannerWidth);

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write("║");
        Console.ForegroundColor = textColor;
        Console.Write(centered);
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("║");
        Console.ResetColor();
    }

    /// <summary>
    /// Vypíše podtržený nadpis obrazovky.
    /// </summary>
    public static void PrintTitle(string title)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"  {title}");
        Console.WriteLine($"  {new string('─', title.Length)}");
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    /// Smaže obrazovku, ale jen když výstup jde opravdu do terminálu.
    /// </summary>
    public static void Clear()
    {
        // Při přesměrování výstupu (např. do souboru při testování) mazání nemá smysl
        // a na Windows by skončilo výjimkou
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
    }

    // ------------------------------------------------------------
    // Hlášky
    // ------------------------------------------------------------

    /// <summary>
    /// Chybová hláška: červeně a s pípnutím.
    /// </summary>
    public static void PrintError(string message)
    {
        ErrorBeep();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  Chyba: {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Potvrzení úspěšné akce: zeleně.
    /// </summary>
    public static void PrintSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"  {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Informace, která není chyba (prázdný katalog, nic se nezměnilo...): žlutě.
    /// </summary>
    public static void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// Pípnutí při chybném vstupu.
    /// </summary>
    public static void ErrorBeep()
    {
        // Jedno místo pro zvuk chyby - kdyby se měnil nebo vypínal, mění se jen tady.
        // Verze bez parametrů funguje na macOS i Windows. Console.Beep(800, 150)
        // s výškou a délkou tónu by na macOS skončila PlatformNotSupportedException.
        Console.Beep();
    }

    /// <summary>
    /// Počká, než uživatel stiskne Enter (aby stihl přečíst výsledek).
    /// </summary>
    public static void WaitForEnter()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Pokračujte klávesou Enter...");
        Console.ResetColor();
        ReadLineOrExit();
    }

    // ------------------------------------------------------------
    // Načítání vstupů
    // ------------------------------------------------------------

    /// <summary>
    /// Načte celé číslo v rozsahu min až max.
    /// </summary>
    /// <param name="error">Vlastní chybová hláška; bez ní se vypíše obecná s rozsahem.</param>
    public static int ReadInt(string prompt, int min, int max, string? error = null)
    {
        Console.Write(prompt);
        int value;
        // TryParse při nečíselném vstupu nespadne, jen vrátí false.
        // Druhá část podmínky hlídá logický rozsah - i platné číslo může být mimo meze.
        while (!int.TryParse(ReadLineOrExit(), out value) || value < min || value > max)
        {
            PrintError(error ?? $"Zadejte celé číslo od {min:#,0} do {max:#,0}.");
            Console.Write(prompt);
        }

        return value;
    }

    /// <summary>
    /// Načte celé číslo v rozsahu min až max. Samotný Enter vrátí výchozí hodnotu.
    /// </summary>
    public static int ReadIntWithDefault(string prompt, int min, int max, int defaultValue)
    {
        while (true)
        {
            Console.Write(prompt);
            string text = ReadLineOrExit().Trim();

            if (text.Length == 0)
            {
                return defaultValue;
            }

            if (int.TryParse(text, out int value) && value >= min && value <= max)
            {
                return value;
            }

            PrintError($"Zadejte celé číslo od {min:#,0} do {max:#,0}, nebo jen Enter pro {defaultValue}.");
        }
    }

    /// <summary>
    /// Načte velké celé číslo (EAN, číslo účtu) v rozsahu min až max.
    /// </summary>
    public static long ReadLong(string prompt, long min, long max, string? error = null)
    {
        Console.Write(prompt);
        long value;
        while (!long.TryParse(ReadLineOrExit(), out value) || value < min || value > max)
        {
            PrintError(error ?? $"Zadejte celé číslo od {min:#,0} do {max:#,0}.");
            Console.Write(prompt);
        }

        return value;
    }

    /// <summary>
    /// Načte částku v Kč v rozsahu min až max, nejvýš na 2 desetinná místa.
    /// </summary>
    public static decimal ReadDecimal(string prompt, decimal min, decimal max, string? error = null)
    {
        Console.Write(prompt);
        decimal value;
        while (!TryParseMoney(ReadLineOrExit(), out value) || value < min || value > max)
        {
            PrintError(error ?? $"Zadejte částku od {min:#,0.00} do {max:#,0.00} Kč, nejvýš na 2 desetinná místa.");
            Console.Write(prompt);
        }

        return value;
    }

    /// <summary>
    /// Načte neprázdný text o délce nejvýš maxLength znaků.
    /// </summary>
    public static string ReadText(string prompt, int maxLength)
    {
        Console.Write(prompt);
        // Trim odstraní mezery na začátku a na konci, aby „ Novák “ a „Novák“ byly totéž
        string text = ReadLineOrExit().Trim();

        // Středník je oddělovač v datových souborech CSV - v textu by rozbil řádek na víc sloupců
        while (text.Length == 0 || text.Length > maxLength || text.Contains(';'))
        {
            if (text.Length == 0)
            {
                PrintError("Text nesmí být prázdný.");
            }
            else if (text.Length > maxLength)
            {
                PrintError($"Text může mít nejvýš {maxLength} znaků, zadali jste {text.Length}.");
            }
            else
            {
                PrintError("Text nesmí obsahovat středník.");
            }

            Console.Write(prompt);
            text = ReadLineOrExit().Trim();
        }

        return text;
    }

    /// <summary>
    /// Zeptá se na otázku s odpovědí ano/ne. Přijímá a, n, ano, ne (velikost písmen nevadí).
    /// </summary>
    public static bool ReadYesNo(string prompt)
    {
        Console.Write($"{prompt} (a/n): ");
        string answer = ReadLineOrExit().Trim().ToLower();

        while (answer != "a" && answer != "n" && answer != "ano" && answer != "ne")
        {
            PrintError("Odpovězte a (ano), nebo n (ne).");
            Console.Write($"{prompt} (a/n): ");
            answer = ReadLineOrExit().Trim().ToLower();
        }

        return answer == "a" || answer == "ano";
    }

    // ------------------------------------------------------------
    // Načítání údajů s kontrolními číslicemi
    // ------------------------------------------------------------

    /// <summary>
    /// Načte IČO a ověří jeho kontrolní číslici.
    /// </summary>
    /// <param name="allowNone">True: 0 znamená „bez IČO“ (soukromá osoba).</param>
    public static int ReadIco(string prompt, bool allowNone)
    {
        int min = allowNone ? 0 : 1;
        string rangeError = allowNone
            ? "Zadejte IČO (nejvýš 8 číslic), nebo 0, když ho odběratel nemá."
            : "Zadejte IČO (nejvýš 8 číslic).";

        int ico = ReadInt(prompt, min, Validators.MaxIco, rangeError);
        while (ico != 0 && !Validators.IsValidIco(ico))
        {
            PrintError($"IČO {ico:D8} nesedí na kontrolní číslici, nejspíš je v něm překlep.");
            ico = ReadInt(prompt, min, Validators.MaxIco, rangeError);
        }

        return ico;
    }

    /// <summary>
    /// Načte EAN-13 a ověří jeho kontrolní číslici. 0 znamená „bez EAN“.
    /// </summary>
    public static long ReadEan(string prompt)
    {
        const string rangeError = "Zadejte kód EAN (13 číslic), nebo 0, když ho produkt nemá.";

        long ean = ReadLong(prompt, 0, Validators.MaxEan, rangeError);
        while (ean != 0 && !Validators.IsValidEan13(ean))
        {
            PrintError($"EAN {ean:D13} nesedí na kontrolní číslici, nejspíš je v něm překlep.");
            ean = ReadLong(prompt, 0, Validators.MaxEan, rangeError);
        }

        return ean;
    }

    /// <summary>
    /// Načte číslo účtu ve tvaru „19-2000145399/0800“ nebo „2000145399/0800“
    /// a ověří kontrolní součet. Části vrátí přes out parametry, stejně jako TryParse.
    /// </summary>
    public static void ReadAccount(string prompt, out long prefix, out long number, out int bankCode)
    {
        while (true)
        {
            Console.Write(prompt);
            string text = ReadLineOrExit();

            // Dvě různé chyby, dvě různé hlášky: špatný tvar zápisu vs. překlep v číslici
            if (!Validators.TryParseAccount(text, out prefix, out number, out bankCode))
            {
                PrintError("Zadejte účet ve tvaru předčíslí-číslo/kód banky, např. 19-2000145399/0800 nebo 2000145399/0800.");
            }
            else if (!Validators.IsValidAccount(prefix, number))
            {
                PrintError("Číslo účtu nesedí na kontrolní součet, nejspíš je v něm překlep.");
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>
    /// Načte DIČ ve tvaru CZ a 8 až 10 číslic. Mezery odstraní, malá písmena převede na velká.
    /// </summary>
    /// <param name="allowEmpty">True: samotný Enter znamená „bez DIČ“ a vrátí prázdný text.</param>
    public static string ReadDic(string prompt, bool allowEmpty = false)
    {
        Console.Write(prompt);
        string dic = ReadLineOrExit().Replace(" ", "").ToUpper();

        while (!(allowEmpty && dic.Length == 0) && !Validators.IsValidDic(dic))
        {
            PrintError("Zadejte DIČ ve tvaru CZ a 8 až 10 číslic, např. CZ12345679.");
            Console.Write(prompt);
            dic = ReadLineOrExit().Replace(" ", "").ToUpper();
        }

        return dic;
    }

    /// <summary>
    /// Převede text na částku. Přijme „1490,50“, „1490.50“ i „1 490,50“.
    /// </summary>
    private static bool TryParseMoney(string input, out decimal value)
    {
        // Mezery (i pevné, které používá česká kultura mezi tisíci) odstraníme
        // a tečku nahradíme čárkou, kterou čeká česká kultura jako desetinný oddělovač
        string text = input.Replace(" ", "").Replace(" ", "").Replace('.', ',');

        if (!decimal.TryParse(text, out value))
        {
            return false;
        }

        // Peníze mají nejvýš 2 desetinná místa (haléře), 10,555 Kč nedává smysl.
        // Když zaokrouhlení na 2 místa číslo změní, mělo jich víc.
        return decimal.Round(value, 2) == value;
    }

    /// <summary>
    /// Přečte řádek z konzole. Když vstup skončil, ukončí program.
    /// </summary>
    private static string ReadLineOrExit()
    {
        string? line = Console.ReadLine();

        // ReadLine vrací null, když vstup skončil (Ctrl+D na macOS, Ctrl+Z na Windows).
        // Bez této kontroly by se dotaz opakoval donekonečna a pořád by hlásil chybu.
        if (line == null)
        {
            Console.WriteLine();
            Console.WriteLine("Vstup byl ukončen, program končí.");
            Environment.Exit(0);
        }

        return line;
    }
}
