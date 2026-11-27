namespace Fakturka.Models;

/// <summary>
/// Zboží v katalogu e-shopu.
/// </summary>
public class Product
{
    // Logické meze, které platí pro zadání z klávesnice i pro data načtená ze souboru.
    // Název se na faktuře vypíše celý, v tabulce katalogu se případně zkrátí.
    public const int MaxNameLength = 40;
    public const decimal MinPrice = 0.01m;
    public const decimal MaxPrice = 1_000_000m;

    /// <summary>Kód EAN-13. Hodnota 0 znamená, že produkt kód nemá.</summary>
    public long Ean { get; set; }

    public string Name { get; set; }

    /// <summary>Cena za 1 ks bez DPH v Kč.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Sazba DPH v procentech, např. 21.</summary>
    public decimal VatRate { get; set; }

    public Product(long ean, string name, decimal unitPrice, decimal vatRate)
    {
        Ean = ean;
        Name = name;
        UnitPrice = unitPrice;
        VatRate = vatRate;
    }

    /// <summary>
    /// Cena za 1 ks včetně DPH, zaokrouhlená na haléře.
    /// </summary>
    public decimal PriceWithVat()
    {
        // AwayFromZero je obchodní zaokrouhlení (pětka nahoru). Výchozí Math.Round
        // zaokrouhluje „bankéřsky“ k sudé číslici (2,5 -> 2), což se u peněz nehodí.
        return Math.Round(UnitPrice * (1 + VatRate / 100), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// EAN pro výpis: vždy 13 číslic včetně úvodních nul, nebo „bez EAN“.
    /// </summary>
    public string EanText()
    {
        // long úvodní nuly nezná (0012... uloží jako 12...), formát D13 je doplní zpět
        return Ean == 0 ? "bez EAN" : Ean.ToString("D13");
    }
}
