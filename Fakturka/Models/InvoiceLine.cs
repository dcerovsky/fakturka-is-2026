namespace Fakturka.Models;

/// <summary>
/// Jedna položka faktury.
/// </summary>
/// <remarks>
/// Název, cena a sazba jsou kopie údajů z katalogu v okamžiku vystavení, ne odkaz na produkt.
/// Když se cena v katalogu později změní, vystavená faktura musí zůstat s cenou,
/// za kterou se opravdu prodávalo.
/// </remarks>
public class InvoiceLine
{
    public const int MaxQuantity = 9999;

    /// <summary>EAN produktu, 0 = bez EAN.</summary>
    public long Ean { get; }

    public string Name { get; }

    /// <summary>Počet kusů. Jde měnit, když se stejný produkt přidá na fakturu znovu.</summary>
    public int Quantity { get; set; }

    /// <summary>Cena za 1 ks bez DPH v Kč.</summary>
    public decimal UnitPrice { get; }

    /// <summary>Sazba DPH v procentech.</summary>
    public decimal VatRate { get; }

    public InvoiceLine(long ean, string name, int quantity, decimal unitPrice, decimal vatRate)
    {
        Ean = ean;
        Name = name;
        Quantity = quantity;
        UnitPrice = unitPrice;
        VatRate = vatRate;
    }

    /// <summary>
    /// Základ daně položky: počet kusů x cena za kus bez DPH.
    /// </summary>
    public decimal Base()
    {
        // Cena má nejvýš 2 desetinná místa a počet je celé číslo, takže výsledek je přesný
        return Quantity * UnitPrice;
    }
}
