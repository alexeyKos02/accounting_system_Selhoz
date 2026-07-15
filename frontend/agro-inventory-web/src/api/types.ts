// Удобные псевдонимы типов из сгенерированной OpenAPI-схемы (npm run gen:api).
import type { components } from './schema'

type S = components['schemas']

export type CropDto = S['CropDto']
export type WarehouseDto = S['WarehouseDto']
export type FieldDto = S['FieldDto']
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

// ---------- Аутентификация и мультиарендность (ТЗ §1–§6, §21) ----------

export type LoginRequest = S['LoginRequest']
export type TokenResponse = S['TokenResponse']
export type ChangePasswordRequest = S['ChangePasswordRequest']
export type MeResponse = S['MeResponse']
export type MembershipInfo = S['MembershipInfo']

export type CompanyListItemDto = S['CompanyListItemDto']
export type CompanyDto = S['CompanyDto']
export type CreateCompanyRequest = S['CreateCompanyRequest']
export type UpdateCompanyRequest = S['UpdateCompanyRequest']

export type MemberDto = S['MemberDto']
export type AddMemberRequest = S['AddMemberRequest']
export type UpdateMemberRequest = S['UpdateMemberRequest']
export type ScopeItemDto = S['ScopeItemDto']
export type MemberScopesDto = S['MemberScopesDto']
export type UpdateScopesRequest = S['UpdateScopesRequest']

export type AdminUserDto = S['AdminUserDto']
export type CreateUserRequest = S['CreateUserRequest']
export type UpdateUserRequest = S['UpdateUserRequest']

// Общий каталог препаратов (ТЗ §12, §17)
export type CanonicalChemicalDto = S['CanonicalChemicalDto']
export type CreateCanonicalChemicalRequest = S['CreateCanonicalChemicalRequest']
export type UpdateCanonicalChemicalRequest = S['UpdateCanonicalChemicalRequest']
export type AggregatedChemicalGroupDto = S['AggregatedChemicalGroupDto']
export type AggregatedPositionDto = S['AggregatedPositionDto']
export type AggregatedWarehouseDto = S['AggregatedWarehouseDto']

// AppRole (Domain/Enums/AppRole.cs): 0 SystemAdmin, 1 Owner, 2 CompanyAdmin, 3 Manager, 4 Storekeeper, 5 Viewer
export const AppRole = {
  SystemAdmin: 0, Owner: 1, CompanyAdmin: 2, Manager: 3, Storekeeper: 4, Viewer: 5,
} as const
export const appRoleLabels: Record<number, string> = {
  0: 'Системный администратор',
  1: 'Владелец',
  2: 'Администратор хозяйства',
  3: 'Менеджер',
  4: 'Кладовщик',
  5: 'Наблюдатель',
}
// Роли, назначаемые в рамках хозяйства (SystemAdmin — глобальная, здесь не назначается).
export const companyRoleOptions = [1, 2, 3, 4, 5].map((value) => ({ value, label: appRoleLabels[value] }))

// MembershipStatus: 0 Active, 1 Suspended, 2 Removed
export const MembershipStatus = { Active: 0, Suspended: 1, Removed: 2 } as const
export const membershipStatusLabels: Record<number, string> = {
  0: 'Активен', 1: 'Приостановлен', 2: 'Удалён',
}

// UserStatus: 0 Active, 1 Blocked, 2 Deleted
export const UserStatus = { Active: 0, Blocked: 1, Deleted: 2 } as const
export const userStatusLabels: Record<number, string> = {
  0: 'Активен', 1: 'Заблокирован', 2: 'Удалён',
}

// AccessScopeType: 0 Company, 1 Warehouse, 2 Field
export const AccessScopeType = { Company: 0, Warehouse: 1, Field: 2 } as const

// Коды прав (Domain/Constants/Permissions.cs) — для скрытия недоступных действий на фронте (ТЗ §5).
export const Permissions = {
  CompanyView: 'company.view', CompanyManage: 'company.manage',
  UsersView: 'users.view', UsersManage: 'users.manage',
  WarehousesView: 'warehouses.view', WarehousesManage: 'warehouses.manage',
  FieldsView: 'fields.view', FieldsManage: 'fields.manage',
  InventoryView: 'inventory.view', InventoryManage: 'inventory.manage',
  ReceiptsView: 'receipts.view', ReceiptsCreate: 'receipts.create',
  WriteoffsView: 'writeoffs.view', WriteoffsCreate: 'writeoffs.create',
  TransfersView: 'transfers.view', TransfersCreate: 'transfers.create',
  AdjustmentsCreate: 'adjustments.create',
  TreatmentsView: 'treatments.view', TreatmentsManage: 'treatments.manage',
  HarvestsView: 'harvests.view', HarvestsManage: 'harvests.manage',
  ReportsView: 'reports.view', AuditView: 'audit.view', SettingsManage: 'settings.manage',
} as const
