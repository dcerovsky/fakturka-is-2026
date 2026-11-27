using Fakturka.Models;

namespace Fakturka.Logic;

/// <summary>
/// Ukázková data pro předvedení programu: dodavatel, katalog a náhodně vygenerované faktury.
/// Data jsou jen v paměti a nic se z nich neukládá, aby se nesmíchala se skutečnou evidencí.
/// </summary>
public static class DemoData
{
    public const int MaxInvoices = 500;
    public const int MaxSeed = 99_999;

    // Všechny ukázkové faktury jsou z jednoho pevného roku. Díky tomu dá stejný seed
    // pokaždé stejná data, ať se program spustí kdykoli, a žádná faktura není z budoucnosti.
    public const int Year = 2025;

    private const int DueDays = 14;
    private const int MaxLinesPerInvoice = 4;
    private const int MaxQuantity = 3;

    /// <summary>True, dokud je zapnutý ukázkový režim. Hlídá ho hlavička programu i ukládání.</summary>
    public static bool IsActive { get; set; }

    /// <summary>Seed generátoru, se kterým se ukázková data vytvořila.</summary>
    public static int Seed { get; set; }

    // Váhy měsíců (leden až prosinec): e-shop prodává nejvíc před Vánoci a nejmíň v létě.
    // Prosinec s vahou 16 vyjde zhruba čtyřikrát častěji než červenec s vahou 4.
    private static readonly int[] MonthWeights = { 6, 5, 6, 6, 7, 5, 4, 4, 6, 8, 12, 16 };

    private static readonly string[] MaleFirstNames = { "Jan", "Petr", "Tomáš", "Martin", "Jakub", "Lukáš", "Ondřej", "David" };
    private static readonly string[] MaleLastNames = { "Novák", "Svoboda", "Dvořák", "Černý", "Procházka", "Kučera", "Veselý", "Horák" };
    private static readonly string[] FemaleFirstNames = { "Jana", "Eva", "Lucie", "Tereza", "Kateřina", "Markéta", "Petra", "Anna" };
    private static readonly string[] FemaleLastNames = { "Nováková", "Svobodová", "Dvořáková", "Černá", "Procházková", "Kučerová", "Veselá", "Horáková" };

    private static readonly string[] CompanyNames =
    {
        "Kavárna Na Rohu s.r.o.", "Bistro Zelený dvůr s.r.o.", "Penzion Pod Ještědem s.r.o.",
        "Knihkupectví Mlýnek s.r.o.", "Pekárna Horák a syn s.r.o.", "Jóga studio Klid s.r.o."
    };

    private static readonly string[] Streets =
    {
        "Husova", "Masarykova", "Nádražní", "Školní", "Palackého", "Jiráskova", "Komenského", "Zahradní"
    };

    private static readonly string[] Towns =
    {
        "460 01 Liberec", "110 00 Praha 1", "602 00 Brno", "301 00 Plzeň", "370 01 České Budějovice",
        "500 02 Hradec Králové", "779 00 Olomouc", "702 00 Ostrava", "466 01 Jablonec nad Nisou"
    };

    /// <summary>
    /// Ukázkový dodavatel: čajový e-shop, plátce DPH.
    /// </summary>
    public static Party CreateSupplier()
    {
        Party supplier = new Party("Čajový lístek s.r.o.", "Husova 12, 460 01 Liberec");
        supplier.Ico = 23456787;
        supplier.Dic = "CZ23456787";
        supplier.IsVatPayer = true;
        supplier.AccountPrefix = 19;
        supplier.AccountNumber = 2000145399;
        supplier.BankCode = 800;
        return supplier;
    }

    /// <summary>
    /// Ukázkový katalog: čaj je potravina (snížená sazba 12 %), nádobí má základní sazbu 21 %.
    /// EAN kódy mají platnou kontrolní číslici.
    /// </summary>
    public static Catalog CreateCatalog()
    {
        Catalog catalog = new Catalog();
        catalog.Add(new Product(8594007650013, "Sencha Kagoshima 100 g", 189m, 12m));
        catalog.Add(new Product(8594007650020, "Assam Hajua 100 g", 129m, 12m));
        catalog.Add(new Product(8594007650037, "Rooibos vanilka 100 g", 99m, 12m));
        catalog.Add(new Product(8594007650044, "Pu-erh Shu 2019 100 g", 249m, 12m));
        catalog.Add(new Product(8594007650051, "Matcha Ceremonial 30 g", 459m, 12m));
        catalog.Add(new Product(8594007650068, "Earl Grey Bio 100 g", 119m, 12m));
        catalog.Add(new Product(8594007650075, "Oolong Milky 50 g", 179m, 12m));
        catalog.Add(new Product(8594007650082, "Konvice Tetsubin 0,6 l", 1290m, 21m));
        catalog.Add(new Product(8594007650099, "Čajová konvička sklo 0,8 l", 549m, 21m));
        catalog.Add(new Product(8594007650105, "Šálek porcelán 150 ml", 149m, 21m));
        catalog.Add(new Product(8594007650112, "Bambusová metlička chasen", 389m, 21m));
        catalog.Add(new Product(8594007650129, "Dárková krabička", 89.90m, 21m));
        return catalog;
    }

    /// <summary>
    /// Vygeneruje evidenci s daným počtem faktur. Se stejným seedem vyjdou vždy stejné faktury.
    /// </summary>
    public static InvoiceBook GenerateInvoices(Catalog catalog, Party supplier, int count, int seed)
    {
        // Generátor se seedem (cvičení #7): stejný seed = stejná posloupnost „náhodných“ čísel
        Random random = new Random(seed);

        // Firmy nakupují opakovaně, proto má každá stálé IČO a adresu.
        // Lidé se pokaždé vylosují znovu.
        Party[] companies = CreateCompanies(random);

        // 1. Faktury s náhodným datem, odběratelem a položkami, zatím bez čísla
        InvoiceBook generated = new InvoiceBook();
        for (int i = 0; i < count; i++)
        {
            DateTime date = RandomDate(random);
            Invoice invoice = new Invoice(RandomCustomer(companies, random), date, supplier.IsVatPayer);
            invoice.DueDate = date.AddDays(DueDays);

            int lineCount = random.Next(1, MaxLinesPerInvoice + 1);
            for (int j = 0; j < lineCount; j++)
            {
                AddRandomLine(invoice, catalog, random);
            }

            generated.Add(invoice);
        }

        // 2. Čísla faktur musí jít podle data. Faktury proto seřadíme stejným řazením
        //    jako v přehledu faktur a očíslujeme je až v seřazeném pořadí.
        Invoice[] byDate = generated.Copy();
        InvoiceBook.Sort(byDate, InvoiceBook.SortByDate, false);

        InvoiceBook book = new InvoiceBook();
        for (int i = 0; i < byDate.Length; i++)
        {
            byDate[i].Number = book.NextNumber(Year);
            book.Add(byDate[i]);
        }

        return book;
    }

    /// <summary>
    /// Náhodné datum v ukázkovém roce. Měsíc se losuje podle vah, takže některé měsíce jsou silnější.
    /// </summary>
    private static DateTime RandomDate(Random random)
    {
        // Vážené losování: čísla 0 až součet vah - 1 rozdělíme na úseky dlouhé jako váhy měsíců
        // a podíváme se, do kterého úseku padlo vylosované číslo.
        int totalWeight = 0;
        for (int month = 0; month < MonthWeights.Length; month++)
        {
            totalWeight += MonthWeights[month];
        }

        int ticket = random.Next(totalWeight);
        int chosenMonth = 0;
        while (ticket >= MonthWeights[chosenMonth])
        {
            ticket -= MonthWeights[chosenMonth];
            chosenMonth++;
        }

        int monthNumber = chosenMonth + 1;   // v poli od 0, v kalendáři od 1
        int day = random.Next(1, DateTime.DaysInMonth(Year, monthNumber) + 1);
        return new DateTime(Year, monthNumber, day);
    }

    /// <summary>
    /// Firemní odběratelé: každá firma z pole CompanyNames dostane náhodné platné IČO a adresu.
    /// </summary>
    private static Party[] CreateCompanies(Random random)
    {
        Party[] companies = new Party[CompanyNames.Length];
        for (int i = 0; i < companies.Length; i++)
        {
            companies[i] = new Party(CompanyNames[i], RandomAddress(random));
            companies[i].Ico = RandomIco(random);

            // Polovina firem je plátcem DPH. DIČ firmy je CZ + její IČO.
            if (random.Next(2) == 0)
            {
                companies[i].Dic = "CZ" + companies[i].Ico.ToString("D8");
                companies[i].IsVatPayer = true;
            }
        }

        return companies;
    }

    /// <summary>
    /// Náhodný odběratel: zhruba každý čtvrtý je některá z firem, ostatní jsou lidé bez IČO.
    /// </summary>
    private static Party RandomCustomer(Party[] companies, Random random)
    {
        if (random.Next(4) == 0)
        {
            // Faktury stejné firmy sdílejí jeden objekt odběratele. Vystavená faktura se nemění,
            // takže to nevadí, a firma se tak dá najít podle IČO napříč fakturami.
            return companies[random.Next(companies.Length)];
        }

        string name = random.Next(2) == 0
            ? $"{Pick(MaleFirstNames, random)} {Pick(MaleLastNames, random)}"
            : $"{Pick(FemaleFirstNames, random)} {Pick(FemaleLastNames, random)}";
        return new Party(name, RandomAddress(random));
    }

    /// <summary>
    /// Náhodná adresa: ulice, číslo domu, PSČ a obec.
    /// </summary>
    private static string RandomAddress(Random random)
    {
        return $"{Pick(Streets, random)} {random.Next(1, 200)}, {Pick(Towns, random)}";
    }

    /// <summary>
    /// Náhodné IČO s platnou kontrolní číslicí.
    /// </summary>
    private static int RandomIco(Random random)
    {
        // Losujeme, dokud nevyjde číslo s platnou kontrolní číslicí. Kontrolní číslice je
        // jen jedna z deseti možných, takže to v průměru vyjde zhruba na desátý pokus.
        int ico;
        do
        {
            ico = random.Next(10_000_000, 100_000_000);
        }
        while (!Validators.IsValidIco(ico));

        return ico;
    }

    /// <summary>
    /// Přidá na fakturu náhodný produkt z katalogu. Stejný produkt jen navýší počet kusů,
    /// stejně jako při ručním vystavení.
    /// </summary>
    private static void AddRandomLine(Invoice invoice, Catalog catalog, Random random)
    {
        Product product = catalog.Get(random.Next(catalog.Count));
        int quantity = random.Next(1, MaxQuantity + 1);

        int existing = invoice.FindLine(product.Name, product.UnitPrice, product.VatRate);
        if (existing >= 0)
        {
            invoice.GetLine(existing).Quantity += quantity;
        }
        else
        {
            invoice.AddLine(new InvoiceLine(product.Ean, product.Name, quantity, product.UnitPrice, product.VatRate));
        }
    }

    /// <summary>
    /// Náhodný prvek pole.
    /// </summary>
    private static string Pick(string[] values, Random random)
    {
        return values[random.Next(values.Length)];
    }
}
