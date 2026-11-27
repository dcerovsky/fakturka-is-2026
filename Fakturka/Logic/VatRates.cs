namespace Fakturka.Logic;

/// <summary>
/// Sazby DPH, se kterými program počítá.
/// </summary>
public static class VatRates
{
    // Sazby v procentech, platné od 1. 1. 2024. Když se zákon změní, stačí upravit
    // tato dvě pole - zbytek programu se řídí jejich délkou, nemá sazby napevno.
    public static readonly decimal[] All = { 21m, 12m };
    public static readonly string[] Names = { "základní", "snížená" };
}
