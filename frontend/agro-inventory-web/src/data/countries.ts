// Справочник стран для формы хозяйства.
// В базе храним код ISO 3166-1 alpha-2 (RU, KZ, …), пользователю показываем русское название.
// Русские названия берём из браузера через Intl.DisplayNames('ru') — не хардкодим список имён вручную.

// Полный список кодов ISO 3166-1 alpha-2.
const ISO_ALPHA2_CODES = [
  'AD','AE','AF','AG','AI','AL','AM','AO','AQ','AR','AS','AT','AU','AW','AX','AZ',
  'BA','BB','BD','BE','BF','BG','BH','BI','BJ','BL','BM','BN','BO','BQ','BR','BS','BT','BV','BW','BY','BZ',
  'CA','CC','CD','CF','CG','CH','CI','CK','CL','CM','CN','CO','CR','CU','CV','CW','CX','CY','CZ',
  'DE','DJ','DK','DM','DO','DZ',
  'EC','EE','EG','EH','ER','ES','ET',
  'FI','FJ','FK','FM','FO','FR',
  'GA','GB','GD','GE','GF','GG','GH','GI','GL','GM','GN','GP','GQ','GR','GS','GT','GU','GW','GY',
  'HK','HM','HN','HR','HT','HU',
  'ID','IE','IL','IM','IN','IO','IQ','IR','IS','IT',
  'JE','JM','JO','JP',
  'KE','KG','KH','KI','KM','KN','KP','KR','KW','KY','KZ',
  'LA','LB','LC','LI','LK','LR','LS','LT','LU','LV','LY',
  'MA','MC','MD','ME','MF','MG','MH','MK','ML','MM','MN','MO','MP','MQ','MR','MS','MT','MU','MV','MW','MX','MY','MZ',
  'NA','NC','NE','NF','NG','NI','NL','NO','NP','NR','NU','NZ',
  'OM',
  'PA','PE','PF','PG','PH','PK','PL','PM','PN','PR','PS','PT','PW','PY',
  'QA',
  'RE','RO','RS','RU','RW',
  'SA','SB','SC','SD','SE','SG','SH','SI','SJ','SK','SL','SM','SN','SO','SR','SS','ST','SV','SX','SY','SZ',
  'TC','TD','TF','TG','TH','TJ','TK','TL','TM','TN','TO','TR','TT','TV','TW','TZ',
  'UA','UG','UM','US','UY','UZ',
  'VA','VC','VE','VG','VI','VN','VU',
  'WF','WS',
  'YE','YT',
  'ZA','ZM','ZW',
] as const

export interface CountryOption {
  code: string
  name: string
}

// Резолвер русских названий стран. Если Intl.DisplayNames недоступен — показываем код.
function makeDisplayNames(): { of(code: string): string } {
  try {
    const dn = new Intl.DisplayNames(['ru'], { type: 'region' })
    return { of: (code) => dn.of(code) ?? code }
  } catch {
    return { of: (code) => code }
  }
}

const displayNames = makeDisplayNames()

// Опции для выпадающего списка, отсортированные по русскому названию.
export const countryOptions: CountryOption[] = ISO_ALPHA2_CODES
  .map((code) => ({ code, name: displayNames.of(code) }))
  .sort((a, b) => a.name.localeCompare(b.name, 'ru'))

const codeSet = new Set<string>(ISO_ALPHA2_CODES)

// Обратный индекс «русское название → код» для нормализации legacy-значений (напр. «Россия» → RU).
const nameToCode = new Map<string, string>()
for (const { code, name } of countryOptions) {
  nameToCode.set(name.toLowerCase(), code)
}

/**
 * Приводит хранимое значение страны к коду ISO 3166-1 alpha-2.
 * Уже-код возвращаем как есть; старые русские названия («Россия») мапим в код;
 * иначе возвращаем исходное значение (селект в editable-режиме его покажет).
 */
export function normalizeCountry(value: string | null | undefined): string {
  const raw = (value ?? '').trim()
  if (!raw) return ''
  const upper = raw.toUpperCase()
  if (codeSet.has(upper)) return upper
  const byName = nameToCode.get(raw.toLowerCase())
  return byName ?? raw
}
