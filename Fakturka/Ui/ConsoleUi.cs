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
