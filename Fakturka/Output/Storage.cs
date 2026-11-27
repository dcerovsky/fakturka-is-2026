using System.Globalization;
using System.Text;
using Fakturka.Logic;
using Fakturka.Models;

namespace Fakturka.Output;

/// <summary>
/// Ukládání a načítání dat do souborů CSV (hodnoty oddělené středníkem).
/// </summary>
/// <remarks>
/// Data jsou ve složce Fakturka v dokumentech uživatele: ne v repozitáři (veřejný GitHub
/// nesmí obsahovat údaje zákazníků) a ne vedle programu (nový build by je mohl smazat).
/// Středník a desetinná čárka je formát, který český Excel otevře bez převodu.
/// </remarks>
public static class Storage
{
    private const char Separator = ';';
    private const string SupplierFile = "dodavatel.csv";
    private const string CatalogFile = "katalog.csv";
    private const string InvoicesFile = "faktury.csv";
    private const string InvoiceLinesFile = "polozky.csv";

    // Datum v souborech vždy jako 27.11.2026: Excel ho pozná a nezávisí na nastavení systému
    private const string DateFormat = "dd.MM.yyyy";

    // Stejné meze jako při zadání odběratele na obrazovce faktury
    public const int MaxPartyNameLength = 60;
    public const int MaxAddressLength = 80;

    // Formát souborů nesmí záviset na nastavení systému, proto pevná česká kultura
    private static readonly CultureInfo FileCulture = new CultureInfo("cs-CZ");

    // UTF-8 se značkou BOM: podle ní Excel pozná kódování a zobrazí diakritiku správně
    private static readonly Encoding FileEncoding = new UTF8Encoding(true);

    /// <summary>
    /// Složka s daty. Proměnná prostředí FAKTURKA_DATA ji umí přesměrovat jinam,
    /// aby se při testování nepřepsala opravdová data.
    /// </summary>
    public static string DataFolder { get; } = ResolveDataFolder();

    // ------------------------------------------------------------
    // Dodavatel
    // ------------------------------------------------------------

    /// <summary>
    /// Načte údaje dodavatele. Vrátí null, když ještě nejsou uložené nebo je soubor poškozený.
    /// </summary>
    /// <param name="warning">Popis problému se souborem, jinak null.</param>
    public static Party? LoadSupplier(out string? warning)
    {
        warning = null;
        string[]? lines = ReadLines(SupplierFile, out warning);
        if (lines == null)
        {
            return null;   // soubor neexistuje (první spuštění) nebo nejde přečíst
        }

        // Řádek 0 je záhlaví, údaje jsou na řádku 1
        Party? supplier = lines.Length >= 2 ? ParseSupplier(lines[1]) : null;
        if (supplier == null)
        {
            warning = $"Soubor {SupplierFile} je poškozený. Vyplňte údaje dodavatele znovu (volba 5).";
        }

        return supplier;
    }

    /// <summary>
    /// Uloží údaje dodavatele. Vrátí false a popis chyby, když se zápis nepovede.
    /// </summary>
    public static bool SaveSupplier(Party supplier, out string? error)
    {
        string[] lines =
        {
            "název;adresa;IČO;DIČ;plátce DPH;předčíslí;číslo účtu;kód banky",
            string.Join(Separator, supplier.Name, supplier.Address, supplier.Ico, supplier.Dic,
                supplier.IsVatPayer ? "ano" : "ne", supplier.AccountPrefix, supplier.AccountNumber, supplier.BankCode)
        };

        return WriteLines(SupplierFile, lines, out error);
    }

    /// <summary>
    /// Převede řádek CSV na dodavatele. Vrátí null, když řádek není v pořádku.
    /// </summary>
    private static Party? ParseSupplier(string line)
    {
        string[] fields = line.Split(Separator);
        if (fields.Length != 8
            || !int.TryParse(fields[2], out int ico)
            || !long.TryParse(fields[5], out long prefix)
            || !long.TryParse(fields[6], out long number)
            || !int.TryParse(fields[7], out int bankCode))
        {
            return null;
        }

        string name = fields[0].Trim();
        string address = fields[1].Trim();
        string dic = fields[3].Trim();
        string vatPayer = fields[4].Trim();

        // Soubor mohl někdo upravit ručně, proto platí stejné kontroly jako při zadání z klávesnice
        if (name.Length == 0 || address.Length == 0
            || !Validators.IsValidIco(ico)
            || (vatPayer != "ano" && vatPayer != "ne")
            || (vatPayer == "ano" && !Validators.IsValidDic(dic))
            || bankCode < 1 || bankCode > 9999
            || !Validators.IsValidAccount(prefix, number))
        {
            return null;
        }

        Party supplier = new Party(name, address);
        supplier.Ico = ico;
        supplier.Dic = dic;
        supplier.IsVatPayer = vatPayer == "ano";
        supplier.AccountPrefix = prefix;
        supplier.AccountNumber = number;
        supplier.BankCode = bankCode;
        return supplier;
    }

    // ------------------------------------------------------------
    // Katalog
    // ------------------------------------------------------------

    /// <summary>
    /// Načte katalog ze souboru. Chybné řádky přeskočí a ohlásí je ve warning.
    /// </summary>
    public static void LoadCatalog(Catalog catalog, out string? warning)
    {
        string[]? lines = ReadLines(CatalogFile, out warning);
        if (lines == null)
        {
            return;
        }

        string skippedLines = "";

        // Řádek 0 je záhlaví, data začínají na řádku 1
        for (int i = 1; i < lines.Length; i++)
        {
            // Prázdný řádek (třeba na konci souboru) není chyba
            if (lines[i].Trim().Length == 0)
            {
                continue;
            }

            Product? product = ParseProduct(lines[i]);
            if (product == null || !catalog.Add(product))
            {
                // Čísla řádků od 1, jak je ukazuje Excel nebo textový editor
                skippedLines += (skippedLines.Length > 0 ? ", " : "") + (i + 1);
            }
        }

        if (skippedLines.Length > 0)
        {
            warning = $"V souboru {CatalogFile} jsou chybné řádky ({skippedLines}), program je přeskočil.";
        }
    }

    /// <summary>
    /// Uloží celý katalog. Vrátí false a popis chyby, když se zápis nepovede.
    /// </summary>
    public static bool SaveCatalog(Catalog catalog, out string? error)
    {
        // Pole řádků: 1 záhlaví + 1 řádek na každý produkt
        string[] lines = new string[catalog.Count + 1];
        lines[0] = "EAN;název;cena bez DPH;sazba DPH";

        for (int i = 0; i < catalog.Count; i++)
        {
            Product product = catalog.Get(i);
            lines[i + 1] = string.Join(Separator, product.Ean, product.Name,
                product.UnitPrice.ToString("0.00", FileCulture), product.VatRate.ToString("0", FileCulture));
        }

        return WriteLines(CatalogFile, lines, out error);
    }

    /// <summary>
    /// Převede řádek CSV na produkt. Vrátí null, když řádek není v pořádku.
    /// </summary>
    private static Product? ParseProduct(string line)
    {
        string[] fields = line.Split(Separator);
        if (fields.Length != 4
            || !long.TryParse(fields[0], out long ean)
            || !decimal.TryParse(fields[2], NumberStyles.Number, FileCulture, out decimal price)
            || !decimal.TryParse(fields[3], NumberStyles.Number, FileCulture, out decimal vatRate))
        {
            return null;
        }

        string name = fields[1].Trim();

        // Stejné kontroly jako při zadání z klávesnice - soubor mohl někdo upravit ručně v Excelu
        if (name.Length == 0 || name.Length > Product.MaxNameLength
            || (ean != 0 && !Validators.IsValidEan13(ean))
            || price < Product.MinPrice || price > Product.MaxPrice || decimal.Round(price, 2) != price
            || !VatRates.IsKnown(vatRate))
        {
            return null;
        }

        return new Product(ean, name, price, vatRate);
    }

    // ------------------------------------------------------------
    // Faktury
    // ------------------------------------------------------------

    /// <summary>
    /// Načte evidenci faktur: hlavičky z faktury.csv a položky z polozky.csv.
    /// Do evidence projdou jen faktury, ke kterým se našla aspoň jedna platná položka.
    /// </summary>
    public static void LoadInvoices(InvoiceBook book, out string? warning)
    {
        string[]? invoiceRows = ReadLines(InvoicesFile, out warning);
        if (invoiceRows == null)
        {
            return;
        }

        string[]? lineRows = ReadLines(InvoiceLinesFile, out string? linesWarning);

        // Faktury nejdřív do pomocné evidence, do skutečné je přeneseme až s položkami
        InvoiceBook loaded = new InvoiceBook();
        string badInvoiceRows = "";
        for (int i = 1; i < invoiceRows.Length; i++)
        {
            if (invoiceRows[i].Trim().Length == 0)
            {
                continue;
            }

            Invoice? invoice = ParseInvoice(invoiceRows[i]);

            // Dvě faktury se stejným číslem by rozbily číslování i variabilní symboly
            if (invoice == null || loaded.FindByNumber(invoice.Number) >= 0 || !loaded.Add(invoice))
            {
                badInvoiceRows = AppendNumber(badInvoiceRows, i + 1);
            }
        }

        // Každou položku přiřadíme k faktuře podle čísla (lineární vyhledávání v evidenci)
        string badLineRows = "";
        if (lineRows != null)
        {
            for (int i = 1; i < lineRows.Length; i++)
            {
                if (lineRows[i].Trim().Length > 0 && !AddParsedLine(lineRows[i], loaded))
                {
                    badLineRows = AppendNumber(badLineRows, i + 1);
                }
            }
        }

        string emptyInvoices = "";
        for (int i = 0; i < loaded.Count; i++)
        {
            Invoice invoice = loaded.Get(i);
            if (invoice.LineCount > 0)
            {
                book.Add(invoice);
            }
            else
            {
                emptyInvoices = AppendNumber(emptyInvoices, invoice.Number);
            }
        }

        // Všechny problémy shrneme do jedné hlášky
        string problems = "";
        if (linesWarning != null)
        {
            problems += linesWarning + " ";
        }

        if (badInvoiceRows.Length > 0)
        {
            problems += $"V souboru {InvoicesFile} jsou chybné řádky ({badInvoiceRows}). ";
        }

        if (badLineRows.Length > 0)
        {
            problems += $"V souboru {InvoiceLinesFile} jsou chybné řádky ({badLineRows}). ";
        }

        if (emptyInvoices.Length > 0)
        {
            problems += $"Faktury bez platných položek se nenačetly ({emptyInvoices}). ";
        }

        if (problems.Length > 0)
        {
            warning = problems.Trim();
        }
    }

    /// <summary>
    /// Uloží celou evidenci faktur do dvou souborů: hlavičky a položky.
    /// </summary>
    public static bool SaveInvoices(InvoiceBook book, out string? error)
    {
        // Velikost pole položek: 1 záhlaví + součet položek všech faktur
        int lineCount = 0;
        for (int i = 0; i < book.Count; i++)
        {
            lineCount += book.Get(i).LineCount;
        }

        string[] invoiceRows = new string[book.Count + 1];
        string[] lineRows = new string[lineCount + 1];
        invoiceRows[0] = "číslo;datum vystavení;DUZP;splatnost;s DPH;odběratel;adresa;IČO;DIČ";
        lineRows[0] = "číslo faktury;EAN;název;počet;cena bez DPH;sazba DPH";

        int row = 1;
        for (int i = 0; i < book.Count; i++)
        {
            Invoice invoice = book.Get(i);
            invoiceRows[i + 1] = string.Join(Separator, invoice.Number,
                invoice.IssueDate.ToString(DateFormat, FileCulture),
                invoice.TaxableDate.ToString(DateFormat, FileCulture),
                invoice.DueDate.ToString(DateFormat, FileCulture),
                invoice.WithVat ? "ano" : "ne",
                invoice.Customer.Name, invoice.Customer.Address, invoice.Customer.Ico, invoice.Customer.Dic);

            for (int j = 0; j < invoice.LineCount; j++)
            {
                InvoiceLine line = invoice.GetLine(j);
                lineRows[row] = string.Join(Separator, invoice.Number, line.Ean, line.Name, line.Quantity,
                    line.UnitPrice.ToString("0.00", FileCulture), line.VatRate.ToString("0", FileCulture));
                row++;
            }
        }

        // Nejdřív položky, pak hlavičky. Kdyby druhý zápis selhal, nová faktura se při
        // příštím načtení neobjeví vůbec (položky bez hlavičky se ohlásí jako chybné řádky).
        return WriteLines(InvoiceLinesFile, lineRows, out error)
            && WriteLines(InvoicesFile, invoiceRows, out error);
    }

    /// <summary>
    /// Převede řádek faktury.csv na fakturu bez položek. Vrátí null, když řádek není v pořádku.
    /// </summary>
    private static Invoice? ParseInvoice(string row)
    {
        string[] fields = row.Split(Separator);
        if (fields.Length != 9
            || !int.TryParse(fields[0], out int number)
            || !DateTime.TryParseExact(fields[1], DateFormat, FileCulture, DateTimeStyles.None, out DateTime issueDate)
            || !DateTime.TryParseExact(fields[2], DateFormat, FileCulture, DateTimeStyles.None, out DateTime taxableDate)
            || !DateTime.TryParseExact(fields[3], DateFormat, FileCulture, DateTimeStyles.None, out DateTime dueDate)
            || !int.TryParse(fields[7], out int ico))
        {
            return null;
        }

        string withVat = fields[4].Trim();
        string name = fields[5].Trim();
        string address = fields[6].Trim();
        string dic = fields[8].Trim();

        // Číslo musí mít tvar RRRRNNN s pořadím 1 až 999 a rok musí sedět s datem vystavení
        if (number / 1000 != issueDate.Year || number % 1000 < 1
            || dueDate < issueDate
            || (withVat != "ano" && withVat != "ne")
            || name.Length == 0 || name.Length > MaxPartyNameLength
            || address.Length == 0 || address.Length > MaxAddressLength
            || (ico != 0 && !Validators.IsValidIco(ico))
            || (dic.Length > 0 && !Validators.IsValidDic(dic)))
        {
            return null;
        }

        Party customer = new Party(name, address);
        customer.Ico = ico;
        customer.Dic = dic;
        customer.IsVatPayer = dic.Length > 0;

        Invoice invoice = new Invoice(customer, issueDate, withVat == "ano");
        invoice.Number = number;
        invoice.TaxableDate = taxableDate;
        invoice.DueDate = dueDate;
        return invoice;
    }

    /// <summary>
    /// Převede řádek polozky.csv na položku a přidá ji k faktuře se stejným číslem.
    /// Vrátí false, když je řádek chybný nebo faktura neexistuje či je plná.
    /// </summary>
    private static bool AddParsedLine(string row, InvoiceBook invoices)
    {
        string[] fields = row.Split(Separator);
        if (fields.Length != 6
            || !int.TryParse(fields[0], out int number)
            || !long.TryParse(fields[1], out long ean)
            || !int.TryParse(fields[3], out int quantity)
            || !decimal.TryParse(fields[4], NumberStyles.Number, FileCulture, out decimal price)
            || !decimal.TryParse(fields[5], NumberStyles.Number, FileCulture, out decimal vatRate))
        {
            return false;
        }

        string name = fields[2].Trim();
        if (name.Length == 0 || name.Length > Product.MaxNameLength
            || (ean != 0 && !Validators.IsValidEan13(ean))
            || quantity < 1 || quantity > InvoiceLine.MaxQuantity
            || price < Product.MinPrice || price > Product.MaxPrice || decimal.Round(price, 2) != price
            || !VatRates.IsKnown(vatRate))
        {
            return false;
        }

        int index = invoices.FindByNumber(number);
        return index >= 0 && invoices.Get(index).AddLine(new InvoiceLine(ean, name, quantity, price, vatRate));
    }

    /// <summary>
    /// Připojí číslo do seznamu odděleného čárkami („3, 7, 12“).
    /// </summary>
    private static string AppendNumber(string list, int number)
    {
        return list.Length == 0 ? number.ToString() : list + ", " + number;
    }

    // ------------------------------------------------------------
    // Společné pomocné metody
    // ------------------------------------------------------------

    private static string ResolveDataFolder()
    {
        string? custom = Environment.GetEnvironmentVariable("FAKTURKA_DATA");
        if (!string.IsNullOrWhiteSpace(custom))
        {
            return custom;
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Fakturka");
    }

    /// <summary>
    /// Přečte všechny řádky souboru. Vrátí null, když soubor neexistuje nebo nejde přečíst
    /// (v druhém případě vyplní i warning).
    /// </summary>
    private static string[]? ReadLines(string fileName, out string? warning)
    {
        warning = null;
        string path = Path.Combine(DataFolder, fileName);

        // Chybějící soubor není chyba: při prvním spuštění ještě nic uložené není
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return File.ReadAllLines(path, FileEncoding);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            // Soubor může mít zamčený jiný program nebo chybět oprávnění - tomu kontrolou předem nezabráníme
            warning = $"Soubor {fileName} nejde přečíst: {ex.Message}";
            return null;
        }
    }

    /// <summary>
    /// Zapíše řádky do souboru tak, aby se při chybě nepoškodil původní obsah.
    /// </summary>
    private static bool WriteLines(string fileName, string[] lines, out string? error)
    {
        // Pojistka: ukázková data nesmí přepsat skutečné soubory. Obrazovky, které ukládají,
        // jsou v ukázkovém režimu zablokované už v menu, tohle je druhá kontrola pro jistotu.
        if (DemoData.IsActive)
        {
            error = "V ukázkovém režimu se nic neukládá.";
            return false;
        }

        string path = Path.Combine(DataFolder, fileName);
        string tempPath = path + ".tmp";

        try
        {
            Directory.CreateDirectory(DataFolder);

            // Nejdřív zapíšeme dočasný soubor a teprve hotový ho přejmenujeme na ostrý.
            // Kdyby zápis selhal v půlce, původní soubor zůstane celý.
            File.WriteAllLines(tempPath, lines, FileEncoding);
            File.Move(tempPath, path, true);

            error = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            error = $"Soubor {fileName} nejde uložit: {ex.Message}";
            return false;
        }
    }
}
