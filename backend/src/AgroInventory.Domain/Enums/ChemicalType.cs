namespace AgroInventory.Domain.Enums;

/// <summary>Тип средства для химии: гербицид, фунгицид и т.д. Необязателен.</summary>
public enum ChemicalType
{
    Herbicide = 1,        // Гербицид
    Fungicide = 2,        // Фунгицид
    Insecticide = 3,      // Инсектицид
    SeedTreatment = 4,    // Протравитель
    Desiccant = 5,        // Десикант
    GrowthRegulator = 6,  // Регулятор роста
    Fertilizer = 7,       // Удобрение
    Other = 8,            // Другое
}

public static class ChemicalTypeExtensions
{
    /// <summary>Русское название типа средства для отображения/экспорта.</summary>
    public static string ToRussian(this ChemicalType type) => type switch
    {
        ChemicalType.Herbicide => "Гербицид",
        ChemicalType.Fungicide => "Фунгицид",
        ChemicalType.Insecticide => "Инсектицид",
        ChemicalType.SeedTreatment => "Протравитель",
        ChemicalType.Desiccant => "Десикант",
        ChemicalType.GrowthRegulator => "Регулятор роста",
        ChemicalType.Fertilizer => "Удобрение",
        ChemicalType.Other => "Другое",
        _ => type.ToString(),
    };
}
