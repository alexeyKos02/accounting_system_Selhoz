// Удобные псевдонимы типов из сгенерированной OpenAPI-схемы (npm run gen:api).
import type { components } from './schema'

type S = components['schemas']

export type CropDto = S['CropDto']
export type WarehouseDto = S['WarehouseDto']
export type ChemicalListItemDto = S['ChemicalListItemDto']
export type ChemicalDetailDto = S['ChemicalDetailDto']
export type ArchivedChemicalDto = S['ArchivedChemicalDto']
export type WarehouseStockDto = S['WarehouseStockDto']
export type PackageGroupDto = S['PackageGroupDto']
export type OpenedPackageDto = S['OpenedPackageDto']
export type CropRefDto = S['CropRefDto']
export type DuplicateDto = S['DuplicateDto']

export type CreateChemicalRequest = S['CreateChemicalRequest']
export type UpdateChemicalRequest = S['UpdateChemicalRequest']
export type MergeChemicalsRequest = S['MergeChemicalsRequest']
export type ArchiveChemicalRequest = S['ArchiveChemicalRequest']

// ChemicalType: тип средства (см. Domain/Enums/ChemicalType.cs). Необязателен.
export type ChemicalTypeValue = S['ChemicalType']

export const ChemicalType = {
  Herbicide: 1,
  Fungicide: 2,
  Insecticide: 3,
  SeedTreatment: 4,
  Desiccant: 5,
  GrowthRegulator: 6,
  Fertilizer: 7,
  Other: 8,
} as const

export const chemicalTypeLabels: Record<number, string> = {
  1: 'Гербицид',
  2: 'Фунгицид',
  3: 'Инсектицид',
  4: 'Протравитель',
  5: 'Десикант',
  6: 'Регулятор роста',
  7: 'Удобрение',
  8: 'Другое',
}

// Опции для выпадающего списка «Тип средства».
export const chemicalTypeOptions = Object.entries(chemicalTypeLabels).map(
  ([value, label]) => ({ value: Number(value), label }),
)

// StockStatus: 0 InStock, 1 Low, 2 Empty (см. Application/Chemicals/ChemicalDtos.cs)
export const StockStatus = { InStock: 0, Low: 1, Empty: 2 } as const
// ItemStatus: 1 Active, 2 Archived, 3 Merged
export const ItemStatus = { Active: 1, Archived: 2, Merged: 3 } as const
// UnitType: 1 Liter, 2 Can, 3 Piece
export const UnitType = { Liter: 1, Can: 2, Piece: 3 } as const
