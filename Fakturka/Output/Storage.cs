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
